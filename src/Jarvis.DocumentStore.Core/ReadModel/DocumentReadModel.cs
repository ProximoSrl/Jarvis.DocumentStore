using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System;
using Jarvis.DocumentStore.Shared.Jobs;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class DocumentReadModel : IReadModel
    {
        [BsonId]
        public DocumentHandle Handle { get; private set; }

        public HashSet<DocumentAttachmentReadModel> Attachments { get; private set; }

        public DocumentDescriptorId DocumentDescriptorId { get; private set; }

        public long CreatetAt { get; private set; }

        public long ProjectedAt { get; private set; }

        public DocumentCustomData CustomData { get; private set; }

        public FileNameWithExtension FileName { get; private set; }

        public DocumentReadModel(DocumentHandle handle)
            : this(handle, null, null, null)
        {

        }

        public DocumentReadModel(DocumentHandle handle, DocumentDescriptorId documentid, FileNameWithExtension fileName)
            : this(handle, documentid, fileName, null)
        {
        }

        public DocumentReadModel(DocumentHandle handle, DocumentDescriptorId documentid, FileNameWithExtension fileName, DocumentCustomData customData)
        {
            Handle = handle;
            DocumentDescriptorId = documentid;
            FileName = fileName;
            CustomData = customData;
        }

        public bool IsPending()
        {
            return this.CreatetAt > this.ProjectedAt;
        }
    }

    public class DocumentAttachmentReadModel 
    {

        public DocumentAttachmentReadModel()
        {

        }

        public DocumentAttachmentReadModel(DocumentHandle attachmentHandle, string attachmentPath)
        {
            Handle = attachmentHandle;
            RelativePath = attachmentPath;
        }

        /// <summary>
        /// Handle of the attachment.
        /// </summary>
        public DocumentHandle Handle { get; set; }

        /// <summary>
        /// Relative path of this attachment to the original handle
        /// </summary>
        public String RelativePath { get; set; }
    }

    public interface IDocumentWriter
    {
        void Promise(DocumentHandle handle, long createdAt);
        DocumentReadModel FindOneById(DocumentHandle handle);
        void Drop();
        void Init();

        void LinkDocument(DocumentHandle handle, DocumentDescriptorId id, long projectedAt);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="primaryHandle">Is the <see cref="DocumentHandle"/> of the primary 
        /// handle associated at the <see cref="DocumentDescriptorId"/> <param name="id"></param></param>
        /// <param name="id">The id of the destination DocumentDescriptor</param>
        /// <param name="projectedAt"></param>
        void DocumentDeDuplicated(
            DocumentHandle handle,  
            DocumentHandle primaryHandle, 
            DocumentDescriptorId id, 
            long projectedAt);

        void UpdateCustomData(DocumentHandle handle, DocumentCustomData customData);
        void Delete(DocumentHandle handle, long projectedAt);
        IQueryable<DocumentReadModel> AllSortedByHandle { get; }
        void CreateIfMissing(DocumentHandle handle, DocumentDescriptorId documentDescriptorId, long createdAt);
        void SetFileName(DocumentHandle handle, FileNameWithExtension fileName, long projectedAt);
        long Count();

        void AddAttachment(DocumentHandle fatherHandle, DocumentHandle attachmentHandle);
    }

    public class DocumentWriter : IDocumentWriter
    {
        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        readonly MongoCollection<DocumentReadModel> _collection;
        private ILogger _logger = NullLogger.Instance;

        public DocumentWriter(MongoDatabase readModelDb)
        {
            _collection = readModelDb.GetCollection<DocumentReadModel>(CollectionNames.GetCollectionName<DocumentReadModel>());
        }

        public void Promise(DocumentHandle handle, long createdAt)
        {
            Logger.DebugFormat("Promise on handle {0} [{1}]", handle, createdAt);
            var args = new FindAndModifyArgs
            {
                Query = Query.And(
                    Query<DocumentReadModel>.EQ(x => x.Handle, handle),
                    Query<DocumentReadModel>.LT(x => x.ProjectedAt, createdAt)
                ),
                Update = Update<DocumentReadModel>
                    .SetOnInsert(x => x.CustomData, null)
                    .SetOnInsert(x => x.ProjectedAt, 0)
                    .Set(x => x.DocumentDescriptorId, null)
                    .Set(x => x.CreatetAt, createdAt)
                    .Set(x => x.FileName, null),
                Upsert = true,
                VersionReturned = FindAndModifyDocumentVersion.Modified
            };

            try
            {
                var result = _collection.FindAndModify(args);

                if (Logger.IsDebugEnabled)
                {
                    Logger.DebugFormat("Promise on handle {0} [{1}] : {2}", handle, createdAt,
                        result.ModifiedDocument != null ? result.ModifiedDocument.ToJson() : "null");
                }

            }
            catch (MongoCommandException ex)
            {
                Logger.WarnFormat("update to handle {0} failed (concurrency): {1}", handle, ex.Message);
            }
        }

        public void SetFileName(DocumentHandle handle, FileNameWithExtension fileName, long projectedAt)
        {
            Logger.DebugFormat("SetFilename on handle {0} [{1}]", handle, projectedAt);
            var args = new FindAndModifyArgs
            {
                Query = Query.And(
                    Query<DocumentReadModel>.EQ(x => x.Handle, handle),
                    Query<DocumentReadModel>.LTE(x => x.CreatetAt, projectedAt)
                ),
                Update = Update<DocumentReadModel>
                    .Set(x => x.FileName, fileName)
                    .Set(x => x.ProjectedAt, projectedAt)
            };
            _collection.FindAndModify(args);
        }

        public long Count()
        {
            return _collection.Count();
        }

        public void LinkDocument(DocumentHandle handle, DocumentDescriptorId id, long projectedAt)
        {
            InnerCreateLinkToDocument(handle, id, null, projectedAt);
        }

        public void DocumentDeDuplicated(
            DocumentHandle handle,
            DocumentHandle primaryHandle,
            DocumentDescriptorId id,
            long projectedAt)
        {
            var linkChanged = InnerCreateLinkToDocument(handle, id, true, projectedAt);

            //if (linkChanged)
            //{
            //    //need to manage attachments, first step, find the original handle that belong to that descriptor
            //    CopyAttachmentFromPrimaryHandle(handle, primaryHandle, projectedAt);
            //}
        }

        private Boolean InnerCreateLinkToDocument(DocumentHandle handle, DocumentDescriptorId id, Boolean? deDuplication, long projectedAt)
        {
            Logger.DebugFormat("LinkDocument on handle {0} [{1}]", handle, projectedAt);
            var update = Update<DocumentReadModel>
                    .Set(x => x.DocumentDescriptorId, id)
                    .Set(x => x.ProjectedAt, projectedAt);

            var query = Query.And(
                    Query<DocumentReadModel>.EQ(x => x.Handle, handle),
                    Query<DocumentReadModel>.NE(x => x.DocumentDescriptorId, id),
                    Query<DocumentReadModel>.LTE(x => x.CreatetAt, projectedAt)
                );
            var args = new FindAndModifyArgs
            {
                Query = query,
                Update = update,
                VersionReturned = FindAndModifyDocumentVersion.Modified
            };
            var result = _collection.FindAndModify(args);

            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("LinkDocument on handle {0} [{1}] : {2}", handle, projectedAt,
                    result.ModifiedDocument != null ? result.ModifiedDocument.ToJson() : "null");
            }
            return result.ModifiedDocument != null;
        }

        //private void CopyAttachmentFromPrimaryHandle(
        //    DocumentHandle handle,
        //    DocumentHandle primaryHandle,
        //    long projectedAt)
        //{
        //    var primaryAttachHandle = _collection.FindOneById(BsonValue.Create(primaryHandle));

        //    if (primaryAttachHandle.Attachments != null && primaryAttachHandle.Attachments.Count > 0)
        //    {
        //        //inherit attachments to de-duplicated handle
        //        var args = new FindAndModifyArgs
        //        {
        //            Query = Query.And(
        //                Query<DocumentReadModel>.EQ(x => x.Handle, handle)
        //            ),
        //            Update = Update<DocumentReadModel>
        //                .Set(x => x.Attachments, primaryAttachHandle.Attachments)
        //        };
        //        _collection.FindAndModify(args);

        //        if (Logger.IsDebugEnabled)
        //        {
        //            Logger.DebugFormat("Inherited Attachment: handle {0} for descriptorid {1} and primary Handle {2} [{3}]",
        //                handle, primaryAttachHandle.DocumentDescriptorId, primaryAttachHandle.Handle, projectedAt);
        //        }
        //    }
        //}

        public void UpdateCustomData(DocumentHandle handle, DocumentCustomData customData)
        {
            Logger.DebugFormat("UpdateCustomData on handle {0}", handle);
            var args = new FindAndModifyArgs
            {
                Query = Query.And(
                    Query<DocumentReadModel>.EQ(x => x.Handle, handle)
                ),
                Update = Update<DocumentReadModel>
                    .Set(x => x.CustomData, customData)
            };
            _collection.FindAndModify(args);
        }

        public void Delete(DocumentHandle handle, long projectedAt)
        {
            Logger.DebugFormat("Delete on handle {0} [{1}]", handle, projectedAt);
            var args = new FindAndRemoveArgs()
            {
                Query = Query.And(
                    Query<DocumentReadModel>.EQ(x => x.Handle, handle),
                    Query<DocumentReadModel>.LTE(x => x.CreatetAt, projectedAt)
                )
            };
            _collection.FindAndRemove(args);

            _collection.Update(
                Query.EQ("Attachments.Handle", BsonValue.Create(handle)),
                Update.Pull("Attachments", Query.EQ("Handle", BsonValue.Create(handle))),
                UpdateFlags.Multi
            );
        }

        public IQueryable<DocumentReadModel> AllSortedByHandle
        {
            get { return _collection.AsQueryable().OrderBy(x => x.Handle); }
        }

        public void CreateIfMissing(DocumentHandle handle, DocumentDescriptorId documentDescriptorId, long createdAt)
        {
            Logger.DebugFormat("CreateIfMissing on handle {0} [{1}]", handle, createdAt);
            var args = new FindAndModifyArgs
            {
                Query = Query<DocumentReadModel>
                    .EQ(x => x.Handle, handle),
                Update = Update<DocumentReadModel>
                    .SetOnInsert(x => x.CustomData, null)
                    .SetOnInsert(x => x.ProjectedAt, 0)
                    .SetOnInsert(x => x.DocumentDescriptorId, documentDescriptorId)
                    .SetOnInsert(x => x.CreatetAt, createdAt)
                    .SetOnInsert(x => x.FileName, null),
                Upsert = true
            };
            _collection.FindAndModify(args);
        }

        public void AddAttachment(DocumentHandle fatherHandle, DocumentHandle attachmentHandle)
        {
            Logger.DebugFormat("Adding attachment {1} on handle {0}", fatherHandle, attachmentHandle);
            var attachmentReadModel = this.FindOneById(attachmentHandle);
            String path = "";
            if (attachmentReadModel.CustomData != null &&
                attachmentReadModel.CustomData.ContainsKey(JobsConstants.AttachmentRelativePath)) 
            {
                path = attachmentReadModel.CustomData[JobsConstants.AttachmentRelativePath] as String;
            }
            _collection.Update
            (
                Query<DocumentReadModel>
                    .EQ(x => x.Handle, fatherHandle),
                Update<DocumentReadModel>
                    .AddToSet(x => x.Attachments,
                        new DocumentAttachmentReadModel(attachmentHandle, path))
            );
        }

        public DocumentReadModel FindOneById(DocumentHandle handle)
        {
            return _collection.FindOneById(BsonValue.Create(handle));
        }

        public void Drop()
        {
            _collection.Drop();
        }

        public void Init()
        {
            _collection.CreateIndex(IndexKeys<DocumentReadModel>.Ascending(x => x.Handle, x => x.CreatetAt));
        }

        public void Create(DocumentHandle documentHandle)
        {
            _collection.Insert(new DocumentReadModel(documentHandle));
        }



    }
}