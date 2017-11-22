using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using Graphite.Exceptions;
using Graphite.Http;

namespace Graphite.Binding
{
    public class MultipartContent : IEnumerable<InputStream>
    {
        private readonly MultipartReader _reader;
        private MultipartPartContent _peeked;
        private IEnumerator<InputStream> _enumerator;
        private MultipartPartContent _currentPart;

        public MultipartContent(Stream stream, HttpContentHeaders headers, Configuration configuration)
        {
            _reader = new MultipartReader(stream, headers, configuration.DefaultBufferSize);
        }

        public MultipartPartContent Pop()
        {
            return PopPeeked() ??
                (_reader.EndOfStream
                    ? null
                    : GetPart());
        }

        private MultipartPartContent PopPeeked()
        {
            if (_peeked == null) return null;
            var peeked = _peeked;
            _peeked = null;
            return peeked;
        }

        public MultipartPartContent Peek()
        {
            if (_peeked != null) return _peeked;
            return _reader.EndOfStream 
                ? null 
                : _peeked = GetPart();
        }

        private IEnumerable<InputStream> GetStreams()
        {
            while (!_reader.EndOfStream && _reader
                .CurrentSection != MultipartSection.Epilogue)
            {
                var part = PopPeeked() ?? GetPart();
                if (part.Error) throw new BadRequestException(part.ErrorMessage);
                yield return part.CreateInputStream();
            }
        }

        private MultipartPartContent GetPart()
        {
            if (_reader.CurrentSection == MultipartSection.Preamble || 
                !(_currentPart?.ReadComplete ?? true))
            {
                var result = _reader.ReadToNextPart();
                if (result.Error)
                    return new MultipartPartContent(result.ErrorMessage);
            }

            var headers = _reader.CurrentSection == MultipartSection.Headers
                ? _reader.ReadString()
                : null;

            _currentPart = new MultipartPartContent(_reader, headers?.Data,
                headers?.Error ?? false, headers?.ErrorMessage);

            return _currentPart;
        }

        public IEnumerator<InputStream> GetEnumerator()
        {
            return _enumerator ?? (_enumerator = GetStreams().GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
