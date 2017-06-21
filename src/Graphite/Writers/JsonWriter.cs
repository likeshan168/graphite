﻿using System.Net.Http;
using System.Text;
using Graphite.Http;
using Newtonsoft.Json;

namespace Graphite.Writers
{
    public class JsonWriter  : StringWriterBase
    {
        private readonly JsonSerializerSettings _settings;

        public JsonWriter(JsonSerializerSettings settings, 
            HttpRequestMessage requestMessage,
            HttpResponseMessage responseMessage) : 
            base(requestMessage, responseMessage, Encoding.UTF8, 
                MimeTypes.ApplicationJson)
        {
            _settings = settings;
        }

        protected override string GetResponse(ResponseWriterContext context)
        {
            return JsonConvert.SerializeObject(context.Response, _settings);
        }
    }
}