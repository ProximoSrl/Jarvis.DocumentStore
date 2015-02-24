using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.TestHelpers;
using NSubstitute;
using System;


// ReSharper disable InconsistentNaming

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    public abstract class DocumentSpecifications : AggregateSpecification<Document, DocumentState>
    {
        protected static readonly DocumentId _id = new DocumentId(1);
        protected static readonly BlobId _blobId = new BlobId("newFile");
        protected static readonly FileHash _fileHash = new FileHash("abcd1234");
        protected static readonly String _fileName = "file.test";
        protected static readonly DocumentHandle Handle = new DocumentHandle("handle-to-file");
        protected static readonly FileNameWithExtension _fname = new FileNameWithExtension("pathTo.file");
        protected static readonly DocumentHandleInfo _handleInfo = new DocumentHandleInfo(Handle, _fname);

        protected static Document Document
        {
            get {
                if (Aggregate.DocumentFormatTranslator == null) Aggregate.DocumentFormatTranslator = Substitute.For<IDocumentFormatTranslator>();
                return Aggregate; 
            }
        }
    }
}
