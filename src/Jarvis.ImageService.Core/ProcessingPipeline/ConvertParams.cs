using System;
using System.IO;
using GraphicsMagick;

namespace Jarvis.ImageService.Core.ProcessinPipeline
{
    public class ConvertParams
    {
        readonly Stream _sourceStream;
        readonly string _sourceFilename;

        public ConvertParams(Stream sourceStream, string sourceFilename, string destFileNamePattern)
        {
            if (sourceStream == null) throw new ArgumentNullException("sourceStream");
            if (sourceFilename == null) throw new ArgumentNullException("sourceFilename");
            if (destFileNamePattern == null) throw new ArgumentNullException("destFileNamePattern");

            _sourceStream = sourceStream;
            _sourceFilename = sourceFilename;
            FromPage = 1;
            Pages = 1;
            DestFileNamePattern = destFileNamePattern;
            Dpi = 72;
        }

        public Stream SourceStream
        {
            get { return _sourceStream; }
        }

        public string SourceFilename
        {
            get { return _sourceFilename; }
        }

        public int FromPage { get; set; }
        public int Pages { get; set; }
        public string DestFileNamePattern { get; set; }
        public int Dpi { get; set; }
        public MagickFormat? Format { get; set; }
    }
}