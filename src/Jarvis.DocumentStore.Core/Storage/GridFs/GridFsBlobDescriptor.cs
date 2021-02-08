using Jarvis.DocumentStore.Core.Model;
using MongoDB.Driver.GridFS;
using System;
using System.IO;
using System.Linq;

namespace Jarvis.DocumentStore.Core.Storage.GridFs
{
    public class GridFsBlobDescriptor : IBlobDescriptor
    {
        public const string ReferenceMarker = "ref-info|";

        private readonly MongoGridFSFileInfo _mongoGridFsFileInfo;

        public GridFsBlobDescriptor(BlobId blobId, MongoGridFSFileInfo mongoGridFsFileInfo)
        {
            if (mongoGridFsFileInfo == null)
            {
                throw new ArgumentNullException(nameof(mongoGridFsFileInfo));
            }

            _mongoGridFsFileInfo = mongoGridFsFileInfo;
            BlobId = blobId;

            FileNameWithExtension = new FileNameWithExtension(_mongoGridFsFileInfo.Name);
            IsReference = mongoGridFsFileInfo.Aliases?.Any(a => a.StartsWith(ReferenceMarker)) == true;
        }

        public BlobId BlobId { get; private set; }

        public Stream OpenRead()
        {
            var alias = _mongoGridFsFileInfo.Aliases?.FirstOrDefault(a => a.StartsWith(ReferenceMarker));
            if (alias != null)
            {
                var reference = ReferenceInfo.FromString(alias.Substring(ReferenceMarker.Length));
                return new FileStream(reference.FileStorage, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            //we save reference 
            return _mongoGridFsFileInfo.OpenRead();
        }

        public FileNameWithExtension FileNameWithExtension { get; private set; }

        public string ContentType
        {
            get { return _mongoGridFsFileInfo.ContentType; }
        }

        public FileHash Hash
        {
            get { return new FileHash(_mongoGridFsFileInfo.MD5); }
        }

        public long Length
        {
            get { return _mongoGridFsFileInfo.Length; }
        }

        /// <summary>
        /// When we are in Gridfs we assume that the content is there (noone should
        /// have be tampered with database content).
        /// </summary>
        public bool Exists => true;

        /// <summary>
        /// True if it is a reference blob
        /// </summary>
        public bool IsReference { get; private set; }

        internal class ReferenceInfo
        {
            public ReferenceInfo(string fileStorage, string md5, long length)
            {
                FileStorage = fileStorage;
                Md5 = md5;
                Length = length;
            }

            private ReferenceInfo()
            {
            }

            public static ReferenceInfo FromStream(Stream stream)
            {
                ReferenceInfo retValue = new ReferenceInfo();
                using (var br = new BinaryReader(stream))
                {
                    retValue.Length = br.ReadInt64();
                    retValue.Md5 = br.ReadString();
                    retValue.FileStorage = br.ReadString();
                }

                return retValue;
            }

            public static ReferenceInfo FromString(string str)
            {
                var splitted = str.Split('|');
                ReferenceInfo retValue = new ReferenceInfo();

                retValue.Length = Int64.Parse(splitted[1]);
                retValue.Md5 = splitted[0];
                retValue.FileStorage = splitted[2];

                return retValue;
            }

            /// <summary>
            /// Location of the reference file, this is the real storage where the file is located.
            /// </summary>
            public string FileStorage { get; private set; }

            public string Md5 { get; private set; }

            public Int64 Length { get; private set; }

            public string ConvertToString()
            {
                return $"{Md5}|{Length}|{FileStorage}";
            }

            public void SerialSize(Stream destinationStream)
            {
                using (var bw = new BinaryWriter(destinationStream))
                {
                    bw.Write(Length);
                    bw.Write(Md5);
                    bw.Write(FileStorage);
                }
            }
        }
    }
}