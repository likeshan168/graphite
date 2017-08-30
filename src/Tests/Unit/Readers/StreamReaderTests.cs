﻿using System.IO;
using System.Threading.Tasks;
using Graphite.Extensions;
using Graphite.Readers;
using NUnit.Framework;
using Should;
using Tests.Common;
using StreamReader = Graphite.Readers.StreamReader;

namespace Tests.Unit.Readers
{
    [TestFixture]
    public class StreamReaderTests
    {
        public class Handler
        {
            public void StreamRequest(Stream stream) { }

            public void InputStreamRequest(InputStream inputStream) { }

            public void NoRequest() { }

            public void NonStreamRequest(int request) { }
        }

        [Test]
        public void Should_only_apply_to_actions_with_a_stream_request()
        {
            var requestGraph = RequestGraph.CreateFor<Handler>(x => x.StreamRequest(null))
                .WithRequestParameter("stream");
            CreateStreamReader(requestGraph).Applies().ShouldBeTrue();
        }

        [Test]
        public void Should_only_apply_to_actions_with_an_input_stream_request()
        {
            var requestGraph = RequestGraph.CreateFor<Handler>(x => x.InputStreamRequest(null))
                .WithRequestParameter("inputStream");
            CreateStreamReader(requestGraph).Applies().ShouldBeTrue();
        }

        [Test]
        public void Should_not_apply_to_actions_with_a_non_stream_request()
        {
            var requestGraph = RequestGraph.CreateFor<Handler>(x => x.NonStreamRequest(0))
                .WithRequestParameter("request");
            CreateStreamReader(requestGraph).Applies().ShouldBeFalse();
        }

        [Test]
        public void Should_not_apply_to_actions_with_no_request()
        {
            var requestGraph = RequestGraph.CreateFor<Handler>(x => x.NoRequest());
            CreateStreamReader(requestGraph).Applies().ShouldBeFalse();
        }

        [Test]
        public async Task Should_pass_stream_to_stream_request_parameter()
        {
            var requestGraph = RequestGraph.CreateFor<Handler>(x => x.StreamRequest(null))
                .WithRequestParameter("stream")
                .WithRequestData("fark");

            var result = await CreateStreamReader(requestGraph).Read();

            result.Status.ShouldEqual(ReadStatus.Success);
            result.Value.ShouldBeType<MemoryStream>();
            result.Value.As<Stream>().ReadAllText().ShouldEqual("fark");
        }

        [TestCase(null, null)]
        [TestCase("fark/farker", "fark.txt")]
        public async Task Should_pass_input_stream_to_input_stream_request_parameter(
            string mimeType, string filename)
        {
            var requestGraph = RequestGraph.CreateFor<Handler>(x => x.InputStreamRequest(null))
                .WithRequestParameter("inputStream")
                .WithRequestData("fark");

            if (mimeType.IsNotNullOrEmpty()) requestGraph.WithContentType(mimeType);
            if (filename.IsNotNullOrEmpty()) requestGraph.WithAttachmentFilename(filename);

            var result = await CreateStreamReader(requestGraph).Read();

            result.Status.ShouldEqual(ReadStatus.Success);
            result.Value.ShouldBeType<InputStream>();
            var inputStream = result.Value.As<InputStream>();
            inputStream.Data.ReadAllText().ShouldEqual("fark");
            inputStream.Length.ShouldEqual(4);
            inputStream.MimeType.ShouldEqual(mimeType);
            inputStream.Filename.ShouldEqual(filename);
        }

        private StreamReader CreateStreamReader(RequestGraph requestGraph)
        {
            return new StreamReader(
                requestGraph.GetRouteDescriptor(),
                requestGraph.GetHttpRequestMessage());
        }
    }
}
