using System;
using System.IO;
using System.Net.Http.Headers;
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

        public MultipartReader(Stream stream, HttpContentHeaders headers,
            int bufferSize = DelimitedBuffer.DefaultBufferSize)
        {
            _buffer = new DelimitedBuffer(stream, bufferSize);

            var boundary = headers.GetContentBoundry();
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
            public ReadResult(MultipartSection? section, 
                DelimitedBuffer.ReadResult result, 
                string errorMessage = null)
            {
                Read = result.Read;
                Section = section;
                Error = errorMessage.IsNotNullOrEmpty();
                ErrorMessage = errorMessage;
                EndOfStream = result.EndOfStream;
                EndOfPart = result.EndOfSection;
            }

            public ReadResult(string errorMessage,
                DelimitedBuffer.ReadResult result)
                : this(null, result, errorMessage)  { }

            public int Read { get; }
            public MultipartSection? Section { get; }
            public bool EndOfPart { get; }
            public bool EndOfStream { get; }
            public bool Error { get; }
            public string ErrorMessage { get; }
        }





        public ReadResult Read(byte[] buffer, int offset, int count)
        {
            if (CurrentSection == MultipartSection.Preamble)
                return ReadPreamble(buffer, offset, count);

            if (CurrentSection == MultipartSection.Headers)
                return ReadHeaders(buffer, offset, count);

            if (CurrentSection == MultipartSection.Body)
                return ReadBody(buffer, offset, count);

            return ReadEpilogue(buffer, offset, count);
        }

        private ReadResult ReadPreamble(byte[] buffer, int offset, int count)
        {
            var preambleResult = _buffer.BeginingOfStream && _buffer.StartsWith(_boundary)
                ? _buffer.ReadTo(buffer, offset, count, _boundary)
                : _buffer.ReadTo(buffer, offset, count, _boundaryLine, _boundary);

            if (preambleResult.Invalid)
                return new ReadResult("Boundary not preceeded by CRLF.", preambleResult);

            if (preambleResult.EndOfStream)
                return new ReadResult(MultipartSection.Preamble, preambleResult, 
                    "Missing closing boundary.");

            if (!preambleResult.EndOfSection)
                return new ReadResult(MultipartSection.Preamble, preambleResult);

            var boundaryResult = ReadBoundary();
                    
            if (boundaryResult.Error)
                return new ReadResult(MultipartSection.Preamble, preambleResult,
                    boundaryResult.ErrorMessage);

            return new ReadResult(MultipartSection.Preamble, preambleResult);
        }

        private ReadResult ReadHeaders(byte[] buffer, int offset, int count)
        {
            var result = _buffer.ReadTo(buffer, offset, 
                count, BodyDelimiter, _boundaryLine, _boundary);

            if (result.Invalid)
                return new ReadResult("Headers not followed by empty line.", result);

            if (result.EndOfStream)
                return new ReadResult("Unexpected end of stream.", result);

            if (result.EndOfSection) CurrentSection = MultipartSection.Body;

            return new ReadResult(MultipartSection.Headers, result);
        }

        private ReadResult ReadBody(byte[] buffer, int offset, int count)
        {
            var bodyResult = _buffer.ReadTo(buffer, offset, count, _boundaryLine, _boundary);

            if (bodyResult.Invalid)
                return new ReadResult("Boundary not in its own line.", bodyResult);

            if (bodyResult.EndOfStream)
                return new ReadResult("Unexpected end of stream.", bodyResult);

            if (!bodyResult.EndOfSection)
                return new ReadResult(MultipartSection.Body, bodyResult);

            var boundaryResult = ReadBoundary();

            if (boundaryResult.Error)
                return new ReadResult(MultipartSection.Body, bodyResult,
                    boundaryResult.ErrorMessage);

            return new ReadResult(MultipartSection.Body, bodyResult);
        }

        private ReadResult ReadBoundary()
        {
            var closingBoundary = _buffer.StartsWith(EpiloguePostfix);

            var result = _buffer.ReadTo(CRLF, ' ', '\r', '\n', '\t');

            if (result.Invalid)
                return new ReadResult("Invalid characters following boundary.", result);

            if (result.EndOfStream && !closingBoundary)
                return new ReadResult("Unexpected end of stream.", result);

            if (closingBoundary)
                return new ReadResult(CurrentSection = MultipartSection.Epilogue, result);

            if (!_buffer.StartsWith(CRLF))
                return new ReadResult(CurrentSection = MultipartSection.Headers, result);

            var bodyResult = _buffer.ReadTo(CRLF);

            if (bodyResult.EndOfStream)
                return new ReadResult("Unexpected end of stream.", bodyResult);

            return new ReadResult(CurrentSection = MultipartSection.Body, result);
        }

        private ReadResult ReadEpilogue(byte[] buffer, int offset, int count)
        {
            if (EndOfStream)
                return new ReadResult(MultipartSection.Epilogue, null);

            var result = _buffer.Read(buffer, offset, count, _boundaryLine, _boundary);

            if (result.Invalid)
                return new ReadResult("Boundary found after closing boundary.", result);

            EndOfStream = result.EndOfStream;
            EndOfPart = result.EndOfSection;

            return new ReadResult(MultipartSection.Epilogue, result);
        }







        public ReadResult Read2(byte[] buffer, int offset, int count)
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