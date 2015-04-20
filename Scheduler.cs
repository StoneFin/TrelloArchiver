using System;
using Quartz;
using Quartz.Impl;


namespace TrelloArchiver
{
  public class Scheduler
  {
    private Common.Logging.ILog _log = Common.Logging.LogManager.GetCurrentClassLogger();
    private IScheduler _scheduler;
    public Scheduler()
    {
      // Grab the Scheduler instance from the Factory 
      this._scheduler = StdSchedulerFactory.GetDefaultScheduler();
    }
    public Scheduler(IScheduler Scheduler)
    {
      this._scheduler = Scheduler;
    }

    public void Start()
    {
      try
      {
        // and start it off
        _scheduler.Start();
        // define the job and tie it to our HelloJob class
        IJobDetail job = JobBuilder.Create<ArchiverJob>()
            .WithIdentity("job1", "group1")
            .Build();
        // Trigger the job to run now, and then repeat every 10 seconds
        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity("trigger1", "group1")
            .WithSimpleSchedule(x => x
                .WithIntervalInHours(1)
                .RepeatForever())
            .Build();
        // Tell quartz to schedule the job using our trigger
        _scheduler.ScheduleJob(job, trigger);
      }
      catch (Exception ex)
      {
        _log.Fatal("Uncaught exception during scheduler start.", ex);
        throw;
      }
    }
    public void Stop()
    {
      // and last shut down the scheduler when you are ready to close your program
      _scheduler.Shutdown();
    }
  }
}
