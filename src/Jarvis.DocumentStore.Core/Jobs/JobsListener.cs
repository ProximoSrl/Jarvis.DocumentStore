using System;
using Castle.Core.Logging;
using CQRS.Kernel.MultitenantSupport;
using CQRS.Shared.MultitenantSupport;
using Jarvis.DocumentStore.Core.Model;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Newtonsoft.Json;
using Quartz;

namespace Jarvis.DocumentStore.Core.Jobs
{
    public class JobsListener : IJobListener
    {
        readonly IExtendedLogger _logger;
        readonly MongoCollection<JobTracker> _trackerCollection;

        public JobsListener(IExtendedLogger logger, MongoDatabase db)
        {
            _logger = logger;
            _trackerCollection = db.GetCollection<JobTracker>("joblog");
        }

        public void JobToBeExecuted(IJobExecutionContext context)
        {
            if (context.JobDetail.JobDataMap.ContainsKey(JobKeys.TenantId))
            {
                TenantContext.Enter(new TenantId(context.JobDetail.JobDataMap.GetString(JobKeys.TenantId)));
            
            }
            else
            {
                TenantContext.Exit();
            }
        }

        public void JobExecutionVetoed(IJobExecutionContext context)
        {
           
        }

        public void JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException)
        {
            _logger.DebugFormat("Handling job post-execution {0}", context.JobDetail.JobType);
            
            string message = jobException != null ? jobException.GetBaseException().Message : null;

            _trackerCollection.Update(
                Query.EQ("_id", context.JobDetail.Key.ToBsonDocument()),
                Update<JobTracker>
                    .Inc(x => x.Elapsed, DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)
                    .Set(x=>x.Message, message)
            );

            if (jobException != null)
            {
                HandleErrors(context, jobException);
            }
        }

        void HandleErrors(IJobExecutionContext context, JobExecutionException jobException)
        {
            jobException.UnscheduleAllTriggers = true;

            var ex = jobException.GetBaseException();
            var retries = context.Trigger.JobDataMap.GetIntValue("_retrycount") + 1;

            _logger.ThreadProperties["job-data-map"] = context.JobDetail.JobDataMap;
            _logger.ErrorFormat(ex, "Job id: {0}.{1} refire count {2}", 
                context.JobDetail.Key.Group, 
                context.JobDetail.Key.Name,
                retries
            );

            try
            {
                if (retries < 5)
                {
                    var rescheduleAt = DateTime.Now.AddSeconds(5 * retries);
                    _logger.DebugFormat("Rescheduling job {0} at {1}", context.JobDetail.Key, rescheduleAt);
                    context.Scheduler.RescheduleJob(
                        context.Trigger.Key,
                        TriggerBuilder
                            .Create()
                            .UsingJobData("_retrycount", retries)
                            .StartAt(rescheduleAt)
                            .Build()
                        );
                }
                else
                {
                    _logger.ErrorFormat("Too many errors on job {0} with data {1}",
                        context.JobDetail.JobType,
                        JsonConvert.SerializeObject(context.JobDetail.JobDataMap)
                    );
                }
            }
            catch (Exception rescheduleException)
            {
                _logger.Fatal("rescheduling", rescheduleException);
            }
        }

        public string Name
        {
            get { return "pipeline.listener"; }
        }
    }
}
