using Graphite.Extensions;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Graphite.Writers
{
    public class AsyncStringContent : HttpContent
    {
        private readonly string _data;
        private readonly Encoding _encoding;

        public AsyncStringContent(string data, Encoding encoding, string mimeType)
        {
            _data = data;
            _encoding = encoding;
            Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return stream.WriteAsync(_data, _encoding);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}