using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Graphite.Extensions;

namespace Graphite.Http
{
    public enum MultipartSection
    {
        Preamble,
        Headers,
        Body,
        Epilogue
    }

    public class MultipartReader
    {
        // Implementation of RFC 2046 5.1.1 (except for nested multitype)

        private static readonly byte[] CRLF = "\r\n".ToBytes();
        private static readonly byte[] BodyDelimiter = "\r\n\r\n".ToBytes();
        private static readonly byte[] EpiloguePostfix = "--".ToBytes();

        private readonly DelimitedBuffer _buffer;
        private readonly byte[] _boundary;
        private readonly byte[] _boundaryLine;

        public MultipartReader(Stream stream, HttpContent content,
            int bufferSize = DelimitedBuffer.DefaultBufferSize)
        {
            _buffer = new DelimitedBuffer(stream, bufferSize);

            var boundary = content.GetContentBoundry();
            if (boundary.IsNullOrEmpty())
                throw new ArgumentException("No boundry specified in the content-type header.");

            _boundary = $"--{boundary}".ToBytes();
            _boundaryLine = $"\r\n--{boundary}".ToBytes();
        }

        public bool EndOfPart { get; private set; }
        public bool EndOfStream { get; private set; }
        public MultipartSection CurrentSection { get; private set; } = MultipartSection.Preamble;

        public void ReadToNextPart()
        {
            while (true)
            {
                var result = Read(null, 0, DelimitedBuffer.DefaultBufferSize);
                if (result.EndOfPart) return;
            }
        }

        public string ReadString(Encoding encoding = null)
        {
            var buffer = new byte[DelimitedBuffer.DefaultBufferSize];
            var data = "";
            while (true)
            {
                var result = Read(buffer, 0, DelimitedBuffer.DefaultBufferSize);
                if (result.Read > 0)
                    data += (encoding ?? Encoding.UTF8)
                        .GetString(buffer, 0, result.Read);
                if (result.EndOfPart) return data;
            }
        }

        public class ReadResult
        {
            public ReadResult(MultipartSection section, 
                DelimitedBuffer.ReadResult result)
            {
                Read = result.Read;
                Section = section;
                EndOfStream = result.EndOfStream;
                EndOfPart = result.EndOfSection;
            }

            public int Read { get; }
            public MultipartSection Section { get; }
            public bool EndOfPart { get; }
            public bool EndOfStream { get; }
        }

        public ReadResult Read(byte[] buffer, int offset, int count)
        {
            var delimiter = CurrentSection == MultipartSection.Headers
                ? BodyDelimiter
                : (_buffer.StartsWith(_boundary)
                    ? _boundary
                    : _boundaryLine);

            var result = _buffer.ReadTo(buffer, offset, count, delimiter);

            var multipartResult = new ReadResult(CurrentSection, result);

            if (!result.EndOfStream && result.EndOfSection && 
                CurrentSection != MultipartSection.Epilogue)
                SetCurrentSection();
            
            EndOfStream = multipartResult.EndOfStream;
            EndOfPart = multipartResult.EndOfPart;
            
            return multipartResult;
        }

        private void SetCurrentSection()
        {
            if (CurrentSection == MultipartSection.Headers)
            {
                CurrentSection = MultipartSection.Body;
                return;
            }

            if (_buffer.StartsWith(EpiloguePostfix))
            {
                _buffer.ReadTo(CRLF);
                CurrentSection = MultipartSection.Epilogue;
                return;
            }

            var result = _buffer.ReadTo(CRLF);

            if (result.EndOfStream)
            {
                CurrentSection = MultipartSection.Epilogue;
            }
            else if (_buffer.StartsWith(CRLF))
            {
                _buffer.ReadTo(CRLF);
                CurrentSection = MultipartSection.Body;
            }
            else if (_buffer.StartsWith(_boundary))
            {
                CurrentSection = MultipartSection.Body;
            }
            else CurrentSection = MultipartSection.Headers;
        }
    }
}