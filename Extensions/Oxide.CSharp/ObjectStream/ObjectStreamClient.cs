using ObjectStream.IO;
using ObjectStream.Threading;
using System;
using System.IO;

namespace ObjectStream
{
    public class ObjectStreamClient<TReadWrite> : ObjectStreamClient<TReadWrite, TReadWrite> where TReadWrite : class
    {
        public ObjectStreamClient(Stream inStream, Stream outStream) : base(inStream, outStream)
        {
        }
    }

    public delegate void StreamExceptionEventHandler(Exception exception);

    public class ObjectStreamClient<TRead, TWrite>
        where TRead : class
        where TWrite : class
    {
        private readonly Stream _inStream;
        private readonly Stream _outStream;
        private ObjectStreamConnection<TRead, TWrite> _connection;

        public ObjectStreamClient(Stream inStream, Stream outStream)
        {
            _inStream = inStream;
            _outStream = outStream;
        }

        public event ConnectionMessageEventHandler<TRead, TWrite> Message;

        public event StreamExceptionEventHandler Error;

        public void Start()
        {
            var worker = new Worker();
            worker.Error += OnError;
            worker.DoWork(ListenSync);
        }

        public void PushMessage(TWrite message)
        {
            if (_connection != null)
                _connection.PushMessage(message);
        }

        public void Stop()
        {
            if (_connection != null)
                _connection.Close();
        }

        #region Private methods

        private void ListenSync()
        {
            // Create a Connection object for the data pipe
            _connection = ConnectionFactory.CreateConnection<TRead, TWrite>(_inStream, _outStream);
            _connection.ReceiveMessage += OnReceiveMessage;
            _connection.Error += ConnectionOnError;
            _connection.Open();
        }

        private void OnReceiveMessage(ObjectStreamConnection<TRead, TWrite> connection, TRead message)
        {
            if (Message != null)
                Message(connection, message);
        }

        private void ConnectionOnError(ObjectStreamConnection<TRead, TWrite> connection, Exception exception)
        {
            OnError(exception);
        }

        private void OnError(Exception exception)
        {
            if (Error != null)
                Error(exception);
        }

        #endregion Private methods
    }

    internal static class ObjectStreamClientFactory
    {
        public static ObjectStreamWrapper<TRead, TWrite> Connect<TRead, TWrite>(Stream inStream, Stream outStream)
            where TRead : class
            where TWrite : class
        {
            return new ObjectStreamWrapper<TRead, TWrite>(inStream, outStream);
        }
    }
}
