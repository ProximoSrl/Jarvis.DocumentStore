using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;
using System;
using System.Diagnostics;
using System.Threading;
using uno;
using uno.util;
using unoidl.com.sun.star.beans;
using unoidl.com.sun.star.frame;
using unoidl.com.sun.star.lang;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
namespace Jarvis.DocumentStore.Jobs.LibreOffice
{
    /// <summary>
    /// http://tinyway.wordpress.com/2011/03/30/how-to-convert-office-documents-to-pdf-using-open-office-in-c/
    /// </summary>
    public class LibreOfficeUnoConversion : ILibreOfficeConversion
    {
        private static readonly object LockRoot = new object();

        public ILogger Logger { get; set; }

        private readonly JobsHostConfiguration _config;

        public LibreOfficeUnoConversion(JobsHostConfiguration config)
        {
            //Needed by UNO SDK5. 
            //http://stackoverflow.com/questions/31856025/bootstrap-uno-api-libreoffice-exception
            //look at comments of funbit. We need to set UNO_PATH and soffice should be in the PATH
            //of the system.
            var sofficePath = config.GetPathToLibreOffice();
            var unoPath = Path.GetDirectoryName(sofficePath);
            Environment.SetEnvironmentVariable("UNO_PATH", unoPath, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + unoPath, EnvironmentVariableTarget.Process);

            _config = config;
            Logger = NullLogger.Instance;
        }

        public string Run(string sourceFile, string outType)
        {
            var outputFile = Path.ChangeExtension(sourceFile, outType);

            Logger.DebugFormat("UNO CONVERSION: Converting: {0} to {1}", sourceFile, outputFile);

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
            inputFile = SanitizeFileName(inputFile);
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
            catch (Exception ex)
            {
                Logger.Error($"Error during conversion of file {inputFile}", ex);
                throw;
            }
            finally
            {
                if (xComponent != null) xComponent.dispose();
            }
        }

        private static string SanitizeFileName(string inputFile)
        {
            if (inputFile.Contains("%"))
            {
                //percentage sign create trouble
                var sanitizeFileName = inputFile.Replace("%", "_");
                File.Move(inputFile, sanitizeFileName);
                inputFile = sanitizeFileName;
            }

            return inputFile;
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

            if (!result)
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
                throw new ArgumentNullException(nameof(file), "Null or empty path passed to OpenOffice");

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
                case ".xlsm":
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
