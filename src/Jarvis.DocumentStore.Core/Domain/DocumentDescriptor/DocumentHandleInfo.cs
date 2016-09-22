using System;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor
{
    public class DocumentHandleInfo
    {
        public FileNameWithExtension FileName { get; private set; }
        public DocumentCustomData CustomData { get; private set; }
        public DocumentHandle Handle { get; private set; }

        public DocumentHandleInfo(
            DocumentHandle handle,
            FileNameWithExtension fileName,
            DocumentCustomData customData = null
            )
        {
            Handle = handle;
            FileName = fileName;
            CustomData = customData;
        }

        internal DocumentHandleInfo Clone()
        {
            return new DocumentHandleInfo(
                    Handle,
                    FileName == null ? null : FileName.Clone(),
                    CustomData == null ? null : CustomData.Clone()
            );
        }

        protected bool Equals(DocumentHandleInfo other)
        {
            return Equals(FileName, other.FileName) &&
                DocumentCustomData.IsEquals(CustomData, other.CustomData) && 
                Equals(Handle, other.Handle);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DocumentHandleInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (FileName != null ? FileName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (CustomData != null ? CustomData.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Handle != null ? Handle.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}