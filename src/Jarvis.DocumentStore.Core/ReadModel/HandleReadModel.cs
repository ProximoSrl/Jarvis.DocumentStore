using CQRS.Shared.ReadModel;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.Handle;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class HandleReadModel : IReadModel
    {
        [BsonId]
        public DocumentHandle Handle { get; private set; }
        
        public DocumentId DocumentId { get; private set; }
        
        public long CreatetAt { get; private set; }
        
        public long ProjectedAt { get; private set; }
        public HandleCustomData CustomData { get; private set; }

        public HandleReadModel(DocumentHandle handle)
        {
            Handle = handle;
        }

        public bool IsPending()
        {
            return this.CreatetAt > this.ProjectedAt;
        }
    }

    public interface IHandleWriter
    {
        void Promise(DocumentHandle handle, DocumentId id, long createdAt);
        HandleReadModel Get(DocumentHandle handle);
        void Drop();
        void Init();
        void ConfirmLink(DocumentHandle handle, DocumentId id, long projectedAt);
        void UpdateCustomData(DocumentHandle handle, HandleCustomData customData);
    }

    public class HandleWriter : IHandleWriter
    {
        readonly MongoCollection<HandleReadModel> _collection;

        public HandleWriter(MongoDatabase readModelDb)
        {
            _collection = readModelDb.GetCollection<HandleReadModel>(CollectionNames.GetCollectionName<HandleReadModel>());
        }

        public void Promise(DocumentHandle handle, DocumentId id, long createdAt)
        {
            var args = new FindAndModifyArgs
            {
                Query = Query<HandleReadModel>
                    .EQ(x => x.Handle, handle), 
                Update = Update<HandleReadModel>
                    .Set(x=>x.DocumentId, id)
                    .Set(x=>x.CreatetAt, createdAt),
                Upsert = true
            };
            _collection.FindAndModify(args);
        }

        public void ConfirmLink(DocumentHandle handle, DocumentId id, long projectedAt)
        {
            var args = new FindAndModifyArgs
            {
                Query = Query.And(
                    Query<HandleReadModel>.EQ(x => x.Handle, handle),
                    Query<HandleReadModel>.LTE(x => x.CreatetAt, projectedAt)
                ),
                Update = Update<HandleReadModel>
                    .Set(x => x.DocumentId, id)
                    .Set(x => x.ProjectedAt, projectedAt)
            };
            _collection.FindAndModify(args);
        }

        public void UpdateCustomData(DocumentHandle handle, HandleCustomData customData)
        {
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

        public HandleReadModel Get(DocumentHandle handle)
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