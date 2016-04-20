using Jarvis.DocumentStore.Shared.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.Attachments.Tests
{
    public class SevenZipTests : IPollerTest
    {
        SevenZipExtractorFunctions _functions;
        public SevenZipTests(SevenZipExtractorFunctions functions)
        {
            _functions = functions;
        }

        public string Name
        {
            get
            {
                return "7zip test";
            }
        }

        public List<PollerTestResult> Execute()
        {
            List<PollerTestResult> retValue = new List<PollerTestResult>();
            if (_functions.IsOk)
            {
                retValue.Add(new PollerTestResult(true, "7zip executable found"));
            }
            else
            {
                retValue.Add(new PollerTestResult(false, _functions.GetErrorStatus()));
            }
            return retValue;
        }
    }
}
