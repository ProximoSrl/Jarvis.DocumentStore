using System;
using System.Collections.Generic;
using System.ComponentModel;
using Jarvis.DocumentStore.Core.Http;
using Jarvis.DocumentStore.Core.Storage;
using MongoDB.Bson.Serialization.Attributes;

namespace Jarvis.DocumentStore.Core.Model
{
    [BsonSerializer(typeof(FileIdSerializer))]
    [TypeConverter(typeof(FileIdTypeConverter))]
    public class FileId
    {
        static readonly IEqualityComparer<FileId> IdComparerInstance = new IdEqualityComparer();

        readonly string _id;

        public FileId(string id)
        {
            if (id == null) throw new ArgumentNullException("id");
            _id = id.ToLowerInvariant();
        }

        public static implicit operator string(FileId id)
        {
            return id._id;
        }

        public static IEqualityComparer<FileId> IdComparer
        {
            get { return IdComparerInstance; }
        }

        sealed class IdEqualityComparer : IEqualityComparer<FileId>
        {
            public bool Equals(FileId x, FileId y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x._id, y._id);
            }

            public int GetHashCode(FileId obj)
            {
                return (obj._id != null ? obj._id.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return _id;
        }
    }
}