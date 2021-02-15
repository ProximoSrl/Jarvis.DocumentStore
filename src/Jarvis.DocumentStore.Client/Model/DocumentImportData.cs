using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client.Model
{
    public class DocumentImportData
    {
        public Guid TaskId { get; private set; }
        public Uri Uri { get; private set; }
        public String FileName { get; set; }
        public DocumentHandle Handle { get;  private set; }
        public DocumentFormat Format { get;  private set; }
        public string Tenant { get;  private set; }
        public IDictionary<string,object> CustomData { get;  set; }
        public bool DeleteAfterImport { get;  set; }

        /// <summary>
        /// If true we want to import file as reference in IBlobStore
        /// </summary>
        public bool ImportAsReference { get; set; }

        public void AddPdfFile(string pdfFile)
        {
            //Add pdf as custom data.
            if (CustomData == null)
            {
                CustomData = new Dictionary<string, Object>();
            }
            CustomData.Add("format-pdf", pdfFile);
        }

        internal DocumentImportData(
            Uri uri,
            string fileName,
            DocumentHandle handle,
            DocumentFormat format,
            string tenant,
            Guid taskId)
        {
            TaskId = taskId;
            Uri = uri;
            FileName = fileName;
            Handle = handle;
            Format = format;
            Tenant = tenant;
        }
    }
}
