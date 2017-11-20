using System;
using System.Collections.Generic;
using System.IO;
using Graphite.Extensions;
using Graphite.Http;
using NUnit.Framework;
using Should;
using Tests.Common;

namespace Tests.Unit.Http
{
    [TestFixture]
    public class DelimitedBufferTests
    {
        private static readonly byte[] Delimiter = "\r\n".ToBytes();

        [TestCase("", "", 2, 1)]
        [TestCase("f", "f", 2, 1)]
        [TestCase("fark", "fark", 2, 1)]
        [TestCase("fark", "fark", 2, 2)]
        [TestCase("fark", "fark", 2, 3)]
        [TestCase("fark", "fark", 2, 1)]
        [TestCase("fark", "fark", 3, 1)]
        [TestCase("fark", "fark", 4, 1)]
        [TestCase("\r\n", ",", 2, 1)]
        [TestCase("f\r\n", "f,", 2, 1)]
        [TestCase("\r\na", ",a", 2, 1)]
        [TestCase("fark\r\nfarker", "fark,farker", 4, 4)]
        [TestCase("fark\r\nfarker", "fark,farker", 6, 4)]
        [TestCase("fark\r\nmc\r\nfarker", "fark,mc,farker", 4, 4)]
        [TestCase("fark\r\nfarker", "fark,farker", 3, 3)]
        [TestCase("fark\r\nmc\r\nfarker", "fark,mc,farker", 5, 5)]
        [TestCase("fark\r\nmc\r\nfarker", "fark,mc,farker", 6, 5)]
        public void Should_read_into_buffer(string data, string expected, 
            int multilineBufferSize, int readBufferSize)
        {
            ReadLines(data, multilineBufferSize, readBufferSize)
                .ShouldOnlyContain(expected.Split(','));
        }

        private List<string> ReadLines(string data,
            int multilineBufferSize, int readBufferSize)
        {
            var stream = new MemoryStream(data.ToBytes());
            var buffer = new DelimitedBuffer(stream, multilineBufferSize);
            var lines = new List<string>();
            var currentLine = "";

            while (true)
            {
                var lineBuffer = new byte[readBufferSize];
                var result = buffer.ReadTo(lineBuffer, 0, readBufferSize, Delimiter);
                var currentRead = lineBuffer.ToString(result.Read);

                result.Invalid.ShouldBeFalse();

                currentLine += currentRead;

                Console.WriteLine($"EOL: {result.EndOfSection} " +
                                  $"EOS: {result.EndOfStream} " +
                                  $"Read: {result.Read} " +
                                  $"Data: {currentRead}");

                if (result.EndOfSection)
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                if (result.EndOfStream) return lines;
            }
        }

        [TestCase("", 1, "", false)]
        [TestCase("fark", 1, "f", true)]
        [TestCase("fark", 1, "fa", false)]
        [TestCase("fark", 2, "fa", true)]
        [TestCase("fark", 4, "fark", true)]
        [TestCase("fark", 4, "farker", false)]
        public void Should_start_with(string data, int bufferSize, string compare, bool expected)
        {
            var stream = new MemoryStream(data.ToBytes());
            var buffer = new DelimitedBuffer(stream, bufferSize);

            buffer.StartsWith(compare.ToBytes()).ShouldEqual(expected);
        }

        [TestCase("", 1, "", false, false, 0)]
        [TestCase("f", 1, "f", true, false, 0)]
        [TestCase("fa", 1, "a", true, false, 1)]
        [TestCase("fark", 4, "ar", true, false, 1)]
        [TestCase("fark", 4, "hai", true, true, 4)]
        [TestCase("fark", 4, "farker", true, true, 4)]
        public void Should_read_to(string data, int bufferSize, string delimiter,
            bool endOfSection, bool endOfStream, int read)
        {
            var stream = new MemoryStream(data.ToBytes());
            var buffer = new DelimitedBuffer(stream, bufferSize);

            var result = buffer.ReadTo(delimiter.ToBytes());

            result.EndOfSection.ShouldEqual(endOfSection);
            result.EndOfStream.ShouldEqual(endOfStream);
            result.Read.ShouldEqual(read);
            result.Invalid.ShouldBeFalse();
        }

        [TestCase("", 1, "", "", false, false, 0, false)]
        [TestCase("f", 1, "f", "", true, false, 0, false)]
        [TestCase("fa", 1, "a", "", true, false, 1, false)]
        [TestCase("fark", 4, "ar", "", true, false, 1, false)]
        [TestCase("fark", 4, "hai", "", true, true, 4, false)]
        [TestCase("fark", 4, "farker", "", true, true, 4, false)]
        public void Should_read_to_failing_on_invalid_chars(
            string data, int bufferSize, string delimiter, string invalidChars,
            bool endOfSection, bool endOfStream, int read, bool invalid)
        {
            var stream = new MemoryStream(data.ToBytes());
            var buffer = new DelimitedBuffer(stream, bufferSize);

            var result = buffer.ReadTo(delimiter.ToBytes(), invalidChars.ToCharArray());

            result.EndOfSection.ShouldEqual(endOfSection);
            result.EndOfStream.ShouldEqual(endOfStream);
            result.Read.ShouldEqual(read);
            result.Invalid.ShouldEqual(invalid);
            throw new NotImplementedException();
        }

        [Test]
        public void Should_read_failing_on_invalid_tokens()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void Should_read_to_failing_on_invalid_tokens()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void Should_fail_if_buffer_less_than_1()
        {
            Assert.Throws<ArgumentException>(() => new DelimitedBuffer(null, 0));
        }

        [Test]
        public void Should_indicate_when_at_begining_of_stream()
        {
            var stream = new MemoryStream("fark".ToBytes());
            var buffer = new DelimitedBuffer(stream);

            buffer.BeginingOfStream.ShouldBeTrue();

            buffer.ReadTo(null, 0, 2, null);

            buffer.BeginingOfStream.ShouldBeFalse();
        }

        [Test]
        public void Should_fail_if_offset_less_than_0()
        {
            new DelimitedBuffer(new MemoryStream())
                .Should().Throw<ArgumentException>(
                    x => x.ReadTo(new byte[] { }, -1, 1, Delimiter));
        }

        [Test]
        public void Should_fail_if_count_less_than_0()
        {
            new DelimitedBuffer(new MemoryStream())
                .Should().Throw<ArgumentException>(x =>
                    x.ReadTo(new byte[] { }, 1, -1, Delimiter));
        }
    }
}
