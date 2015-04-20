# TrelloArchiver
Windows service that archives cards from specified trello lists.

Usage:

1) clone, build, copy to a directory somewhere.

2) open up app.config, setup your logging preferences. Defaults include an email target so you get fatal errors AND a Warn when a card is archived.

3) `TrelloArchiver.exe install`

4) Check logs and services.msc, make sure service installed properly and started. Logs should have a url to get your auth token. Follow url, copy/paste your auth token into app.config.

5) restart service, check logs again. Now they should have a list of your Trello Lists and their IDs. Copy the lists you want monitored into app.config, semicolon separated.

6) restart service. You should be good to go! The service checks hourly, but that's all configured using quarts in Scheduler.cs

`TrelloArchiver.exe install`