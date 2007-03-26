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
            string contents = "<lift version='0.9.1'></lift>";
            Validate(contents, true);
        }
        [Test]
        public void BadLiftDoesNotValidate()
        {
            string contents = "<lift version='0.9.1'><header></header><header></header></lift>";
            Validate(contents, false);
        }

        [Test]
        public void WrongVersionNumberGivesHelpfulMessage()
        {
            string contents = "<lift version='0.8'><header></header><header></header></lift>";
            string errors = Validate(contents, false);
            Assert.IsTrue(errors.Contains("This file claims to be version"));
        }

        private static string Validate(string contents, bool shouldPass)
        {
            string f = Path.GetTempFileName();
            File.WriteAllText(f, contents);
            string errors;
            try
            {
                errors = Validator.GetAnyValidationErrors(f);
                if(shouldPass)
                {
                    if (errors != null)
                    {
                        Console.WriteLine(errors);
                    }
                    Assert.IsNull(errors);
                }
                else
                {
                    Assert.IsNotNull(errors);
                }
            }
            finally
            {
                File.Delete(f);
            }
            return errors;
        }

    }

}