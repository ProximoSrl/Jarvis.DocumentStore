using System.Collections.Generic;
using System.Threading.Tasks;
using Jarvis.DocumentStore.Client.Model;
using Jarvis.DocumentStore.JobsHost.Helpers;
using Jarvis.DocumentStore.Shared.Jobs;

namespace Jarvis.DocumentStore.Jobs.Email
{
	public class AnalyzeEmailOutOfProcessJob : AbstractOutOfProcessPollerJob
	{
		public AnalyzeEmailOutOfProcessJob()
		{
			base.PipelineId = "email";
			base.QueueName = "email";
		}

		protected async override Task<ProcessResult> OnPolling(PollerJobParameters parameters, string workingFolder)
		{
			var task = new MailMessageToHtmlConverterTask()
			{
				Logger = Logger
			};

			string localFile = await DownloadBlob(
				parameters.TenantId,
				parameters.JobId,
				parameters.FileName,
				workingFolder).ConfigureAwait(false);

			var zipFile = task.Convert(parameters.JobId, localFile, workingFolder);

			await AddFormatToDocumentFromFile(
			  parameters.TenantId,
			  parameters.JobId,
			  new DocumentFormat(DocumentFormats.Email),
			  zipFile,
			  new Dictionary<string, object>()).ConfigureAwait(false);

			return ProcessResult.Ok;
		}
	}
}
