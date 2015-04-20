using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Topshelf;
using Quartz;
using Quartz.Impl;
using Common.Logging;
using Common.Logging.Configuration;

namespace TrelloArchiver
{
  public class Scheduler
  {
    private ILog _log = LogManager.GetCurrentClassLogger();
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
  class Program
  {
    private static ILog _log;
    static void Main(string[] args)
    {
      NameValueCollection properties = new NameValueCollection();
      properties["showDateTime"] = "true";
      properties["level"] = "All";
      properties["showLogName"] = "true";
#if DEBUG
      Common.Logging.LogManager.Adapter = new Common.Logging.Simple.ConsoleOutLoggerFactoryAdapter(properties);
#endif
      _log = LogManager.GetCurrentClassLogger();

      try
      {
        HostFactory.Run(x => {
            x.Service<Scheduler>(s =>                        //2
            {
              s.ConstructUsing(name => new Scheduler());     //3
              s.WhenStarted(tc => tc.Start());              //4
              s.WhenStopped(tc => tc.Stop());               //5
            });
            x.RunAsLocalSystem();                            //6
            x.SetDescription("Trello Archiver - regularly archives cards.");        //7
            x.SetDisplayName("TrelloArchiver");                       //8
            x.SetServiceName("TrelloArchiver");                       //9
            x.StartAutomaticallyDelayed();
          });                                                  //10
      }
      catch (Exception ex)
      {
        _log.Fatal("Uncaught exception during program start.", ex);
        throw;
      }
    }
  }
}
