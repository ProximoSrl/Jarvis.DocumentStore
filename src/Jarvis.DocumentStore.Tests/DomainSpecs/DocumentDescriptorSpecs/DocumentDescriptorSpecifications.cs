using System;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.TestHelpers;
using NSubstitute;
using Jarvis.DocumentStore.Core.Support;
using Machine.Specifications;

// ReSharper disable InconsistentNaming

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{

    public abstract class DocumentDescriptorSpecifications : AggregateSpecification<DocumentDescriptor, DocumentDescriptorState>
    {
        protected static readonly DocumentDescriptorId _id = new DocumentDescriptorId(1);
        protected static readonly BlobId _blobId = new BlobId("newFile");
        protected static readonly FileHash _fileHash = new FileHash("abcd1234");
        protected static readonly String _fileName = "file.test";
        protected static readonly DocumentHandle Handle = new DocumentHandle("handle-to-file");
        protected static readonly FileNameWithExtension _fname = new FileNameWithExtension("pathTo.file");
        protected static readonly DocumentHandleInfo _handleInfo = new DocumentHandleInfo(Handle, _fname);
        protected static readonly DocumentHandle _fatherHandle = new DocumentHandle("handle-to-father");

        protected static readonly DocumentHandle _attachmentDocumentHandle = new DocumentHandle("this_is_the_attachment");
        protected static readonly DocumentHandle _otherAttachmentDocumentHandle = new DocumentHandle("this_is_the_other_attachment");
        protected static readonly String _attachmentPath = "Attach-path.txt";

        protected static DocumentDescriptor DocumentDescriptor
        {
            get
            {
                if (Aggregate.DocumentFormatTranslator == null) Aggregate.DocumentFormatTranslator = Substitute.For<IDocumentFormatTranslator>();
                return Aggregate;
            }
        }
    }
}
