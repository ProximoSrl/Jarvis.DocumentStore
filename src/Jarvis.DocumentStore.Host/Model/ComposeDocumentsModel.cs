﻿using Jarvis.DocumentStore.Client.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Host.Model
{
    public class ComposeDocumentsModel
    {
        public DocumentHandle[] ListOfDocumentsToCompose { get; set; }

        public DocumentHandle DocumentToCreate { get; set; }
    }
}
