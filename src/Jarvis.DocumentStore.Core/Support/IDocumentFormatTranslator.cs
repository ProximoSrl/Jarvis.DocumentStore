using Jarvis.DocumentStore.Core.Domain.DocumentDescriptor;
using System;
using System.Collections.Generic;
using System.IO;

namespace Jarvis.DocumentStore.Core.Support
{
    public interface IDocumentFormatTranslator
    {
        /// <summary>
        /// Given a name of a file, this method should return a valid <see cref="DocumentFormat" /> object.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        DocumentFormat GetFormatFromFileName(String fileName);
    }

    public class StandardDocumentFormatTranslator : IDocumentFormatTranslator 
    {
        private HashSet<String> imageFormatExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif"
        };

        public DocumentFormat GetFormatFromFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            if (".pdf".Equals(extension, StringComparison.OrdinalIgnoreCase)) return new DocumentFormat("pdf");

            if (imageFormatExtensions.Contains(extension)) return new DocumentFormat("rasterimage");

            return null;
        }
    }
}
