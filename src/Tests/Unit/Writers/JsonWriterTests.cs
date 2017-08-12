﻿using System.Threading.Tasks;
using Graphite;
using Graphite.Http;
using Newtonsoft.Json;
using NUnit.Framework;
using Should;
using Tests.Common;
using JsonWriter = Graphite.Writers.JsonWriter;

namespace Tests.Unit.Writers
{
    [TestFixture]
    public class JsonWriterTests
    {
        public class InputModel { }

        public class OutputModel
        {
            public string Value1 { get; set; }
            public string Value2 { get; set; }
        }

        public class Handler
        {
            public OutputModel Post()
            {
                return null;
            }
        }

        [TestCase("text/html", false)]
        [TestCase("application/json", true)]
        [TestCase("text/html,application/json", true)]
        [TestCase("text/html,application/*", true)]
        [TestCase("text/html,*/*", true)]
        public void Should_only_apply_to_mime_types_that_are_accepted(
            string acceptType, bool applies)
        {
            var requestGraph = RequestGraph
                .CreateFor<Handler>(h => h.Post())
                    .WithAccept(acceptType);
            var context = requestGraph.GetResponseWriterContext(null);

            CreateWriter(requestGraph)
                .AppliesTo(context).ShouldEqual(applies);
        }

        [Test]
        public async Task Should_write_json()
        {
            var requestGraph = RequestGraph
                .CreateFor<Handler>(h => h.Post())
                .WithAccept(MimeTypes.ApplicationJson);
            var context = requestGraph.GetResponseWriterContext(new OutputModel
            {
                Value1 = "value1",
                Value2 = "value2"
            });

            var response = await CreateWriter(requestGraph).Write(context);

            var content = await response.Content.ReadAsStringAsync();

            content.ShouldEqual("{\"Value1\":\"value1\",\"Value2\":\"value2\"}");
        }

        private JsonWriter CreateWriter(RequestGraph requestGraph)
        {
            return new JsonWriter(new JsonSerializer(),
                requestGraph.GetHttpRequestMessage(),
                requestGraph.GetHttpResponseMessage(),
                new Configuration());
        }
    }
}
