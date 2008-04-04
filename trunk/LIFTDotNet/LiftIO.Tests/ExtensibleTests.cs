using System;
using NUnit.Framework;

namespace LiftIO.Tests
{
    [TestFixture]
    public class ExtensibleTests
    {
        [Test]
        public void ParseDateOnly()
        {
            DateTime parsedDateTime = Extensible.ParseDateTimeCorrectly("2007-02-03");
            Assert.IsTrue(parsedDateTime.Kind == DateTimeKind.Utc);
            Assert.AreEqual(new DateTime(2007, 2, 3),parsedDateTime);
        }

        [Test]
        public void ParseDateTimeWithTimeZone()
        {
            DateTime parsedDateTime = Extensible.ParseDateTimeCorrectly("2007-02-03T03:01:39+07:00");
            Assert.IsTrue(parsedDateTime.Kind == DateTimeKind.Utc);
            Assert.AreEqual(new DateTime(2007, 2, 2, 20, 1, 39, DateTimeKind.Utc), parsedDateTime);
        }

        [Test]
        public void ParseDateTimeWithTimeZoneAtBeginningOfYear()
        {
            DateTime parsedDateTime = Extensible.ParseDateTimeCorrectly("2005-01-01T01:11:11+8:00");
            Assert.IsTrue(parsedDateTime.Kind == DateTimeKind.Utc);
            Assert.AreEqual(new DateTime(2004, 12, 31, 17, 11, 11, DateTimeKind.Utc), parsedDateTime);
        }

        [Test]
        public void ParseDateTimeNoTimeZone()
        {
            DateTime parsedDateTime = Extensible.ParseDateTimeCorrectly("2007-02-03T03:01:39Z");
            Assert.IsTrue(parsedDateTime.Kind == DateTimeKind.Utc);
            Assert.AreEqual(new DateTime(2007, 2, 3,3,1,39,DateTimeKind.Utc), parsedDateTime);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void ParseDate_Bad_Throws()
        {
            Extensible.ParseDateTimeCorrectly("2007-02-03T03:01:39");
        }


    }
}
