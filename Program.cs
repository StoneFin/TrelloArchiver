using System;
using Common.Logging;
using Common.Logging.Configuration;
using Topshelf;
using Topshelf.Common.Logging;

namespace TrelloArchiver
{
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
        HostFactory.Run(x =>
        {
          x.UseCommonLogging();
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
