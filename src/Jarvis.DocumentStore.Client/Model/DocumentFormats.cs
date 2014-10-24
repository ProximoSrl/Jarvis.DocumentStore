using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Jarvis.DocumentStore.Client.Model
{
    [TypeConverter(typeof(StringValueTypeConverter<DocumentFormat>))]
    public class DocumentFormat : LowercaseClientAbstractStringValue
    {
        public DocumentFormat(string value) : base(value)
        {
        }
    }

    [TypeConverter(typeof(StringValueTypeConverter<DocumentHandle>))]
    public class DocumentHandle : LowercaseClientAbstractStringValue
    {
        public DocumentHandle(string value) : base(value)
        {
        }

        public static DocumentHandle FromString(string handle)
        {
            return new DocumentHandle(handle);
        }
    }

    public class DocumentFormats
    {
        readonly IDictionary<DocumentFormat, Uri> _formats;

        public DocumentFormats(IDictionary<DocumentFormat, Uri> formats)
        {
            _formats = formats;
        }

        public bool HasFormat(DocumentFormat format)
        {
            return _formats.ContainsKey(format);
        }
    }
}
