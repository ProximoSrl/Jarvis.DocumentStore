using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jarvis.DocumentStore.Core.Domain.Document
{

    public class DocumentCustomData : Dictionary<string, object>
    {
        public DocumentCustomData()
        {

        }

        public DocumentCustomData(Dictionary<string, object> original)
            : base(original)
        {

        }

        public static bool IsEquals(DocumentCustomData dic1, DocumentCustomData dic2)
        {
            if (dic1 == null && dic2 == null)
                return true;

            if (object.ReferenceEquals(dic1, dic2))
                return true;

            if (dic1 == null || dic2 == null)
                return false;

            return dic1.Count == dic2.Count && !dic1.Except(dic2).Any();
        }

        internal DocumentCustomData Clone()
        {
            DocumentCustomData cloned = new DocumentCustomData();
            foreach (var kvp in this)
            {
                cloned.Add(kvp.Key, kvp.Value);
            }
            return cloned;
        }
    }
}