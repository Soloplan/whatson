namespace Soloplan.WhatsON.CLI
{
  using CommandLine;
  class Options
  {
    [Value(0, HelpText = "The command to be executed.", MetaName ="{command}", MetaValue = "[subject, dummy, observe, query]", Required = true)]
    public string Command { get; set; }

    [Value(1)]
    public string SubjectName { get; set; }
  }
}
