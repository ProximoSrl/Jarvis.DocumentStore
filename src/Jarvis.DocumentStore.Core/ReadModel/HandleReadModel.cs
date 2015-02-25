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

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class DocumentReadModel : IReadModel
    {
        [BsonId]
        public DocumentHandle Handle { get; private set; }

        public HashSet<DocumentHandle> Attachments { get; private set; }
        
        public DocumentDescriptorId DocumentId { get; private set; }
        
        public long CreatetAt { get; private set; }
        
        public long ProjectedAt { get; private set; }
        public DocumentCustomData CustomData { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public DocumentReadModel(DocumentHandle handle) : this(handle, null, null, null)
        {

        }

        public DocumentReadModel(DocumentHandle handle, DocumentDescriptorId documentid, FileNameWithExtension fileName)
            : this(handle, documentid, fileName, null)
        {
        }

        public DocumentReadModel(DocumentHandle handle, DocumentDescriptorId documentid, FileNameWithExtension fileName, DocumentCustomData customData)
        {
            Handle = handle;
            DocumentId = documentid;
            FileName = fileName;
            CustomData = customData;
        }

        public bool IsPending()
        {
            return this.CreatetAt > this.ProjectedAt;
        }
    }

    public interface IHandleWriter
    {
        void Promise(DocumentHandle handle, long createdAt);
        DocumentReadModel FindOneById(DocumentHandle handle);
        void Drop();
        void Init();
        void LinkDocument(DocumentHandle handle, DocumentDescriptorId id, long projectedAt);
        void UpdateCustomData(DocumentHandle handle, DocumentCustomData customData);
        void Delete(DocumentHandle handle, long projectedAt);
        IQueryable<DocumentReadModel> AllSortedByHandle { get;}
        void CreateIfMissing(DocumentHandle handle, long createdAt);
        void SetFileName(DocumentHandle handle, FileNameWithExtension fileName, long projectedAt);
        long Count();

        void AddAttachment(DocumentHandle fatherHandle, DocumentHandle attachmentHandle);
    }

    public class HandleWriter : IHandleWriter
    {
        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        readonly MongoCollection<DocumentReadModel> _collection;
        private ILogger _logger = NullLogger.Instance;

        public HandleWriter(MongoDatabase readModelDb)
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
                    .Set(x => x.DocumentId, null)
                    .Set(x=>x.CreatetAt, createdAt)
                    .Set(x=>x.FileName, null),
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
            Logger.DebugFormat("LinkDocument on handle {0} [{1}]", handle, projectedAt);

            var args = new FindAndModifyArgs
            {
                Query = Query.And(
                    Query<DocumentReadModel>.EQ(x => x.Handle, handle),
                    Query<DocumentReadModel>.LTE(x => x.CreatetAt, projectedAt)
                ),
                Update = Update<DocumentReadModel>
                    .Set(x => x.DocumentId, id)
                    .Set(x => x.ProjectedAt, projectedAt),
                VersionReturned = FindAndModifyDocumentVersion.Modified
            };
            var result = _collection.FindAndModify(args);
            
            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("LinkDocument on handle {0} [{1}] : {2}", handle, projectedAt,
                    result.ModifiedDocument != null ? result.ModifiedDocument.ToJson() : "null");
            }
        }

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
        }

        public IQueryable<DocumentReadModel> AllSortedByHandle {
            get { return _collection.AsQueryable().OrderBy(x => x.Handle); }
        }

        public void CreateIfMissing(DocumentHandle handle, long createdAt)
        {
            Logger.DebugFormat("CreateIfMissing on handle {0} [{1}]", handle, createdAt);
            var args = new FindAndModifyArgs
            {
                Query = Query<DocumentReadModel>
                    .EQ(x => x.Handle, handle),
                Update = Update<DocumentReadModel>
                    .SetOnInsert(x => x.CustomData, null)
                    .SetOnInsert(x => x.ProjectedAt, 0)
                    .SetOnInsert(x => x.DocumentId, null)
                    .SetOnInsert(x => x.CreatetAt, createdAt)
                    .SetOnInsert(x => x.FileName, null),
                Upsert = true
            };
            _collection.FindAndModify(args);
        }

        public void AddAttachment(DocumentHandle fatherHandle, DocumentHandle attachmentHandle)
        {
            Logger.DebugFormat("Adding attachment {1} on handle {0}", fatherHandle, attachmentHandle);
            var args = new FindAndModifyArgs
            {
                Query = Query<DocumentReadModel>
                    .EQ(x => x.Handle, fatherHandle),
                Update = Update<DocumentReadModel>
                    .AddToSet(x => x.Attachments, attachmentHandle),
                Upsert = false
            };
            _collection.FindAndModify(args);
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