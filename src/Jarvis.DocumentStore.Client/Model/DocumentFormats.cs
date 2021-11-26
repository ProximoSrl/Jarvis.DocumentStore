using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static readonly DocumentFormat RasterImage = new DocumentFormat("rasterimage");
        public static readonly DocumentFormat Pdf = new DocumentFormat("pdf");
        public static readonly DocumentFormat Original = new DocumentFormat("original");
        public static readonly DocumentFormat Email = new DocumentFormat("email");
        public static readonly DocumentFormat Tika = new DocumentFormat("tika");
        public static readonly DocumentFormat Content = new DocumentFormat("content");
        public static readonly DocumentFormat ZHtml = new DocumentFormat("zhtml");

        readonly IDictionary<DocumentFormat, Uri> _formats;

        public DocumentFormats(IDictionary<DocumentFormat, Uri> formats)
        {
            _formats = formats;
        }

        public bool HasFormat(DocumentFormat format)
        {
            return _formats.ContainsKey(format);
        }

        public Uri GetFormatUri(DocumentFormat format)
        {
            if (_formats.TryGetValue(format, out var formatUri))
            {
                return formatUri;
            }

            return null;
        }

        /// <summary>
        /// Clone content of the internal dictionary, returning a copy
        /// </summary>
        /// <returns></returns>
        public IDictionary<DocumentFormat, Uri> GetAllFormats() 
        {
            return new ReadOnlyDictionary<DocumentFormat, Uri>(_formats);
        }

        public Int32 Count
        {
            get { return _formats.Count; }
        }
    }
}
