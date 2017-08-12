﻿using System.Linq;
using System.Text;
using Graphite.Extensions;
using NUnit.Framework;
using Should;
using Tests.Common;

namespace Tests.Unit.Extensions
{
    [TestFixture]
    public class StringExtensionTests
    {
        [Test]
        public void Should_remove_by_regex()
        {
            "OneTwoOneThreeThree".Remove("^One", "Three").ShouldEqual("TwoOne");
        }

        [TestCase("", "", null)]
        [TestCase("FarkFarker", "^(Fark)", "Fark")]
        [TestCase("Fark", "^(Farker)", null)]
        public void Should_return_matched_groups(
            string source, string regex, string expected)
        {
            source.MatchGroups(regex).ShouldOnlyContain(
                expected?.Split(',') ?? new string[] {});
        }

        [Test]
        public void Should_create_table()
        {
            1.To(3).Select(x => new
            {
                FirstName = $"Fark{x * 100}",
                LastName = $"Farker{x}"
            }).ToTable(x => new
            {
                First = x.FirstName,
                Last_Name = x.LastName
            }).ShouldEqual(
                "First·· Last Name\r\n" +
                "Fark100 Farker1  \r\n" +
                "Fark200 Farker2··\r\n" +
                "Fark300 Farker3  ");
        }

        [TestCase(null, null)]
        [TestCase("", "")]
        [TestCase("f", "F")]
        [TestCase("fark", "Fark")]
        [TestCase("FARK", "Fark")]
        public void Should_initial_cap(string source, string expected)
        {
            source.InitialCap().ShouldEqual(expected);
        }

        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("fark", "ZmFyaw==")]
        public void Should_encode_base64(string text, string expected)
        {
            text.ToBase64(Encoding.UTF8).ShouldEqual(expected);
        }

        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase("farker", "")]
        [TestCase("ZmFyaw==", "fark")]
        public void Should_decode_base64(string base64, string expected)
        {
            base64.FromBase64(Encoding.UTF8).ShouldEqual(expected);
        }
    }
}
