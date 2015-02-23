using Jarvis.DocumentStore.JobsHost.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Constraints;

namespace Jarvis.DocumentStore.Tests.JobsHostTest.Support
{
    [TestFixture]
    public class ClientPasswordSetTests
    {

        [Test]
        public void basic_parsing_of_password_set()
        {
            ClientPasswordTestTsc sut = new ClientPasswordTestTsc("regex1||password1,regex2||password2");
            Assert.That(sut.GetPasswords().ToList(), Is.EquivalentTo(new []{"password1", "password2"}));
        }

        [Test]
        public void basic_parsing_of_password_with_escape_set()
        {
            ClientPasswordTestTsc sut = new ClientPasswordTestTsc(@"regex1||password1,regex2||pas,,sword2");
            Assert.That(sut.GetPasswords().ToList(), Is.EquivalentTo(new[] { "password1", "pas,sword2" }));
        }

        [Test]
        public void basic_use_of_regex()
        {
            ClientPasswordTestTsc sut = new ClientPasswordTestTsc(@"\.pdf||password1,\.doc||password2");
            Assert.That(sut.GetPasswordFor("blabla.doc"), Is.EquivalentTo(new[] { "password2" }));
        }

        public class ClientPasswordTestTsc : ClientPasswordSetBase
        {
            public ClientPasswordTestTsc(string passwordList) : base(passwordList)
            {
            }
        }
    }
}
