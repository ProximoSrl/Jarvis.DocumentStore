﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Client.Model
{
    public class ClientAttachmentInfo
    {
        public String RelativePath { get; set; }

        public String Handle { get; set; }

        public Boolean HasAttachments { get; set; }
    }
}
