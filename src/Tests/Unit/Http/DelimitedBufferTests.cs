using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        //        Data     Delimiter  Buffer  Invalid  EOP     EOS    Read  Result Invalid
        [TestCase("",      "",        1,      "",      true,   true,  0,    "",    false)]
        public void Should_read_to_delimiter_failing_on_invalid_tokens(
            string data, string delmiter, int bufferSize, string invalidTokens, 
            bool endOfSection, bool endOfStream, int read, string expectedData, 
            bool invalid)
        {
            var invalidTokenBytes = invalidTokens.Split(",")
                .Select(x => x.Select(y => (byte)y).ToArray()).ToArray();
            var stream = new MemoryStream(data.ToBytes());
            var buffer = new DelimitedBuffer(stream, bufferSize);
            var readBuffer = new byte[10];
            var readData = "";
            var readLength = 0;
            DelimitedBuffer.ReadResult result;

            while (true)
            {
                result = buffer.Read(readBuffer, 0, 10, invalidTokenBytes);
                readLength += result.Read;
                if (result.Read > 0) readData += readBuffer.ToString(result.Read);
                if (result.Invalid ||
                    result.EndOfSection || result.EndOfStream) break;
            }

            result.Invalid.ShouldEqual(invalid);
            result.EndOfSection.ShouldEqual(endOfSection);
            result.EndOfStream.ShouldEqual(endOfStream);
            readLength.ShouldEqual(read);
            readData.ShouldEqual(expectedData);
            throw new NotImplementedException("fix me");
        }

        private List<string> ReadLines(string data, byte[] delimiter,
            int delimitedBufferSize, int readBufferSize)
        {
            var stream = new MemoryStream(data.ToBytes());
            var buffer = new DelimitedBuffer(stream, delimitedBufferSize);
            var lines = new List<string>();
            var currentLine = "";

            while (true)
            {
                var lineBuffer = new byte[readBufferSize];
                var result = buffer.ReadTo(lineBuffer, 0, readBufferSize, delimiter);
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

                if (result.EndOfStream || result.Read == 0) return lines;
            }
        }

        //        Data    Buffer  Compare   Expected
        [TestCase("",     1,      "",       false)]
        [TestCase("fark", 1,      "f",      true)]
        [TestCase("fark", 1,      "fa",     false)]
        [TestCase("fark", 2,      "fa",     true)]
        [TestCase("fark", 4,      "fark",   true)]
        [TestCase("fark", 4,      "farker", false)]
        public void Should_start_with(string data, int bufferSize, string compare, bool expected)
        {
            var stream = new MemoryStream(data.ToBytes());
            var buffer = new DelimitedBuffer(stream, bufferSize);

            buffer.StartsWith(compare.ToBytes()).ShouldEqual(expected);
        }

        //        Data        Buffer  Delimiter   EOP    EOS    Read  Remaining
        [TestCase("",         1,      "",         true,  true,  0,    "")]
        [TestCase(",",        1,      ",",        true,  false, 0,    "")]
        [TestCase("a,",       1,      ",",        true,  false, 1,    "")]
        
        [TestCase("ab\r\ncd", 2,      "\r\n",     true,  false, 2,    "cd")]
        [TestCase("ab\r\ncd", 3,      "\r\n",     true,  false, 2,    "cd")]
        [TestCase("ab\r\ncd", 4,      "\r\n",     true,  false, 2,    "cd")]
        [TestCase("ab\r\ncd", 5,      "\r\n",     true,  false, 2,    "cd")]
        [TestCase("ab\r\ncd", 6,      "\r\n",     true,  false, 2,    "cd")]
        [TestCase("ab\r\ncd", 7,      "\r\n",     true,  false, 2,    "cd")]
        
        [TestCase("abcd\r\n", 2,      "\r\n",     true,  false, 4,    "")]
        [TestCase("abcd\r\n", 3,      "\r\n",     true,  false, 4,    "")]
        [TestCase("abcd\r\n", 4,      "\r\n",     true,  false, 4,    "")]
        [TestCase("abcd\r\n", 5,      "\r\n",     true,  false, 4,    "")]
        [TestCase("abcd\r\n", 6,      "\r\n",     true,  false, 4,    "")]
        [TestCase("abcd\r\n", 7,      "\r\n",     true,  false, 4,    "")]

        [TestCase("abcd",     4,      "hai",      true,  true,  4,    "")]
        public void Should_read_to(string data, int bufferSize, string delimiter,
            bool endOfSection, bool endOfStream, int read, string remainingData)
        {
            var stream = new MemoryStream(data.ToBytes());
            var buffer = new DelimitedBuffer(stream, bufferSize);

            var result = buffer.ReadTo(delimiter.ToBytes());

            result.EndOfSection.ShouldEqual(endOfSection);
            result.EndOfStream.ShouldEqual(endOfStream);
            result.Invalid.ShouldBeFalse();
            result.Read.ShouldEqual(read);

            var nextBuffer = new byte[10];
            var nextData = "";

            while (true)
            {
                result = buffer.Read(nextBuffer, 0, 10);
                if (result.Read > 0) nextData += nextBuffer.ToString(result.Read);
                if (result.Invalid || result.EndOfSection || result.EndOfStream) break;
            }
            
            nextData.ShouldEqual(remainingData);
        }

        //        Data     Buffer  Delimiter   Valid   EOP    EOS    Read  Invalid
        [TestCase("",      1,      "",         "",     true,  true,  0,    false)]
        [TestCase(",",     1,      ",",        "b",    true,  false, 0,    false)]
        [TestCase("a,",    1,      ",",        "a",    true,  false, 1,    false)]
        [TestCase("a,",    2,      ",",        "a",    true,  false, 1,    false)]
        [TestCase("a,",    3,      ",",        "a",    true,  false, 1,    false)]
        [TestCase("a,",    3,      ",",        "a",    true,  false, 1,    false)]
        [TestCase("ab,",   1,      ",",        "a",    false, false, 1,    true)]
        [TestCase("ab,",   1,      ",",        "a",    false, false, 1,    true)]
        [TestCase("ab,",   2,      ",",        "a",    false, false, 0,    true)]
        [TestCase("ab,",   3,      ",",        "a",    false, false, 0,    true)]
        [TestCase("ab,",   4,      ",",        "a",    false, false, 0,    true)]
        [TestCase("ab,",   1,      ",",        "ab",   true,  false, 2,    false)]
        [TestCase("ab,",   2,      ",",        "ab",   true,  false, 2,    false)]
        [TestCase("ab,",   3,      ",",        "ab",   true,  false, 2,    false)]
        [TestCase("ab,",   4,      ",",        "ab",   true,  false, 2,    false)]
        public void Should_read_to_failing_on_invalid_chars(
            string data, int bufferSize, string delimiter, string validChars,
            bool endOfSection, bool endOfStream, int read, bool invalid)
        {
            var stream = new MemoryStream(data.ToBytes());
            var buffer = new DelimitedBuffer(stream, bufferSize);

            var result = buffer.ReadTo(delimiter.ToBytes(), validChars.ToCharArray());

            result.Invalid.ShouldEqual(invalid);
            result.EndOfSection.ShouldEqual(endOfSection);
            result.EndOfStream.ShouldEqual(endOfStream);
            result.Read.ShouldEqual(read);
        }
        
        //        Data     Buffer  Invalid  EOP     EOS    Read  Result Invalid
        [TestCase("",      1,      "",      true,   true,  0,    "",    false)]
        [TestCase("a",     1,      "a",     false,  false, 0,    "",    true)]
        [TestCase("a",     1,      "b",     true,   true,  1,    "a",   false)]

        [TestCase("ab",    1,      "b",     false,  false, 1,    "a",   true)]
        [TestCase("ab",    1,      "c",     true,   true,  2,    "ab",  false)]

        [TestCase("abcde", 2,      "yz,cd", false,  false, 2,    "ab",  true)]

        [TestCase("abcde", 2,      "cd,yz", false,  false, 2,    "ab",  true)]
        [TestCase("abcde", 3,      "cd,yz", false,  false, 2,    "ab",  true)]
        [TestCase("abcde", 4,      "cd,yz", false,  false, 0,    "",    true)]
        [TestCase("abcde", 5,      "cd,yz", false,  false, 0,    "",    true)]
        [TestCase("abcde", 6,      "cd,yz", false,  false, 0,    "",    true)]
        
        [TestCase("abcde", 2,      "ab",    false,  false, 0,    "",    true)]
        [TestCase("abcde", 3,      "ab",    false,  false, 0,    "",    true)]
        [TestCase("abcde", 4,      "ab",    false,  false, 0,    "",    true)]
        [TestCase("abcde", 5,      "ab",    false,  false, 0,    "",    true)]
        [TestCase("abcde", 6,      "ab",    false,  false, 0,    "",    true)]

        [TestCase("abcde", 2,      "de",    false,  false, 3,    "abc", true)]
        [TestCase("abcde", 3,      "de",    false,  false, 2,    "ab",  true)]
        [TestCase("abcde", 4,      "de",    false,  false, 3,    "abc", true)]
        [TestCase("abcde", 5,      "de",    false,  false, 0,    "",    true)]
        [TestCase("abcde", 6,      "de",    false,  false, 0,    "",    true)]
        
        [TestCase("abcde", 6,      "xy",    true,   true,  5,  "abcde", false)]
        public void Should_read_failing_on_invalid_tokens(
            string data, int bufferSize, string invalidTokens, bool endOfSection, 
            bool endOfStream, int read, string expectedData, bool invalid)
        {
            var invalidTokenBytes = invalidTokens.Split(",")
                .Select(x => x.Select(y => (byte)y).ToArray()).ToArray();
            var stream = new MemoryStream(data.ToBytes());
            var buffer = new DelimitedBuffer(stream, bufferSize);
            var readBuffer = new byte[10];
            var readData = "";
            var readLength = 0;
            DelimitedBuffer.ReadResult result;

            while (true)
            {
                result = buffer.Read(readBuffer, 0, 10, invalidTokenBytes);
                readLength += result.Read;
                if (result.Read > 0) readData += readBuffer.ToString(result.Read);
                if (result.Invalid || result.EndOfSection || result.EndOfStream) break;
            }
            
            result.Invalid.ShouldEqual(invalid);
            result.EndOfSection.ShouldEqual(endOfSection);
            result.EndOfStream.ShouldEqual(endOfStream);
            readLength.ShouldEqual(read);
            readData.ShouldEqual(expectedData);
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
                    x => x.ReadTo(new byte[] { }, -1, 1, "fark".ToBytes()));
        }

        [Test]
        public void Should_fail_if_count_less_than_0()
        {
            new DelimitedBuffer(new MemoryStream())
                .Should().Throw<ArgumentException>(x =>
                    x.ReadTo(new byte[] { }, 0, -1, "fark".ToBytes()));
        }

        [Test]
        public void Should_fail_if_buffer_size_less_than_minimum_padding()
        {
            new DelimitedBuffer(new MemoryStream(), 5)
                .Should().Throw<ArgumentException>(x =>
                    x.ReadTo(new byte[] { }, 0, 1, "fark".ToBytes(), "farker".ToBytes()));
        }
    }
}
