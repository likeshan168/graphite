using System;
using System.IO;
using System.Linq;
using Graphite.Extensions;

namespace Graphite.Http
{
    public class DelimitedBuffer
    {
        public const int DefaultBufferSize = 1024;

        private readonly byte[] _buffer;
        private readonly Stream _stream;
        private bool _end;
        private int _size;
        private int _offset;

        public DelimitedBuffer(Stream stream,
            int bufferSize = DefaultBufferSize)
        {
            if (bufferSize < 1) throw new ArgumentException(
                "Buffer size must be greater than one.",
                nameof(bufferSize));

            _buffer = new byte[bufferSize];
            _stream = stream;
        }

        public bool BeginingOfStream { get; private set; } = true;

        public bool StartsWith(byte[] compare)
        {
            if (_size < compare.Length) Fill();

            return _buffer.ContainsAt(compare, _offset);
        }

        public ReadResult Read(byte[] buffer, int offset, int count,
            params byte[][] invalidTokens)
        {
            return ReadTo(null, (b, o, c) => !invalidTokens
                .Any(x => b.ContainsAt(x, o, c)));
        }

        public ReadResult ReadTo(byte[] delimiter, params char[] validChars)
        {
            return ReadTo(delimiter, (b, o, c) => !validChars
                .Select(x => new [] { (byte)x })
                .Any(x => b.ContainsAt(x, o, c)));
        }

        public ReadResult ReadTo(byte[] delimiter, Func<byte[], int, int, bool> validate = null)
        {
            var read = 0;
            while (true)
            {
                var result = ReadTo(null, 0, DefaultBufferSize, delimiter, validate);
                read += result.Read;
                if (result.Read == 0 || result.EndOfSection || result.EndOfStream)
                    return new ReadResult(read, result.EndOfSection, result.EndOfStream);
            }
        }

        public class ReadResult
        {
            public ReadResult(int read, bool endOfSection, bool endOfStream, bool invalid = false)
            {
                Read = read;
                EndOfSection = endOfSection;
                EndOfStream = endOfStream;
                Invalid = invalid;
            }

            public int Read { get; }
            public bool EndOfSection { get; }
            public bool EndOfStream { get; }
            public bool Invalid { get; }
        }

        public ReadResult ReadTo(byte[] buffer, int offset, int count, 
            byte[] delimiter, params byte[][] invalidTokens)
        {
            return ReadTo(buffer, offset, count, delimiter, 
                (b, o, c) => !invalidTokens.Any(x => b.Contains(x, o, c)));
        }
        
        private ReadResult ReadTo(byte[] buffer, int offset, int count, byte[] delimiter,
            Func<byte[], int, int, bool> validate = null)
        {
            if (offset < 0) throw new ArgumentException($"Offset must be greater than zero but was {offset}.");
            if (count < 1) throw new ArgumentException($"Count must be greater than 1 but was {count}.");

            BeginingOfStream = false;

            if (delimiter == null || delimiter.Length == 0)
                return new ReadResult(0, false, _end);

            if (_size < delimiter.Length)
            {
                Fill();
                if (_end && _size == 0) return new ReadResult(0, true, true);
            }

            var maxSize = Math.Min(count, _size);

            var endOfLine = _buffer.IndexOfSequence(delimiter, _offset, maxSize);

            if (endOfLine == 0)
            {
                ShiftOffset(delimiter.Length);
                return new ReadResult(0, true, _end);
            }

            var readSize = endOfLine > 0
                ? endOfLine
                : Math.Min(maxSize, _size - delimiter.Length + 1);

            if (readSize <= 0) readSize = maxSize;

            if (!(validate?.Invoke(_buffer, _offset, readSize) ?? true))
                return new ReadResult(0, false, _end);

            if (buffer != null)
                Array.Copy(_buffer, _offset, buffer, offset, readSize);

            var readToEndOfLine = _offset + readSize == endOfLine || (_end && _size - readSize == 0);

            ShiftOffset(readSize);
            if (readToEndOfLine && endOfLine >= 0) ShiftOffset(delimiter.Length);

            return new ReadResult(readSize, readToEndOfLine, _end);
        }

        private void ShiftOffset(int count)
        {
            _size -= count;
            _offset = _size == 0 ? 0 : _offset + count;
        }

        private void Fill()
        {
            if (_end) return;

            if (_size > 0)
            {
                Array.Copy(_buffer, _offset, _buffer, 0, _size);
                _offset = 0;
            }
            
            var maxLength = _buffer.Length - _size;

            if (maxLength == 0) return;

            var bytesRead = _stream.Read(_buffer, _size, maxLength);

            if (bytesRead == 0) _end = true;

            _size += bytesRead;
        }
    }
}