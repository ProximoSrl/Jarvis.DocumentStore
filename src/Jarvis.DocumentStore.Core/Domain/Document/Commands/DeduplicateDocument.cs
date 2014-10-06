﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.Document.Commands
{
    public class DeduplicateDocument : DocumentCommand
    {
        public DocumentId OtherDocumentId { get; private set; }
        public FileAlias OtherAlias { get; private set; }
        public FileNameWithExtension OtherFileName { get; private set; }

        public DeduplicateDocument(DocumentId documentId, DocumentId otherDocumentId, FileAlias otherAlias, FileNameWithExtension otherFileName)
            : base(documentId)
        {
            OtherFileName = otherFileName;
            OtherDocumentId = otherDocumentId;
            OtherAlias = otherAlias;
        }
    }
}