using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Graphite.Http;

namespace Graphite.Binding
{
    public class MultipartContent : IEnumerable<InputStream>
    {
        private readonly MultipartReader _reader;
        private MultipartPartContent _peeked;
        private IEnumerator<InputStream> _enumerator;

        public MultipartContent(Stream stream, HttpContent content, Configuration configuration)
        {
            _reader = new MultipartReader(stream, content, configuration.DefaultBufferSize);
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
                yield return (PopPeeked() ?? GetPart())
                    .CreateInputStream();
            }
        }

        private MultipartPartContent GetPart()
        {
            if (!_reader.EndOfPart)
                _reader.ReadToNextPart();

            var headers = _reader.CurrentSection == MultipartSection.Headers
                ? _reader.ReadString()
                : null;

            return new MultipartPartContent(_reader, headers);
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
