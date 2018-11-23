using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Jarvis.DocumentStore.Tools
{
    public class CheckOrphanedBlobs
    {
        private static DateTime _dateLimit;

        /// <summary>
        /// Execute the check.
        /// </summary>
        /// <param name="dateLimit">To avoid deleting formats orphaned caused by slow projection
        /// this parameter avoid to delete format newer than this value</param>
        internal static void PerformCheck(DateTime dateLimit)
        {
            Console.WriteLine("Check all queued tika job that have no original in document descriptor");

            _dateLimit = dateLimit;
            var urlReadModel = new MongoUrl(ConfigurationManager.AppSettings["mainDb"]);
            var clientReadModel = new MongoClient(urlReadModel);

            var dbReadModel = clientReadModel.GetServer().GetDatabase(urlReadModel.DatabaseName);
            MongoCollection<BsonDocument> _descriptorCollection = dbReadModel.GetCollection("rm.DocumentDescriptor");

            var allBlobs = _descriptorCollection.FindAll()
                .SetFields("Formats.v.BlobId");

            HashSet<String> allValidBlobs = new HashSet<string>();

            Int32 count = 0;
            foreach (var element in allBlobs)
            {
                var formats = (BsonArray)element["Formats"];
                foreach (var format in formats)
                {
                    allValidBlobs.Add(format["v"]["BlobId"].AsString);
                }
                count++;
                if (count % 100 == 0)
                {
                    Console.WriteLine("Scanned {0} descriptors." + count);
                }
            }

            Console.WriteLine("Found {0} valid formats in rm.DocumentDescriptor readmodel", allValidBlobs.Count);
            HashSet<String> blobsToDelete = CheckBlobStore(allValidBlobs, "oriFsDb", "original");
            PurgeOrphanedBlobs(blobsToDelete, "oriFsDb", "original");
            blobsToDelete = CheckBlobStore(allValidBlobs, "artFsDb", "tika");
            PurgeOrphanedBlobs(blobsToDelete, "artFsDb", "tika");
            blobsToDelete = CheckBlobStore(allValidBlobs, "artFsDb", "rasterimage");
            PurgeOrphanedBlobs(blobsToDelete, "artFsDb", "rasterimage");
            blobsToDelete = CheckBlobStore(allValidBlobs, "artFsDb", "thumb.small");
            PurgeOrphanedBlobs(blobsToDelete, "artFsDb", "thumb.small");
            blobsToDelete = CheckBlobStore(allValidBlobs, "artFsDb", "thumb.large");
            PurgeOrphanedBlobs(blobsToDelete, "artFsDb", "thumb.large");

            Console.WriteLine("Press a key to return to menu.");
            Console.ReadKey();
        }

        private static void PurgeOrphanedBlobs(HashSet<String> blobsToDelete, String connectionString, String format)
        {
            Console.WriteLine("Found {0} orphaned blobs in BlobStorage named {1}", blobsToDelete.Count, format);
            if (blobsToDelete.Count > 0)
            {
                Console.WriteLine("Press y if you want to delete them, any other key to list without deletion");
                var key = Console.ReadKey();
                Console.WriteLine();
                if (Char.ToLower(key.KeyChar) == 'y')
                {
                    var uri = new MongoUrl(ConfigurationManager.AppSettings[connectionString]);
                    var client = new MongoClient(uri);

                    var database = client.GetServer().GetDatabase(uri.DatabaseName);
                    var settings = new MongoGridFSSettings()
                    {
                        Root = format
                    };
                    var gridfs = database.GetGridFS(settings);
                    foreach (var blobToDelete in blobsToDelete)
                    {
                        gridfs.DeleteById(blobToDelete);
                        Console.WriteLine("Deleted {0} in database {1}", blobToDelete, ConfigurationManager.AppSettings[connectionString]);
                    }
                }
                else
                {
                    foreach (var blobToDelete in blobsToDelete)
                    {
                        Console.WriteLine("Blob {0} in database {1} is orphaned", blobToDelete, ConfigurationManager.AppSettings[connectionString]);
                    }
                }
            }
        }

        private static HashSet<String> CheckBlobStore(
            HashSet<String> allValidBlobs,
            String connectionString,
            String type)
        {
            var uri = new MongoUrl(ConfigurationManager.AppSettings[connectionString]);
            var client = new MongoClient(uri);

            var database = client.GetServer().GetDatabase(uri.DatabaseName);
            MongoCollection<BsonDocument> blobStoreCollection = database.GetCollection(type + ".files");

            var allOriginals = blobStoreCollection.FindAll();
            HashSet<String> blobToDelete = new HashSet<string>();

            foreach (var blob in allOriginals)
            {
                var id = blob["_id"].AsString;
                DateTime uploadDate = blob["uploadDate"].AsDateTime;
                if (!allValidBlobs.Contains(id) &&
                    uploadDate < _dateLimit)
                {
                    blobToDelete.Add(id);
                }
            }
            return blobToDelete;
        }
    }
}
