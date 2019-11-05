using Castle.Core;
using Castle.Core.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Jobs.MsOffice
{
    /// <summary>
    /// Base converter class to avoid problem using office automation.
    /// </summary>
    public abstract class BaseConverter : IStartable
    {
        /// <summary>
        /// We really do not need a collection, we will execute one job at a time, but blocking
        /// collection is the standard way to implement producer consumer.
        /// </summary>
        protected BlockingCollection<JobData> jobs = new BlockingCollection<JobData>();
        protected Thread thread;
        protected CancellationTokenSource TokenSource = new CancellationTokenSource();

        public ILogger Logger { get; set; } = NullLogger.Instance;

        protected BaseConverter()
        {
            //Start a STAThread to dequeue jobs.
            thread = new Thread(() =>
            {
                try
                {
                    foreach (var job in jobs.GetConsumingEnumerable(TokenSource.Token))
                    {
                        String result = String.Empty;
                        try
                        {
                            result = OnRunJob(job);
                        }
                        catch (Exception ex)
                        {
                            Logger.ErrorFormat(ex, "Error running job for converting file {0} - {1} ", Path.GetFileName(job.SourceFile), ex.Message);
                            result = String.Format("Error running job for converting file {0} - {1} ", Path.GetFileName(job.SourceFile), ex.Message);
                        }
                        job.TaskCompletionSource.SetResult(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    //ignore the exception.
                }

                Logger.InfoFormat("Exiting conversion thread");
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start(); //immediately start the thread
        }

        /// <summary>
        /// Queue a job and return a standard task to wait for completion, all the work is done inside the 
        /// OnRunJob method, no different code should be run.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destinationFile"></param>
        /// <returns></returns>
        protected Task<String> QueueJob(String sourceFile, String destinationFile)
        {
            var job = new JobData(sourceFile, destinationFile);
            jobs.Add(job);
            return job.TaskCompletionSource.Task;
        }

        /// <summary>
        /// This is the real function that should be implemented to perform the job in a STA
        /// thread, once at a time, etc.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        protected abstract String OnRunJob(JobData job);

        public void Stop()
        {
            jobs.CompleteAdding();
            TokenSource.Cancel();
        }

        public void Start()
        {
        }

        protected class JobData
        {
            public JobData(string sourceFile, string destinationFile)
            {
                SourceFile = sourceFile;
                DestinationFile = destinationFile;
                TaskCompletionSource = new TaskCompletionSource<String>();
            }

            public String SourceFile { get; }

            public String DestinationFile { get; }

            public TaskCompletionSource<String> TaskCompletionSource { get; }
        }
    }
}
