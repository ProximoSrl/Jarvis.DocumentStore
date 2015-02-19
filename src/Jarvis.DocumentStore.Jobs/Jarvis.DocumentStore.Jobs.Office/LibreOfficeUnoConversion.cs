using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;
using uno;
using uno.util;
using unoidl.com.sun.star.beans;
using unoidl.com.sun.star.frame;
using unoidl.com.sun.star.lang;

namespace Jarvis.DocumentStore.Jobs.Office
{
    /// <summary>
    /// http://tinyway.wordpress.com/2011/03/30/how-to-convert-office-documents-to-pdf-using-open-office-in-c/
    /// </summary>
    public class LibreOfficeUnoConversion : ILibreOfficeConversion
    {
        static readonly object LockRoot = new object();

        public ILogger Logger { get; set; }

        readonly JobsHostConfiguration _config;

        public LibreOfficeUnoConversion(JobsHostConfiguration config)
        {
            _config = config;
        }

        public string Run(string sourceFile, string outType)
        {
            var outputFile = Path.ChangeExtension(sourceFile, outType);

            Logger.DebugFormat("Converting: {0} to {1}", sourceFile, outputFile);

            lock (LockRoot) // -> single runner (todo: more user profiles)
            {
                ConvertToPdf(sourceFile, outputFile);
            }

            if (!File.Exists(outputFile))
                throw new Exception("Conversion failed");


            return outputFile;
        }

        public void Initialize()
        {
            CloseOpenOffice();
        }

        public void ConvertToPdf(string inputFile, string outputFile)
        {
            if (ConvertExtensionToFilterType(Path.GetExtension(inputFile)) == null)
                throw new InvalidProgramException("Unknown file type for OpenOffice. File = " + inputFile);

            StartOpenOffice();

            //Get a ComponentContext
            var xLocalContext = Bootstrap.bootstrap();
            //Get MultiServiceFactory
            var xRemoteFactory = (XMultiServiceFactory)xLocalContext.getServiceManager();
            //Get a CompontLoader
            var aLoader = (XComponentLoader)xRemoteFactory.createInstance("com.sun.star.frame.Desktop");
            //Load the sourcefile

            XComponent xComponent = null;
            try
            {
                xComponent = InitDocument(aLoader, PathConverter(inputFile), "_blank");
                //Wait for loading
                while (xComponent == null)
                {
                    Thread.Sleep(1000);
                }

                // save/export the document
                SaveDocument(xComponent, inputFile, PathConverter(outputFile));
            }
            finally
            {
                if (xComponent != null) xComponent.dispose();
            }
        }

        public void CloseOpenOffice()
        {
            var ps = Process.GetProcessesByName("soffice.bin");
            foreach (var process in ps)
            {
                Logger.DebugFormat("Closing openoffice pid: {0}", process.Id);
                process.Kill();
            }

            ps = Process.GetProcessesByName(_config.GetPathToLibreOffice());
            foreach (var process in ps)
            {
                Logger.DebugFormat("Closing openoffice pid: {0}", process.Id);
                process.Kill();
            }
        }

        private void StartOpenOffice()
        {
            var ps = Process.GetProcessesByName("soffice");
            if (ps.Length > 0)
                return;

            var pathToLibreOffice = _config.GetPathToLibreOffice();
            var p = new Process
            {
                StartInfo =
                {
                    Arguments = "-headless -nofirststartwizard",
                    FileName = pathToLibreOffice,
                    CreateNoWindow = true
                }
            };

            Logger.DebugFormat(
                "Starting liberoffice headless {0} {1}",
                p.StartInfo.FileName,
                p.StartInfo.Arguments
            );

            var result = p.Start();

            if (result == false)
                throw new InvalidProgramException("OpenOffice failed to start.");
        }

        private XComponent InitDocument(XComponentLoader aLoader, string file, string target)
        {
            var openProps = new PropertyValue[1];
            openProps[0] = new PropertyValue { Name = "Hidden", Value = new Any(true) };

            var xComponent = aLoader.loadComponentFromURL(
                file, target, 0,
                openProps);

            return xComponent;
        }

        private void SaveDocument(XComponent xComponent, string sourceFile, string destinationFile)
        {
            var propertyValues = new PropertyValue[2];
            // Setting the flag for overwriting
            propertyValues[1] = new PropertyValue { Name = "Overwrite", Value = new Any(true) };
            //// Setting the filter name
            propertyValues[0] = new PropertyValue
            {
                Name = "FilterName",
                Value = new Any(ConvertExtensionToFilterType(Path.GetExtension(sourceFile)))
            };
            ((XStorable)xComponent).storeToURL(destinationFile, propertyValues);
        }

        private string PathConverter(string file)
        {
            if (string.IsNullOrEmpty(file))
                throw new NullReferenceException("Null or empty path passed to OpenOffice");

            return String.Format("file:///{0}", file.Replace(@"\", "/"));
        }

        public string ConvertExtensionToFilterType(string extension)
        {
            switch (extension)
            {
                case ".doc":
                case ".docx":
                case ".txt":
                case ".rtf":
                case ".html":
                case ".htm":
                case ".xml":
                case ".odt":
                case ".wps":
                case ".wpd":
                    return "writer_pdf_Export";
                case ".xls":
                case ".xlsb":
                case ".xlsx":
                case ".ods":
                    return "calc_pdf_Export";
                case ".ppt":
                case ".ppsx":
                case ".pptx":
                case ".odp":
                    return "impress_pdf_Export";

                default:
                    return null;
            }
        }



    }
}
