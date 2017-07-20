﻿using System.Threading.Tasks;
using Bender.Configuration;
using Graphite.Extensions;
using Graphite.Http;
using Graphite.Readers;
using NUnit.Framework;
using Should;
using Tests.Common;

namespace Tests.Unit.Readers.Bender
{
    [TestFixture]
    public class JsonReaderTests
    {
        public class InputModel
        {
            public string Value { get; set; }
        }

        public class Handler
        {
            public void Post(InputModel request, string param) { }
        }

        [Test]
        public void Should_only_apply_if_the_content_type_is_application_json(
            [Values(true, false)] bool isJson)
        {
            var requestGraph = RequestGraph
                .CreateFor<Handler>(h => h.Post(null, null))
                    .WithRequestData("{}")
                    .WithRequestParameter("request")
                    .AddParameters("param");

            if (isJson)
            {
                requestGraph.WithContentType(MimeTypes.ApplicationJson);
            }

            new Graphite.Bender.JsonReader(new Options(),
                    requestGraph.GetRouteDescriptor(),
                    requestGraph.GetHttpRequestMessage())
                .Applies()
                .ShouldEqual(isJson);
        }

        [Test]
        public async Task Should_read_json()
        {
            var requestGraph = RequestGraph
                .CreateFor<Handler>(h => h.Post(null, null))
                    .WithRequestData("{\"Value\":\"fark\"}")
                    .WithRequestParameter("request")
                    .WithContentType(MimeTypes.ApplicationJson)
                    .AddParameters("param");

            var result = await new Graphite.Bender.JsonReader(new Options(),
                    requestGraph.GetRouteDescriptor(),
                    requestGraph.GetHttpRequestMessage()).Read();

            result.ShouldNotBeNull();
            result.ShouldBeType<InputModel>();
            result.CastTo<InputModel>().Value.ShouldEqual("fark");
        }
    }
}
