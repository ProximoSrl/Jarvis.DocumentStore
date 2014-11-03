using Jarvis.DocumentStore.Core.Domain.Document;

namespace Jarvis.DocumentStore.Core.Processing
{
    public static class DocumentFormats
    {
        public static readonly DocumentFormat RasterImage = new DocumentFormat("RasterImage");
        public static readonly DocumentFormat Pdf = new DocumentFormat("pdf");
        public static readonly DocumentFormat Original = new DocumentFormat("original");
        public static readonly DocumentFormat Email = new DocumentFormat("email");
        public static readonly DocumentFormat Tika = new DocumentFormat("tika");
        public static readonly DocumentFormat ZHtml = new DocumentFormat("zhtml");
    }
}