using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quartz;
using TrelloNet;
using Common.Logging;

namespace TrelloArchiver
{
  class ArchiveSetting
  {
    public string ListID { get; set; }
    public TimeSpan Aging { get { return TimeSpan.FromDays(double.Parse(System.Configuration.ConfigurationManager.AppSettings["daysToArchive"] ?? "14")); } }
  }
  public class ArchiverJob : IJob
  {
    private string _key;
    private string _secret;
    private string _authToken;
    private ITrello _trello;
    private ILog _log = LogManager.GetCurrentClassLogger();
    public ArchiverJob() : this(null,null,null)
    {
      
    }

    public ArchiverJob(ITrello trello = null, string key = null, string secret = null)
    {
      this._key = key ?? System.Configuration.ConfigurationManager.AppSettings["trellokey"];
      this._secret = secret ?? System.Configuration.ConfigurationManager.AppSettings["trellosecret"];
      this._authToken = System.Configuration.ConfigurationManager.AppSettings["trelloauthtoken"];
      this._trello = trello ?? new Trello(this._key);
      if(_authToken == null) {
        string msg = "You must authorize this app for your trello account. Use the url: " + _trello.GetAuthorizationUrl("Trello Archiver",Scope.ReadWrite,Expiration.Never).ToString();
        _log.Fatal(msg);
        throw new ArgumentException(msg);
      }
      _trello.Authorize(this._authToken);
      _log.InfoFormat("Authorized with token {0}",this._authToken);
    }

    private int timeoutRequests = 0;
    private int totalRequests = 1;
    private DateTime LastReset = DateTime.UtcNow;
    private void addRequest()
    {
      timeoutRequests++;
      totalRequests++;
      if (timeoutRequests > 250)
      {
        _log.Info("Hit 250 requests.");
        if ((DateTime.UtcNow - LastReset).TotalSeconds < 15)
        {
          _log.Info("nearing api limit, sleeping for a bit.");
          System.Threading.Thread.Sleep(1000 * 15);
        }
        timeoutRequests = 0;
        LastReset = DateTime.UtcNow;
      }
      else
        System.Threading.Thread.Sleep(10); 
      //completely arbitrary, just being a good citizen here.
      //10 means we can hit up to 666 requests in the 10 second timespan.

    }
    public void Execute(IJobExecutionContext context)
    {
      var listIDs = System.Configuration.ConfigurationManager.AppSettings["ListIDs"];
      if(listIDs == null) {
        var myBoards = _trello.Boards.ForMe();
        string listInfoStr = "";
        if (myBoards != null)
        {
          var listInfo = myBoards.SelectMany(x => _trello.Lists.ForBoard(new BoardId(x.Id)).Select(l => l.Name + "-" + l.Id + "-board:" + x.Name));
          listInfoStr = String.Join(Environment.NewLine, listInfo);
        }
        else
          listInfoStr = "null response from trello, probably a bug in TrelloNet";
        string msg = "Must have a ListIDs app settings with a semicolon separated list of list Ids to archive against. Your current list ids are: " + listInfoStr;
        _log.Fatal(msg);
        throw new ConfigurationException(msg);
      }
      var lists = listIDs.Split(';').Select(x =>
      {
        return new ArchiveSetting() { ListID = x };
      });
      int requests = 1; //1 because auth request.
      int totalRequests = 0;
      //rate limit is 300 / 10 seconds.
      foreach (var list in lists)
      {
        _log.Trace("Examining list " + list.ListID);
        var lid = list.ListID;
        var theCards = _trello.Cards.ForList(new ListId(list.ListID),CardFilter.Open);
        requests++;
        foreach (var card in theCards)
        {
          if (card.DateLastActivity < DateTime.UtcNow.Subtract(list.Aging))
          {
            _trello.Cards.Archive(new CardId(card.Id));
            _log.WarnFormat("Archived card {0} with name {1} from list {2} and board {3}", card.Id, card.Name, card.IdList, card.IdBoard);
          }
          else
          {
            _log.TraceFormat("Card {0} ({1}) has {2} days left.",card.Id,card.Name,(list.Aging - (DateTime.UtcNow - card.DateLastActivity)).TotalDays);
          }
        }
      }
      _log.Trace("Finished run. requests: " + totalRequests);
      totalRequests = 0;
      requests = 0;
      //get all the trello data, then match it up with ours!
    }
  }
}
