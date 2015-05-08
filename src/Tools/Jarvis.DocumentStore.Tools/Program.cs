using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tools
{
    public class Program
    {
        static void Main(string[] args)
        {
            Menu();
            CommandLoop(c =>
            {
                switch (c)
                {
                    case "1":
                        CheckQueueScheduledJob();
                        break;
                    case "2":
                        CheckOrphanedBlobs.PerformCheck(DateTime.UtcNow);
                        break;
                    case "q":
                        return true;
                }

                Menu();
                return false;
            });
        }

        static void CommandLoop(Func<string, bool> action, String prompt = ">")
        {
            while (true)
            {
                Console.Write(prompt);
                var command = Console.ReadLine();
                if (command == null)
                    continue;

                command = command.ToLowerInvariant().Trim();

                if (action(command))
                    return;
            }
        }

        static void Message(string msg)
        {
            Console.WriteLine("");
            Console.WriteLine(msg);
            Console.WriteLine("(return to continue)");
            Console.ReadLine();
        }

        static void Banner(string title)
        {
            Console.WriteLine("-----------------------------");
            Console.WriteLine(title);
            Console.WriteLine("-----------------------------");
        }

        static void Menu()
        {
            Console.Clear();
            Banner("Menu");
            Console.WriteLine("1 - Check tika scheduled job");
            Console.WriteLine("2 - Find orphaned blobs");
            Console.WriteLine("");
            Console.WriteLine("Q - esci");
        }

        static void CheckQueueScheduledJob()
        {
            Console.WriteLine("Check all queued tika job that have no original in document descriptor");
            var urlQueue = new MongoUrl("mongodb://avalance,typhoon,earthquake/ds-queues");
            var clientQueue = new MongoClient(urlQueue);

            var dbQueue = clientQueue.GetServer().GetDatabase(urlQueue.DatabaseName);
            MongoCollection<BsonDocument> _queueCollection = dbQueue.GetCollection("queue.tika");

            HashSet<String> blobIdQueued = new HashSet<string>();
            var allBlobIdQueued = _queueCollection
                .FindAll()
                .SetFields("BlobId");

            foreach (var blobId in allBlobIdQueued)
            {
                blobIdQueued.Add(blobId["BlobId"].AsString);
            }

            var urlDs = new MongoUrl("mongodb://avalance,typhoon,earthquake/ds-docs");
            var clientDs = new MongoClient(urlDs);

            var dbDs = clientDs.GetServer().GetDatabase(urlDs.DatabaseName);
            MongoCollection<BsonDocument> _ddCollection = dbDs.GetCollection("rm.DocumentDescriptor");

            HashSet<String> blobIdDeDuplicated = new HashSet<string>();
            var allBlobIdDeDuplicated = _ddCollection
                .FindAll()
                .SetFields("Formats.v.BlobId");

            foreach (var blobId in allBlobIdDeDuplicated)
            {
                var blobIdOriginal = blobId["Formats"].AsBsonArray
                    .Select(b => b["v"]["BlobId"].AsString)
                    .Where(s => s.StartsWith("original"))
                    .Distinct();
                foreach (var id in blobIdOriginal)
                {
                    blobIdDeDuplicated.Add(id);
                }
                if (blobIdOriginal.Count() > 1)
                {
                    Console.WriteLine("Descriptor with more than one original {0}", blobId["_id"]);
                }
            }

            //now check
            Int32 nonExisting = 0;
            foreach (var blobId in blobIdQueued)
            {
                if (!blobIdDeDuplicated.Contains(blobId))
                {
                    Console.WriteLine("Blob {0} queued but it does not belongs to any descriptor", blobId);
                    nonExisting++;
                }
            }

            Console.WriteLine("Found {0} blob id in tika.queue that does not belongs to any descriptor", nonExisting);
            Console.WriteLine("Press a key to continue");
            Console.ReadKey();
        }
    }
}
