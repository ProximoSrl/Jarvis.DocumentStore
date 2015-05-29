using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Shared.Jobs
{

    /// <summary>
    /// Represents a check for a job to verify configuration
    /// and functionalities for a job.
    /// </summary>
    public interface IPollerTest
    {
        String Name { get; }

        List<PollerTestResult> Execute();
    }

    public class PollerTestResult
    {

        public PollerTestResult(
            Boolean result,
            String message)
        {
            Result = result;
            Message = message;
        }

        public Boolean Result { get; private set; }

        public String Message { get; private set; }
    }
}
