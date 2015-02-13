using Jarvis.DocumentStore.Core.ReadModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Jobs.QueueManager
{
    public class QueueInfo
    {
        /// <summary>
        /// name of the queue
        /// </summary>
        public String Name { get; private set; }

        /// <summary>
        /// Reason to exists: if you want to implement a series of operation
        /// where you create a series of pipeline and setup exact sequence with names.
        /// 
        /// It is a .NET regex
        /// If different from null it contains a filter that permits to queue jobs
        /// only if the <see cref="StreamReadModel" /> is generated from a specific
        /// pipeline.
        /// Remember that negative match can be expressed by this one
        /// ^(?!office$|tika$).* http://stackoverflow.com/questions/6830796/regex-to-match-anything-but-two-words
        /// </summary>
        public String Pipeline { get; private set; }

        private String _extension;
        /// <summary>
        /// It is a pipe separated list of desired extension.
        /// </summary>
        public String Extension
        {
            get { return _extension; }
            private set
            {
                _extension = value;
                if (!String.IsNullOrEmpty(value))
                    _splittedExtensions = value.Split('|');
                else
                    _splittedExtensions = new string[] { };
            }
        }

        private String _formats;
        /// <summary>
        /// It is a pipe separated list of all the formats the pipeline is interested to
        /// </summary>
        public String Formats
        {
            get { return _formats; }
            private set
            {
                _formats = value;

                if (!String.IsNullOrEmpty(value))
                    _splittedFormats = value.Split('|');
                else
                    _splittedFormats = new string[] { };
            }
        }

        public PollerInfo[] PollersInfo { get; set; }

        private String[] _splittedExtensions;

        private String[] _splittedFormats;

        public Dictionary<String, String> Parameters { get; set; }

        public int MaxNumberOfFailure { get; set; }

        /// <summary>
        /// When a job is in <see cref="QueuedJobExecutionStatus.Executing "/> status for more
        /// minutes than this value, it will be killed and rescheduled.
        /// </summary>
        public int JobLockTimeout { get; set; }

        public QueueInfo(
                      String name,
                      String pipeline = null,
                      String extensions = null,
                      String formats = null
                  )
        {
            Name = name;
            Pipeline = pipeline;
            Extension = extensions;
            Formats = formats;
            MaxNumberOfFailure = 5;
            JobLockTimeout = 5;
        }

        internal bool ShouldCreateJob(StreamReadModel streamElement)
        {
            if (_splittedExtensions.Length > 0 && !_splittedExtensions.Contains(streamElement.Filename.Extension))
                return false;

            if (_splittedFormats.Length > 0 && !_splittedFormats.Contains(streamElement.FormatInfo.DocumentFormat.ToString()))

                return false;
            if (!String.IsNullOrEmpty(Pipeline) &&
                !Regex.IsMatch(streamElement.FormatInfo.PipelineId, Pipeline))
                return false;

            return true;
        }

        /// <summary>
        /// This represents the info to start a poller in some computer.
        /// </summary>
        public class PollerInfo
        {
            public String Name { get; set; }

            public Dictionary<String, String> Parameters { get; set; }
        }
    }
}
