using System;
using System.IO;
using System.Text;

using Oxide.Core.Logging;

namespace Oxide.Ext.Python
{
    /// <summary>
    /// Stream to redirect output
    /// </summary>
    public class PythonLogger : Stream
    {
        /// <summary>
        /// Gets the logger that this library writes to
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Initializes a new instance of the PythonLogger
        /// </summary>
        /// <param name="logger"></param>
        public PythonLogger(Logger logger)
        {
            Logger = logger;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var message = Encoding.UTF8.GetString(buffer, offset, count);
            if (message != Environment.NewLine) // Filter empty
                Logger.Write(LogType.Info, message);
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}
