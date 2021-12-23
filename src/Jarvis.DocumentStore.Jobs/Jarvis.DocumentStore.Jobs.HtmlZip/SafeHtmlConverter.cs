using Castle.Core.Logging;
using Ganss.XSS;
using System;
using System.IO;

namespace Jarvis.DocumentStore.Jobs.HtmlZip
{
    public class SafeHtmlConverter
    {
        public ILogger Logger { get; set; }
        private readonly String _filePath;

        public SafeHtmlConverter(String filePath)
        {
            _filePath = filePath;
        }

        public string Run(String jobId)
        {
            Logger.DebugFormat("Sanitize HTML for {0}", jobId);
            string html = File.ReadAllText(_filePath);
            var sanitizer = new HtmlSanitizer();
            var sanitized = sanitizer.Sanitize(html);
            File.WriteAllText(_filePath, sanitized);
            Logger.DebugFormat("Sanitize HTML for {0} done!", jobId);
            return _filePath;
        }
    }
}
