using System;
using System.Drawing.Printing;
using System.IO;
using System.IO.Compression;
using Castle.Core.Logging;
using Jarvis.DocumentStore.JobsHost.Support;
using TuesPechkin;
using Path = Jarvis.DocumentStore.Shared.Helpers.DsPath;
using File = Jarvis.DocumentStore.Shared.Helpers.DsFile;
using System.Linq;

namespace Jarvis.DocumentStore.Jobs.HtmlZipOld
{
	/// <summary>
	/// Version that does not need blob store.
	/// </summary>
	public class HtmlToPdfConverterFromDiskFileOld
	{
		const bool ProduceOutline = false;
		private String _inputFileName;
		public ILogger Logger { get; set; }
		readonly JobsHostConfiguration _config;

		private String[] unzippedHtmlExtension = new[] { ".html", ".htm" };

		public HtmlToPdfConverterFromDiskFileOld(String inputFileName, JobsHostConfiguration config)
		{
			_inputFileName = inputFileName;
			_config = config;
		}

		private Boolean IsUnzippedHtmlFile()
		{
			var fileExtension = Path.GetExtension(_inputFileName);
			return unzippedHtmlExtension.Any(s => s.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Elaborate
		/// </summary>
		/// <param name="tenantId"></param>
		/// <param name="jobId"></param>
		/// <returns></returns>
		public String Run(String jobId)
		{
			Logger.DebugFormat("Converting {0} to pdf", jobId);
			var localFileName = DownloadLocalCopy(jobId);

            var sanitizer = new SafeHtmlConverter(localFileName)
            {
                Logger = Logger
            };
            localFileName = sanitizer.Run(jobId);

            var outputFileName = localFileName + ".pdf";
			var uri = new Uri(localFileName);

			var document = new HtmlToPdfDocument
			{
				GlobalSettings =
				{
					ProduceOutline = ProduceOutline,
					PaperSize = PaperKind.A4, // Implicit conversion to PechkinPaperSize
                    Margins =
					{
						All = 1.375,
						Unit = Unit.Centimeters
					},
					OutputFormat = GlobalSettings.DocumentOutputFormat.PDF
				},
				Objects = {
					new ObjectSettings
					{
						PageUrl = uri.AbsoluteUri,
						WebSettings = new WebSettings()
						{
							EnableJavascript = false,
							PrintMediaType = false
						}
					},
				}
			};

			var converter = Factory.Create();
			var pdf = converter.Convert(document);

			File.WriteAllBytes(outputFileName, pdf);

			Logger.DebugFormat("Deleting {0}", localFileName);
			File.Delete(localFileName);
			Logger.DebugFormat("Conversion of {0} to pdf done!", jobId);

			return outputFileName;
		}

		string DownloadLocalCopy(String jobId)
		{
			Logger.DebugFormat("Downloaded {0}", _inputFileName);

			if (IsUnzippedHtmlFile()) return _inputFileName;

			var workingFolder = Path.GetDirectoryName(_inputFileName);
			ZipFile.ExtractToDirectory(_inputFileName, workingFolder);
			Logger.DebugFormat("Extracted zip to {0}", workingFolder);

			var originalFileWithoutExtension = Path.GetFileNameWithoutExtension(_inputFileName);

			var extractedFiles = Directory.GetFiles(workingFolder, "*.htm*");
			String htmlFile = null;
			if (extractedFiles.Length == 1)
			{
				htmlFile = extractedFiles[0];
			}
			else if (extractedFiles.Length > 1)
			{
				htmlFile = extractedFiles
					.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).TrimEnd('.') == originalFileWithoutExtension) ??
					extractedFiles[0];
			}

			if (htmlFile != null)
			{
				Logger.Debug($"Extracted html from {_inputFileName} is {htmlFile}");
				return htmlFile;
			}

			var msg = $"Html file not found for {jobId} {_inputFileName}";
			Logger.Error(msg);
			throw new Exception(msg);
		}
	}
}
