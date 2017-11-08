using System;
using System.IO;

namespace Graphite.Http
{
    public class MultipartPartStream : Stream
    {
        private readonly MultipartReader _reader;
        private bool _endOfPart;

        public MultipartPartStream(MultipartReader reader)
        {
            _reader = reader;
        }

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = false;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_endOfPart) return 0;
            var result = _reader.Read(buffer, offset, count);
            _endOfPart = result.EndOfPart;
            return result.Read;
        }
        
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override long Length => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => 
            throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => 
            throw new NotSupportedException();
    }
}