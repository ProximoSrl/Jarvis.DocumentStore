using System;
using System.IO;

namespace Jarvis.DocumentStore.Core.Domain.DocumentDescriptor
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

        public DocumentFormat GetFormatFromFileName(string fileName)
        {
            if (Path.GetExtension(fileName) == ".pdf") return new DocumentFormat("pdf");

            return null;
        }
    }
}
