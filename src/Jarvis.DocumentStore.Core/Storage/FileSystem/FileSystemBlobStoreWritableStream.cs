using Fasterflect;
using System;
using System.IO;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Storage.FileSystem
{
    /// <summary>
    /// <para>
    /// We have a small problem, some part of DocumentStore are used to 
    /// directly write to a stream. For Gridfs this is not a problem, because
    /// once the stream is closed, GridFs will consolidate the final record
    /// for the file. 
    /// We need to simulate the very same behaviour, we need to give to the 
    /// caller a writable stream that.
    /// <br />
    /// 1) Calculate the md5 hash<br />
    /// 2) Calculate the length<br />
    /// 3) Write the final record on Mongo (the <see cref="FileSystemBlobDescriptor"/><br />
    /// </para>
    /// <para>This wrapper is used to accomplish this task.</para>
    /// </summary>
    /// <remarks>This class can support storage of the descriptor both on mongodb or 
    /// directly in files stored with the real content (extension .descriptor)</remarks>
    internal class FileSystemBlobStoreWritableStream : Stream
    {
        private readonly Stream _wrapped;
        private readonly MD5 _md5;
        private readonly IBlobWriter _writer;
        private Boolean _wrappedClosed;

        internal FileSystemBlobStoreWritableStream(
            Stream wrapped,
            IBlobWriter writer)
        {
            _wrapped = wrapped;
            _md5 = MD5.Create();
            _writer = writer;
        }

        public override bool CanRead
        {
            get { return _wrapped.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _wrapped.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _wrapped.CanWrite; }
        }

        public override long Length
        {
            get { return _wrapped.Length; }
        }

        public override long Position
        {
            get { return _wrapped.Position; }
            set { _wrapped.Position = value; }
        }

        public override void Flush()
        {
            _wrapped.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _wrapped.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _wrapped.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _wrapped.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _wrapped.Write(buffer, offset, count);
            _md5.TransformBlock(buffer, offset, count, buffer, offset);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _md5.TransformBlock(buffer, offset, count, buffer, offset);
            return _wrapped.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            Byte[] buffer = new Byte[1];
            buffer[0] = value;
            _md5.TransformBlock(buffer, 0, 1, buffer, 0);
            _wrapped.WriteByte(value);
        }

        /// <summary>
        /// Close wrapped stream and finalize writing the <see cref="FileSystemBlobDescriptor"/>
        /// with MD5 and length.
        /// </summary>
        public override void Close()
        {
            if (!_wrappedClosed && !Disposed)
            {
                var length = _wrapped.Length;
                _wrapped.Close();

                Byte[] buffer = new Byte[0];
                _md5.TransformFinalBlock(buffer, 0, 0);
                var md5 = BitConverter.ToString(_md5.Hash).Replace("-", "");
                OnStreamClosed(md5, length);
                _wrappedClosed = true;
            }
            base.Close();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _wrapped.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _wrapped.BeginWrite(buffer, offset, count, callback, state);
        }

        public override bool CanTimeout
        {
            get { return _wrapped.CanTimeout; }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return _wrapped.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        public override ObjRef CreateObjRef(Type requestedType)
        {
            return _wrapped.CreateObjRef(requestedType);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _wrapped.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _wrapped.EndWrite(asyncResult);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _wrapped.FlushAsync(cancellationToken);
        }

        [Obsolete("Is obsolete in base class")]
        protected override WaitHandle CreateWaitHandle()
        {
            return (WaitHandle)_wrapped.CallMethod("CreateWaitHandle");
        }

        public override bool Equals(object obj)
        {
            return _wrapped.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _wrapped.GetHashCode();
        }

        public override object InitializeLifetimeService()
        {
            return _wrapped.CallMethod("InitializeLifetimeService");
        }

        [Obsolete("Is obsolete in base class")]
        protected override void ObjectInvariant()
        {
            _wrapped.CallMethod("ObjectInvariant");
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _wrapped.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override int ReadByte()
        {
            return _wrapped.ReadByte();
        }

        public override int ReadTimeout
        {
            get { return _wrapped.ReadTimeout; }
            set { _wrapped.ReadTimeout = value; }
        }

        public override string ToString()
        {
            return _wrapped.ToString();
        }

        public override int WriteTimeout
        {
            get { return _wrapped.WriteTimeout; }
            set { _wrapped.WriteTimeout = value; }
        }

        public Boolean Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            _writer.Dispose();
            _md5.Dispose();
            base.Dispose(disposing);
        }

        #region Events

        public  class FileSystemBlobStoreWritableStreamClosedEventArgs : EventArgs
        {
            public FileSystemBlobStoreWritableStreamClosedEventArgs(string md5, long length)
            {
                Md5 = md5;
                Length = length;
            }

            public String Md5 { get; private set; }

            public Int64 Length { get; private set; }
        }

        public event EventHandler<FileSystemBlobStoreWritableStreamClosedEventArgs> StreamClosed;

        protected void OnStreamClosed(String md5, Int64 length)
        {
            StreamClosed?.Invoke(this, new FileSystemBlobStoreWritableStreamClosedEventArgs(md5, length));
        }

        #endregion
    }
}
