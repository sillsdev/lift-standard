using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace LiftIO.Tests
{
    [TestFixture]
    public class ValidatorTests
    {
        [SetUp]
        public void Setup()
        {

        }

        [TearDown]
        public void TearDown()
        {

        }


        [Test]
        public void GoodLiftValidates()
        {
            string contents = "<lift version='0.9'></lift>";
            Validate(contents, true);
        }
        [Test]
        public void BadLiftDoesNotValidate()
        {
            string contents = "<lift version='0.9'><header></header><header></header></lift>";
            Validate(contents, false);
        }

        private static void Validate(string contents, bool shouldPass)
        {
            string f = Path.GetTempFileName();
            File.WriteAllText(f, contents);
            try
            {
                Assert.AreEqual(shouldPass, Validator.CheckValidity(f));
            }
            finally
            {
                File.Delete(f);
            }
        }

    }

}