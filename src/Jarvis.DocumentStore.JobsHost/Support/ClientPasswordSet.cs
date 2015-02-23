using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.JobsHost.Support
{
    /// <summary>
    /// represents a set of password configured in the client
    /// to being able to decrypt file protected with passwords
    /// </summary>
    public interface IClientPasswordSet
    {
        /// <summary>
        /// Given a file name it returns the sets
        /// of passwords that are available for that file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        IEnumerable<String> GetPasswordFor(String fileName);

        /// <summary>
        /// Get all passwords for all files.
        /// </summary>
        /// <returns></returns>
        IEnumerable<String> GetPasswords();
    }

    public class NullClientPasswordSet : IClientPasswordSet
    {
        public static IClientPasswordSet Instance { get; private set; }

        static NullClientPasswordSet ()
        {
            Instance = new NullClientPasswordSet();
        }

        private NullClientPasswordSet()
        {
            
        }

        public IEnumerable<string> GetPasswordFor(string fileName)
        {
            return new String[] { };
        }

        public IEnumerable<string> GetPasswords()
        {
            return new String[] { };
        }


    }

    public class ClientPasswordSetBase : IClientPasswordSet
    {
         private Dictionary<String, String> _passwords;
 
        public ClientPasswordSetBase(String passwordList)
        {
            if (String.IsNullOrEmpty(passwordList))
            {
                _passwords = new Dictionary<string, string>();
                return;
            }

            //handle the escape, the ♣ is a not used UNICODE chars
            //used to handle the escape of doubel colon in password.
            var tempPasswordList = passwordList.Replace(",,", "♣");
            var matches = Regex.Matches(tempPasswordList, @"(?<regex>.+?)\|\|(?<pwd>.+?)(?:,|$)");
            _passwords = new Dictionary<string, string>();
            foreach (Match match in matches)
            {
                _passwords[match.Groups["regex"].Value] = match.Groups["pwd"].Value.Replace("♣", ",");
            }
        }

        public IEnumerable<string> GetPasswordFor(string fileName)
        {
            return _passwords.Where(kvp =>
                Regex.IsMatch(fileName, kvp.Key))
                .Select(kvp => kvp.Value)
                .ToList();
        }

        public IEnumerable<string> GetPasswords()
        {
            return _passwords.Values;
        }
    }

    public class EnvironmentVariableClientPasswordSet : ClientPasswordSetBase
    {
        public EnvironmentVariableClientPasswordSet() : 
            base(Environment.GetEnvironmentVariable("DS_DOCPWDS"))
        {
        }
    }
}
