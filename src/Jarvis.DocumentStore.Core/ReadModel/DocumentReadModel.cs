using System.Linq;
using Castle.Core.Logging;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using Jarvis.DocumentStore.Core.Model;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

using MongoDB.Driver.Linq;
using System.Collections.Generic;
using System;
using Jarvis.DocumentStore.Shared.Jobs;
using Jarvis.Framework.Shared.Helpers;
using MongoDB.Driver.Linq;

namespace Jarvis.DocumentStore.Core.ReadModel
{
    public class DocumentReadModel : IReadModel
    {
        [BsonId]
        public DocumentHandle Handle { get; private set; }

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

    }

    public class DocumentWriter : IDocumentWriter
    {
        public ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        readonly IMongoCollection<DocumentReadModel> _collection;
        private ILogger _logger = NullLogger.Instance;

        public DocumentWriter(IMongoDatabase readModelDb)
        {
            _collection = readModelDb.GetCollection<DocumentReadModel>(CollectionNames.GetCollectionName<DocumentReadModel>());
        }

        public void Promise(DocumentHandle handle, long createdAt)
        {
            Logger.DebugFormat("Promise on handle {0} [{1}]", handle, createdAt);
            
            try
            {
                var result = _collection.FindOneAndUpdate(
                     Builders<DocumentReadModel>.Filter.And(
                        Builders<DocumentReadModel>.Filter.Eq(x => x.Handle, handle),
                        Builders<DocumentReadModel>.Filter.Lt(x => x.ProjectedAt, createdAt)
                    ),
                     Builders < DocumentReadModel >.Update
                        .SetOnInsert(x => x.CustomData, null)
                        .SetOnInsert(x => x.ProjectedAt, 0)
                        .Set(x => x.DocumentDescriptorId, null)
                        .Set(x => x.CreatetAt, createdAt)
                        .Set(x => x.FileName, null),
                     new FindOneAndUpdateOptions<DocumentReadModel, DocumentReadModel>()
                     {
                         ReturnDocument = ReturnDocument.After,
                         IsUpsert = true,
                     }
                    );

                if (Logger.IsDebugEnabled)
                {
                    Logger.DebugFormat("Promise on handle {0} [{1}] : {2}", handle, createdAt, result != null);
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
            var result = _collection.FindOneAndUpdate(
                    Builders<DocumentReadModel>.Filter.And(
                       Builders<DocumentReadModel>.Filter.Eq(x => x.Handle, handle),
                       Builders<DocumentReadModel>.Filter.Lte(x => x.CreatetAt, projectedAt)
                   ),
                    Builders<DocumentReadModel>.Update
                       .Set(x => x.FileName, fileName)
                        .Set(x => x.ProjectedAt, projectedAt),
                    new FindOneAndUpdateOptions<DocumentReadModel, DocumentReadModel>()
                    {
                        ReturnDocument = ReturnDocument.After
                    }
                   );

        }

        public long Count()
        {
            return _collection.AsQueryable().Count();
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
        }

        private Boolean InnerCreateLinkToDocument(DocumentHandle handle, DocumentDescriptorId id, Boolean? deDuplication, long projectedAt)
        {
            Logger.DebugFormat("LinkDocument on handle {0} [{1}]", handle, projectedAt);

            var result = _collection.FindOneAndUpdate(
               Builders<DocumentReadModel>.Filter.And(
                  Builders<DocumentReadModel>.Filter.Eq(x => x.Handle, handle),
                  Builders<DocumentReadModel>.Filter.Ne(x => x.DocumentDescriptorId, id),
                  Builders<DocumentReadModel>.Filter.Lte(x => x.CreatetAt, projectedAt)
              ),
               Builders<DocumentReadModel>.Update
                   .Set(x => x.DocumentDescriptorId, id)
                   .Set(x => x.ProjectedAt, projectedAt),
            new FindOneAndUpdateOptions<DocumentReadModel, DocumentReadModel>()
               {
                   ReturnDocument = ReturnDocument.After
               }
              );

            if (Logger.IsDebugEnabled)
            {
                Logger.DebugFormat("LinkDocument on handle {0} [{1}] : {2}", handle, projectedAt, result != null);
            }
            return result != null;
        }

        


        public void UpdateCustomData(DocumentHandle handle, DocumentCustomData customData)
        {
            Logger.DebugFormat("UpdateCustomData on handle {0}", handle);

            var result = _collection.FindOneAndUpdate(
               Builders<DocumentReadModel>.Filter.Eq(x => x.Handle, handle),
               Builders<DocumentReadModel>.Update.Set(x => x.CustomData, customData),
            new FindOneAndUpdateOptions<DocumentReadModel, DocumentReadModel>()
            {
                ReturnDocument = ReturnDocument.After
            });
        }

        public void Delete(DocumentHandle handle, long projectedAt)
        {
            Logger.DebugFormat("Delete on handle {0} [{1}]", handle, projectedAt);

            var result = _collection.FindOneAndDelete(
               Builders<DocumentReadModel>.Filter.And(
                  Builders<DocumentReadModel>.Filter.Eq(x => x.Handle, handle),
                  Builders<DocumentReadModel>.Filter.Lte(x => x.CreatetAt, projectedAt)
              ));

        }

        public IQueryable<DocumentReadModel> AllSortedByHandle
        {
            get { return _collection.AsQueryable().OrderBy(x => x.Handle); }
        }

        public void CreateIfMissing(DocumentHandle handle, DocumentDescriptorId documentDescriptorId, long createdAt)
        {
            Logger.DebugFormat("CreateIfMissing on handle {0} [{1}]", handle, createdAt);
           
            var result = _collection.FindOneAndUpdate(
                Builders<DocumentReadModel>.Filter.Eq(x => x.Handle, handle),
                 Builders<DocumentReadModel>.Update
                    .SetOnInsert(x => x.CustomData, null)
                    .SetOnInsert(x => x.ProjectedAt, 0)
                    .SetOnInsert(x => x.DocumentDescriptorId, documentDescriptorId)
                    .SetOnInsert(x => x.CreatetAt, createdAt)
                    .SetOnInsert(x => x.FileName, null),
          new FindOneAndUpdateOptions<DocumentReadModel, DocumentReadModel>()
          {
              ReturnDocument = ReturnDocument.After,
              IsUpsert = true
          });
        }

        public DocumentReadModel FindOneById(DocumentHandle handle)
        {
            return _collection.Find(Builders<DocumentReadModel>.Filter.Eq(d => d.Handle, handle)).SingleOrDefault();
        }

        public void Drop()
        {
            _collection.Drop();
        }

        public void Init()
        {
            _collection.Indexes.CreateOne(Builders<DocumentReadModel>.IndexKeys.Ascending(x => x.Handle).Ascending(x => x.CreatetAt));
        }

        public void Create(DocumentHandle documentHandle)
        {
            _collection.InsertOne(new DocumentReadModel(documentHandle));
        }



    }
}