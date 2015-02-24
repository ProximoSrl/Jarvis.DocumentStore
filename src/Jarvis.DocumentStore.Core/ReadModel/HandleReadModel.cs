using System;
using System.Linq;
using System.Threading;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Handle;
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
    public class HandleReadModel : IReadModel
    {
        [BsonId]
        public DocumentHandle Handle { get; private set; }

        public HashSet<DocumentHandle> Attachments { get; private set; }
        
        public DocumentId DocumentId { get; private set; }
        
        public long CreatetAt { get; private set; }
        
        public long ProjectedAt { get; private set; }
        public HandleCustomData CustomData { get; private set; }
        public FileNameWithExtension FileName { get; private set; }

        public HandleReadModel(DocumentHandle handle) : this(handle, null, null, null)
        {

        }

        public HandleReadModel(DocumentHandle handle, DocumentId documentid, FileNameWithExtension fileName)
            : this(handle, documentid, fileName, null)
        {
        }

        public HandleReadModel(DocumentHandle handle, DocumentId documentid, FileNameWithExtension fileName, HandleCustomData customData)
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
        HandleReadModel FindOneById(DocumentHandle handle);
        void Drop();
        void Init();
        void LinkDocument(DocumentHandle handle, DocumentId id, long projectedAt);
        void UpdateCustomData(DocumentHandle handle, HandleCustomData customData);
        void Delete(DocumentHandle handle, long projectedAt);
        IQueryable<HandleReadModel> AllSortedByHandle { get;}
        void CreateIfMissing(DocumentHandle handle, DocumentHandle fatherHandle, long createdAt);
        void SetFileName(DocumentHandle handle, FileNameWithExtension fileName, long projectedAt);
        long Count();
    }

    public class HandleWriter : IHandleWriter
    {
        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        readonly MongoCollection<HandleReadModel> _collection;
        private ILogger _logger = NullLogger.Instance;

        public HandleWriter(MongoDatabase readModelDb)
        {
            _collection = readModelDb.GetCollection<HandleReadModel>(CollectionNames.GetCollectionName<HandleReadModel>());
        }

        public void Promise(DocumentHandle handle, long createdAt)
        {
            Logger.DebugFormat("Promise on handle {0} [{1}]", handle, createdAt);
            var args = new FindAndModifyArgs
            {
                Query = Query.And(
                    Query<HandleReadModel>.EQ(x => x.Handle, handle),
                    Query<HandleReadModel>.LT(x => x.ProjectedAt, createdAt)
                ),
                Update = Update<HandleReadModel>
                    .SetOnInsert(x => x.CustomData, null)
                    .SetOnInsert(x => x.ProjectedAt, 0)
                    .SetOnInsert(x => x.DocumentId, null)
                    .Set(x=>x.CreatetAt, createdAt)
                    .SetOnInsert(x => x.FileName, null),
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
                    Query<HandleReadModel>.EQ(x => x.Handle, handle),
                    Query<HandleReadModel>.LTE(x => x.CreatetAt, projectedAt)
                ),
                Update = Update<HandleReadModel>
                    .Set(x => x.FileName, fileName)
                    .Set(x => x.ProjectedAt, projectedAt)
            };
            _collection.FindAndModify(args);
        }

        public long Count()
        {
            return _collection.Count();
        }

        public void LinkDocument(DocumentHandle handle, DocumentId id, long projectedAt)
        {
            Logger.DebugFormat("LinkDocument on handle {0} [{1}]", handle, projectedAt);

            var args = new FindAndModifyArgs
            {
                Query = Query.And(
                    Query<HandleReadModel>.EQ(x => x.Handle, handle),
                    Query<HandleReadModel>.LTE(x => x.CreatetAt, projectedAt)
                ),
                Update = Update<HandleReadModel>
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

        public void UpdateCustomData(DocumentHandle handle, HandleCustomData customData)
        {
            Logger.DebugFormat("UpdateCustomData on handle {0}", handle);
            var args = new FindAndModifyArgs
            {
                Query = Query.And(
                    Query<HandleReadModel>.EQ(x => x.Handle, handle)
                ),
                Update = Update<HandleReadModel>
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
                    Query<HandleReadModel>.EQ(x => x.Handle, handle),
                    Query<HandleReadModel>.LTE(x => x.CreatetAt, projectedAt)
                )
            };
            _collection.FindAndRemove(args);
        }

        public IQueryable<HandleReadModel> AllSortedByHandle {
            get { return _collection.AsQueryable().OrderBy(x => x.Handle); }
        }

        public void CreateIfMissing(DocumentHandle handle, DocumentHandle fatherHandle, long createdAt)
        {
            Logger.DebugFormat("CreateIfMissing on handle {0} [{1}]", handle, createdAt);
            var args = new FindAndModifyArgs
            {
                Query = Query<HandleReadModel>
                    .EQ(x => x.Handle, handle),
                Update = Update<HandleReadModel>
                    .SetOnInsert(x => x.CustomData, null)
                    .SetOnInsert(x => x.ProjectedAt, 0)
                    .SetOnInsert(x => x.DocumentId, null)
                    .SetOnInsert(x => x.CreatetAt, createdAt)
                    .SetOnInsert(x => x.FileName, null),
                Upsert = true
            };
            _collection.FindAndModify(args);

            if (fatherHandle != null) 
            {
                _collection.Update(
                    Query.Or( 
                    Query<HandleReadModel>.EQ(x => x.Handle, fatherHandle),
                    Query<HandleReadModel>.EQ(x => x.Attachments, fatherHandle)),
                    Update<HandleReadModel>.Push(x => x.Attachments, handle),
                    UpdateFlags.Multi
                );
            }
        }

        public HandleReadModel FindOneById(DocumentHandle handle)
        {
            return _collection.FindOneById(BsonValue.Create(handle));
        }

        public void Drop()
        {
            _collection.Drop();
        }

        public void Init()
        {
            _collection.CreateIndex(IndexKeys<HandleReadModel>.Ascending(x => x.Handle, x => x.CreatetAt));
        }

        public void Create(DocumentHandle documentHandle)
        {
            _collection.Insert(new HandleReadModel(documentHandle));
        }
    }
}