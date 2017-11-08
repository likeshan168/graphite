using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Graphite.Http;
using NUnit.Framework;
using Should;
using Graphite.Extensions;

namespace Tests.Unit.Http
{
    [TestFixture]
    public class MultipartReaderTests
    {
        [Test]
        public void Should_read_all_part_types()
        {
            var results = ReadAll(
                "some preamble\r\n" +
                "--some-boundry\r\n" +
                "content-type: text/plain\r\n" +
                "\r\n" +
                "some text\r\n" +
                "--some-boundry--\r\n" +
                "some epilogue");

            Should_match_section(results[0], "some preamble", MultipartSection.Preamble, true, false);
            Should_match_section(results[1], "content-type: text/p", MultipartSection.Headers, false, false);
            Should_match_section(results[2], "lain", MultipartSection.Headers, false, false);
            Should_match_section(results[3], "", MultipartSection.Headers, true, false);
            Should_match_section(results[4], "some text", MultipartSection.Body, true, false);
            Should_match_section(results[5], "some epilogue", MultipartSection.Epilogue, true, true);
        }

        [Test]
        public void Should_read_part_with_no_headers()
        {
            var results = ReadAll(
                "--some-boundry\r\n" +
                "\r\n" +
                "some text\r\n" +
                "--some-boundry--");

            Should_match_section(results[0], "", MultipartSection.Preamble, true, false);
            Should_match_section(results[1], "some text", MultipartSection.Body, true, false);
            Should_match_section(results[2], "", MultipartSection.Epilogue, true, true);
        }

        [Test]
        public void Should_read_part_with_no_headers_or_body()
        {
            var results = ReadAll(
                "--some-boundry\r\n" +
                "\r\n" +
                "\r\n" +
                "--some-boundry--");

            Should_match_section(results[0], "", MultipartSection.Preamble, true, false);
            Should_match_section(results[1], "", MultipartSection.Body, true, false);
            Should_match_section(results[2], "", MultipartSection.Epilogue, true, true);
        }

        [Test]
        public void Should_read_multiple_parts_with_no_headers()
        {
            var results = ReadAll(
                "--some-boundry\r\n" +
                "content-type: text/plain\r\n" +
                "\r\n" +
                "some text 1\r\n" +
                "--some-boundry\r\n" +
                "content-type: text/plain\r\n" +
                "\r\n" +
                "some text 2\r\n" +
                "--some-boundry--");

            Should_match_section(results[0], "", MultipartSection.Preamble, true, false);

            Should_match_section(results[1], "content-typ", MultipartSection.Headers, false, false);
            Should_match_section(results[2], "e: text/plain", MultipartSection.Headers, true, false);
            Should_match_section(results[3], "some text 1", MultipartSection.Body, true, false);

            Should_match_section(results[4], "content-type: text/p", MultipartSection.Headers, false, false);
            Should_match_section(results[5], "lain", MultipartSection.Headers, false, false);
            Should_match_section(results[6], "", MultipartSection.Headers, true, false);
            Should_match_section(results[7], "some text 2", MultipartSection.Body, true, false);

            Should_match_section(results[8], "", MultipartSection.Epilogue, true, true);
        }

        [Test]
        public void Should_read_empty_part([Values("", "\r\n")] string prefix)
        {
            var results = ReadAll(
                $"{prefix}--some-boundry\r\n" +
                "\r\n" +
                "--some-boundry--");

            Should_match_section(results[0], "", MultipartSection.Preamble, true, false);
            Should_match_section(results[1], "", MultipartSection.Body, true, false);
            Should_match_section(results[2], "", MultipartSection.Epilogue, true, true);
        }

        [Test]
        public void Should_read_boundary_with_missing_leading_crlf()
        {
            var results = ReadAll(
                "--some-boundry\r\n" +
                "--some-boundry\r\n" +
                "content-type: text/plain\r\n" +
                "\r\n" +
                "some text 2\r\n" +
                "--some-boundry--");

            Should_match_section(results[0], "", MultipartSection.Preamble, true, false);
            
            Should_match_section(results[1], "", MultipartSection.Body, true, false);

            Should_match_section(results[2], "content-type: text/p", MultipartSection.Headers, false, false);
            Should_match_section(results[3], "lain", MultipartSection.Headers, false, false);
            Should_match_section(results[4], "", MultipartSection.Headers, true, false);
            Should_match_section(results[5], "some text 2", MultipartSection.Body, true, false);

            Should_match_section(results[6], "", MultipartSection.Epilogue, true, true);
        }

        [Test]
        public void Should_read_epilogue_missing_leading_crlf([Values("", "\r\n")] string prefix)
        {
            var results = ReadAll(
                $"{prefix}--some-boundry\r\n" +
                "--some-boundry--");

            Should_match_section(results[0], "", MultipartSection.Preamble, true, false);
            Should_match_section(results[1], "", MultipartSection.Body, true, false);
            Should_match_section(results[2], "", MultipartSection.Epilogue, true, true);
        }

        [Test]
        public void Should_read_boundary_with_trailing_linear_whitespace()
        {
            var results = ReadAll(
                "--some-boundry \t\r \t\n\r\n" +
                "\r\n" +
                "some text\r\n" +
                "--some-boundry--");

            Should_match_section(results[0], "", MultipartSection.Preamble, true, false);
            Should_match_section(results[1], "some text", MultipartSection.Body, true, false);
            Should_match_section(results[2], "", MultipartSection.Epilogue, true, true);
        }

        [Test]
        public void Should_read_epilogue_only_boundry([Values("", "\r\n")] string prefix)
        {
            var results = ReadAll($"{prefix}--some-boundry--");

            Should_match_section(results[0], "", MultipartSection.Preamble, true, false);
            Should_match_section(results[1], "", MultipartSection.Epilogue, true, true);
        }

        [Test]
        public void Should_read_boundry_only([Values("", "\r\n")] string prefix)
        {
            var results = ReadAll($"{prefix}--some-boundry");

            Should_match_section(results[0], "", MultipartSection.Preamble, true, false);
            Should_match_section(results[1], "", MultipartSection.Epilogue, true, true);
        }

        [Test]
        public void Should_read_part_with_no_epilogue([Values("", "\r\n")] string postfix)
        {
            var results = ReadAll(
                "--some-boundry\r\n" +
                "\r\n" +
                $"some text{postfix}");

            Should_match_section(results[0], "", MultipartSection.Preamble, true, false);
            Should_match_section(results[1], $"some text{postfix}", MultipartSection.Body, true, true);
        }

        [Test]
        public void Should_()
        {
            // TODO: add tests for changes...
            throw new NotImplementedException();
        }

        private void Should_match_section(ReadResult result,
            string value, MultipartSection section,
            bool endOfPart, bool endOfStream)
        {
            result.Data.ShouldEqual(value);
            result.Result.Section.ShouldEqual(section);
            result.Result.Read.ShouldEqual(value?.Length ?? 0);
            result.Result.EndOfPart.ShouldEqual(endOfPart);
            result.Result.EndOfStream.ShouldEqual(endOfStream);
        }

        public class ReadResult
        {
            public MultipartReader.ReadResult Result { get; set; }
            public string Data { get; set; }
        }

        private List<ReadResult> ReadAll(string source)
        {
            var content = new StringContent(source);
            content.Headers.ContentType.Parameters.Add(
                new NameValueHeaderValue("boundary", "some-boundry"));
            var reader =  new MultipartReader(content.ReadAsStreamAsync().Result, content, 30);
            var results = new List<ReadResult>();
            var buffer = new byte[20];

            while (true)
            {
                var result = reader.Read(buffer, 0, 20);
                string data = "";
                if (result.Read > 0)
                    data = buffer.ToString(result.Read);
                Console.WriteLine($"{result.Section.ToString().PadRight(8)}: " +
                    $"{(data ?? "").PadRight(20)}       " +
                    $"EOP: {result.EndOfPart.ToString().PadRight(5)} EOS: {result.EndOfStream}");
                results.Add(new ReadResult
                {
                    Result = result,
                    Data = data
                });
                if (result.EndOfStream) return results;
            }
        }
    }
}
