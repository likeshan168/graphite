﻿using System.Linq;
using System.Threading.Tasks;
using Graphite.Binding;
using Graphite.Extensions;
using Graphite.Readers;
using NUnit.Framework;
using Should;
using Tests.Common;

namespace Tests.Unit.Binding
{
    [TestFixture]
    public class ReaderBinderTests
    {
        public class Handler
        {
            public void Get(string request) { }
        }

        [Test]
        public async Task Should_bind_reader_result_with_the_first_matching_reader()
        {
            var requestGraph = RequestGraph
                .CreateFor<Handler>(h => h.Get(null))
                .WithRequestParameter("request")
                .AddRequestReader1(() => ReadResult.Success("reader1").ToTaskResult())
                .AddRequestReader2(() => ReadResult.Success("reader2").ToTaskResult());
            var requestReaderContext = requestGraph.GetRequestBinderContext();

            var result = await CreateBinder(requestGraph).Bind(requestReaderContext);

            result.Status.ShouldEqual(BindingStatus.Success);

            requestGraph.ActionArguments.ShouldOnlyContain("reader1");

            requestGraph.RequestReader1.AppliesCalled.ShouldBeTrue();
            requestGraph.RequestReader1.ReadCalled.ShouldBeTrue();

            requestGraph.RequestReader2.AppliesCalled.ShouldBeTrue();
            requestGraph.RequestReader2.ReadCalled.ShouldBeFalse();
        }

        [Test]
        public void Should_not_bind_reader_if_route_does_not_have_a_request()
        {
            var requestGraph = RequestGraph.CreateFor<Handler>(h => h.Get(null));

            CreateBinder(requestGraph).AppliesTo(requestGraph
                .GetRequestBinderContext()).ShouldBeFalse();
        }

        [Test]
        public async Task Should_not_bind_with_reader_that_does_not_apply_in_configuration()
        {
            var requestGraph = RequestGraph
                .CreateFor<Handler>(h => h.Get(null))
                .WithRequestParameter("request")
                .AddRequestReader1(() => ReadResult.Success("reader1").ToTaskResult(),
                    configAppliesTo: x => false)
                .AddRequestReader2(() => ReadResult.Success("reader2").ToTaskResult())
                .AddValueMapper1(x => MapResult.Success(x.Values.First()));

            var result = await CreateBinder(requestGraph).Bind(requestGraph.GetRequestBinderContext());

            result.Status.ShouldEqual(BindingStatus.Success);

            requestGraph.ActionArguments.ShouldOnlyContain("reader2");

            requestGraph.RequestReader1.AppliesCalled.ShouldBeFalse();
            requestGraph.RequestReader1.ReadCalled.ShouldBeFalse();

            requestGraph.RequestReader2.AppliesCalled.ShouldBeTrue();
            requestGraph.RequestReader2.ReadCalled.ShouldBeTrue();
        }

        [Test]
        public async Task Should_not_bind_with_reader_instance_that_does_not_apply()
        {
            var requestGraph = RequestGraph
                .CreateFor<Handler>(h => h.Get(null))
                .WithRequestParameter("request")
                .AddRequestReader1(() => ReadResult.Success("reader1").ToTaskResult(),
                    instanceAppliesTo: () => false)
                .AddRequestReader2(() => ReadResult.Success("reader2").ToTaskResult())
                .AddValueMapper1(x => MapResult.Success(x.Values.First()));

            var binder = CreateBinder(requestGraph);

            var result = await binder.Bind(requestGraph.GetRequestBinderContext());

            result.Status.ShouldEqual(BindingStatus.Success);

            requestGraph.ActionArguments.ShouldOnlyContain("reader2");

            requestGraph.RequestReader1.AppliesCalled.ShouldBeTrue();
            requestGraph.RequestReader1.ReadCalled.ShouldBeFalse();

            requestGraph.RequestReader2.AppliesCalled.ShouldBeTrue();
            requestGraph.RequestReader2.ReadCalled.ShouldBeTrue();
        }

        [Test]
        public async Task Should_bind_default_reader_if_configured_and_no_reader_applies()
        {
            var requestGraph = RequestGraph
                .CreateFor<Handler>(h => h.Get(null))
                .WithRequestParameter("request")
                .AddRequestReader1(() => ReadResult.Success("reader1").ToTaskResult(),
                    instanceAppliesTo: () => false)
                .AddRequestReader2(() => ReadResult.Success("reader2").ToTaskResult(),
                    instanceAppliesTo: () => false, @default: true)
                .AddValueMapper1(x => MapResult.Success(x.Values.First()));

            var binder = CreateBinder(requestGraph);

            var result = await binder.Bind(requestGraph.GetRequestBinderContext());

            result.Status.ShouldEqual(BindingStatus.Success);

            requestGraph.ActionArguments.ShouldOnlyContain("reader2");

            requestGraph.RequestReader1.AppliesCalled.ShouldBeTrue();
            requestGraph.RequestReader1.ReadCalled.ShouldBeFalse();

            requestGraph.RequestReader2.AppliesCalled.ShouldBeTrue();
            requestGraph.RequestReader2.ReadCalled.ShouldBeTrue();
        }

        [Test]
        public async Task Should_not_bind_if_no_reader_applies()
        {
            var requestGraph = RequestGraph
                .CreateFor<Handler>(h => h.Get(null))
                .WithRequestParameter("request")
                .AddValueMapper1(x => MapResult.Success($"{x.Values.First()}mapper"));
            var binder = CreateBinder(requestGraph);

            var result = await binder.Bind(requestGraph.GetRequestBinderContext());

            result.Status.ShouldEqual(BindingStatus.NoReader);

            requestGraph.ActionArguments.ShouldOnlyContain(null);

            requestGraph.ValueMapper1.MapCalled.ShouldBeFalse();
        }

        public ReaderBinder CreateBinder(RequestGraph requestGraph)
        {
            return new ReaderBinder(requestGraph.GetActionDescriptor(),
                requestGraph.RequestReaders);
        }
    }
}
