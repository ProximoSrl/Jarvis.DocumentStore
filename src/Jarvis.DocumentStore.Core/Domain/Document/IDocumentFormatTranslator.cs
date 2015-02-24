using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Core.Domain.Document
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
