using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.DocumentStore.Core.Processing;
using MongoDB.Driver.GridFS;
using DocumentFormat = Jarvis.DocumentStore.Core.Domain.DocumentDescriptor.DocumentFormat;

namespace Jarvis.DocumentStore.Core.Storage
{
    public class BlobStoreByFormat : IBlobStore
    {
        readonly IBlobStore _originals;
        readonly IBlobStore _artifacts;
        public BlobStoreByFormat(IBlobStore originals, IBlobStore artifacts)
        {
            _originals = originals;
            _artifacts = artifacts;
        }

        public IBlobStore ForFormat(DocumentFormat format)
        {
            if (format == DocumentFormats.Original)
            {
                return _originals;
            }

            return _artifacts;
        }

        public IBlobStore ForBlobId(BlobId blobId)
        {
            return ForFormat(blobId.Format);
        }

        public IBlobDescriptor GetDescriptor(BlobId blobId)
        {
            return ForBlobId(blobId).GetDescriptor(blobId);
        }

        public void Delete(BlobId blobId)
        {
            ForBlobId(blobId).Delete(blobId);
        }

        public string Download(BlobId blobId, string folder)
        {
            return ForBlobId(blobId).Download(blobId, folder);
        }

        public IBlobWriter CreateNew(DocumentFormat format, FileNameWithExtension fname)
        {
            return ForFormat(format).CreateNew(format, fname);
        }

        public BlobId Upload(DocumentFormat format, string pathToFile)
        {
            return ForFormat(format).Upload(format, pathToFile);
        }

        public BlobId Upload(DocumentFormat format, FileNameWithExtension fileName, Stream sourceStrem)
        {
            return ForFormat(format).Upload(format, fileName, sourceStrem);
        }

        public BlobStoreInfo GetInfo()
        {
            var ori = _originals.GetInfo();
            var art = _artifacts.GetInfo();
            return new BlobStoreInfo(ori,art);
        }
    }
}