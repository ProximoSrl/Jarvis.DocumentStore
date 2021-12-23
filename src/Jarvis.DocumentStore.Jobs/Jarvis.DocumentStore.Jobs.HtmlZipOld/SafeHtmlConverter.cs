using Castle.Core.Logging;
using Ganss.XSS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Jarvis.DocumentStore.Jobs.HtmlZipOld
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
            // see: https://github.com/mganss/HtmlSanitizer/issues/231
            sanitizer.AllowedAttributes.Remove("src");
            sanitizer.RemovingAttribute += (s, e) =>
            {
                var _dataImage = new List<string> { "data:image/gif", "data:image/jpeg", "data:image/png", "data:image/jpg", "http://", "https://" };

                switch (e.Tag.TagName)
                {
                    case "IMG":
                        {
                            if (_dataImage.Any(x => e.Attribute.Value.StartsWith(x)))
                            {
                                e.Reason = RemoveReason.NotAllowedAttribute;
                                e.Cancel = true;
                            }

                            break;
                        }
                }
            };
            var sanitized = sanitizer.Sanitize(html);
            File.WriteAllText(_filePath, sanitized);
            Logger.DebugFormat("Sanitize HTML for {0} done!", jobId);
            return _filePath;
        }
    }
}
