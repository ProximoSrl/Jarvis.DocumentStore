﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client.Model
{
    public class DocumentImportData
    {
        public Uri Uri { get; private set; }
        public DocumentHandle Handle { get;  private set; }
        public DocumentFormat Format { get;  private set; }
        public string Tenant { get;  private set; }
        public IDictionary<string,object> CustomData { get;  set; }
        public bool DeleteAfterImport { get;  set; }

        internal DocumentImportData(
            Uri uri, 
            DocumentHandle handle, 
            DocumentFormat format, 
            string tenant
        )
        {
            Uri = uri;
            Handle = handle;
            Format = format;
            Tenant = tenant;
        }
    }
}
