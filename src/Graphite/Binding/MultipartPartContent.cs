using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Graphite.Extensions;
using Graphite.Http;

namespace Graphite.Binding
{
    public class MultipartPartContent : HttpContent
    {
        private readonly MultipartPartStream _stream;

        public MultipartPartContent(MultipartReader reader, string headers)
        {
            _stream = new MultipartPartStream(reader);
            ParseHeaders(headers);
        }

        public string Name { get; private set; }

        private void ParseHeaders(string headers)
        {
            if (headers.IsNullOrEmpty()) return;
            
            foreach (var parsedHeader in headers.ParseHeaders())
            {
                if (parsedHeader.Key.EqualsUncase(RequestHeaders.ContentDisposition))
                {
                    if (ContentDispositionHeaderValue.TryParse(
                        parsedHeader.Value, out var contentDisposition))
                    {
                        Headers.ContentDisposition = contentDisposition;
                        Name = contentDisposition.Name;
                    }
                }
                else if (parsedHeader.Key.EqualsUncase(RequestHeaders.ContentType))
                {
                    if (MediaTypeHeaderValue.TryParse(parsedHeader.Value, out var contentType))
                        Headers.ContentType = contentType;
                }
                else if (parsedHeader.Key.EqualsUncase(RequestHeaders.ContentEncoding))
                {
                    var tokens = parsedHeader.Value.ParseTokens();
                    if (tokens?.Any() ?? false)
                        Headers.ContentEncoding.AddRange(tokens);
                }
                else if (parsedHeader.Key.EqualsUncase(RequestHeaders.ContentLength))
                {
                    if (parsedHeader.Value.TryParseLong(out var length))
                        Headers.ContentLength = length;
                }
                else if (parsedHeader.Key.EqualsUncase(RequestHeaders.ContentLanguage))
                {
                    var tokens = parsedHeader.Value.ParseTokens();
                    if (tokens?.Any() ?? false)
                        Headers.ContentLanguage.AddRange(tokens);
                }
                else Headers.TryAddWithoutValidation(parsedHeader.Key, parsedHeader.Value);
            }
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return _stream.ToTaskResult<Stream>();
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return _stream.CopyToAsync(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}