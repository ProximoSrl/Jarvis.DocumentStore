using System;
using System.Linq;

namespace Jarvis.DocumentStore.Shared.Model
{
    public class DocumentContent : IEquatable<DocumentContent>
    {
        public const string MedatataTitle = "suggested-subject";
        public const string MetadataProtocolNumber = "suggested-protocolNo";
        public const string MetadataWithoutPageInfo = "jarvis-without-page-info";

        public static readonly DocumentContent NullContent = new DocumentContent(new DocumentPage[0], new MetadataHeader[0]);

        public DocumentContent(DocumentPage[] pages, MetadataHeader[] metadata)
        {
            Pages = pages;
            Metadata = metadata;
        }

        public MetadataHeader[] Metadata { get; protected set; }
        public DocumentPage[] Pages { get; protected set; }

        public void AddMetadata(string key, string value)
        {
            Metadata = Metadata.Union(new MetadataHeader[]
                {
                    new MetadataHeader(key, value)
                }
            ).ToArray();
        }

        public string SafeGetMetadata(string key)
        {
            if (Metadata == null)
                return null;

            var found = Metadata.FirstOrDefault(x => x.Name == key);
            return found != null ? found.Value : null;
        }

        public bool Equals(DocumentContent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Metadata.StructuralEquals(other.Metadata) && Pages.StructuralEquals(other.Pages);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DocumentContent)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Metadata != null ? Metadata.GetHashCode() : 0) * 397) ^ (Pages != null ? Pages.GetHashCode() : 0);
            }
        }

        public static bool operator ==(DocumentContent left, DocumentContent right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DocumentContent left, DocumentContent right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        ///     Rappresenta il testo della pagina
        /// </summary>
        public class DocumentPage : IEquatable<DocumentPage>
        {
            public DocumentPage(int pageNumber, string content)
            {
                PageNumber = pageNumber;
                Content = content;
            }

            /// <summary>
            ///     Indice della pagina in 1..n
            /// </summary>
            public int PageNumber { get; protected set; }

            /// <summary>
            ///     Contenuto html della pagina
            /// </summary>
            public string Content { get; protected set; }

            public bool Equals(DocumentPage other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return PageNumber == other.PageNumber && string.Equals(Content, other.Content);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((DocumentPage)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (PageNumber * 397) ^ (Content != null ? Content.GetHashCode() : 0);
                }
            }

            public static bool operator ==(DocumentPage left, DocumentPage right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(DocumentPage left, DocumentPage right)
            {
                return !Equals(left, right);
            }
        }

        /// <summary>
        ///     Metadato del documento
        /// </summary>
        public class MetadataHeader : IEquatable<MetadataHeader>
        {
            public MetadataHeader(string name, string value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; protected set; }
            public string Value { get; protected set; }

            public bool Equals(MetadataHeader other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Name, other.Name) && string.Equals(Value, other.Value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((MetadataHeader)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                }
            }

            public static bool operator ==(MetadataHeader left, MetadataHeader right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(MetadataHeader left, MetadataHeader right)
            {
                return !Equals(left, right);
            }
        }
    }
}