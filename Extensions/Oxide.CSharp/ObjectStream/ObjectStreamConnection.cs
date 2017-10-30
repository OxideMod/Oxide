using ObjectStream.IO;
using ObjectStream.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ObjectStream
{
    public class ObjectStreamConnection<TRead, TWrite>
        where TRead : class
        where TWrite : class
    {
        private readonly ObjectStreamWrapper<TRead, TWrite> _streamWrapper;
        private readonly Queue<TWrite> _writeQueue = new Queue<TWrite>();
        private readonly AutoResetEvent _writeSignal = new AutoResetEvent(false);

        internal ObjectStreamConnection(Stream inStream, Stream outStream)
        {
            _streamWrapper = new ObjectStreamWrapper<TRead, TWrite>(inStream, outStream);
        }

        public event ConnectionMessageEventHandler<TRead, TWrite> ReceiveMessage;

        public event ConnectionExceptionEventHandler<TRead, TWrite> Error;

        public void Open()
        {
            var readWorker = new Worker();
            readWorker.Error += OnError;
            readWorker.DoWork(ReadStream);

            var writeWorker = new Worker();
            writeWorker.Error += OnError;
            writeWorker.DoWork(WriteStream);
        }

        public void PushMessage(TWrite message)
        {
            _writeQueue.Enqueue(message);
            _writeSignal.Set();
        }

        public void Close() => CloseImpl();

        private void CloseImpl()
        {
            Error = null;
            _streamWrapper.Close();
            _writeSignal.Set();
        }

        private void OnError(Exception exception)
        {
            if (Error != null)
                Error(this, exception);
        }

        private void ReadStream()
        {
            while (_streamWrapper.CanRead)
            {
                var obj = _streamWrapper.ReadObject();
                ReceiveMessage?.Invoke(this, obj);
                if (obj != null) continue;
                CloseImpl();
                return;
            }
        }

        private void WriteStream()
        {
            while (_streamWrapper.CanWrite)
            {
                _writeSignal.WaitOne();
                while (_writeQueue.Count > 0)
                    _streamWrapper.WriteObject(_writeQueue.Dequeue());
            }
        }
    }

    internal static class ConnectionFactory
    {
        public static ObjectStreamConnection<TRead, TWrite> CreateConnection<TRead, TWrite>(Stream inStream, Stream outStream)
            where TRead : class
            where TWrite : class
        {
            return new ObjectStreamConnection<TRead, TWrite>(inStream, outStream);
        }
    }

    public delegate void ConnectionMessageEventHandler<TRead, TWrite>(ObjectStreamConnection<TRead, TWrite> connection, TRead message)
        where TRead : class
        where TWrite : class;

    public delegate void ConnectionExceptionEventHandler<TRead, TWrite>(ObjectStreamConnection<TRead, TWrite> connection, Exception exception)
        where TRead : class
        where TWrite : class;
}
