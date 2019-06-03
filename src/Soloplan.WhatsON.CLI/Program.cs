namespace Soloplan.WhatsON.CLI
{
  using System;
  using System.Collections.Generic;
  using System.Threading;
  using CommandLine;
  using Soloplan.WhatsON.Serialization;

  public class Program
  {
    private static readonly object lockObj = new object();

    public static void Main(string[] args)
    {
      var results = Parser.Default.ParseArguments<Options>(args);
      if (results.Tag == ParserResultType.NotParsed || !(results is Parsed<Options> parsed))
      {
        Console.ReadKey();
        return;
      }

      var options = parsed.Value;

      switch (options.Command)
      {
#if DEBUG
        case "dummy":
          CreateDummyData();
          break;
#endif
        case "ls":
          ListConfiguredConnectors();
          break;
        case "observe":
          ObserveConfiguredConnectors();
          break;
      }

      Console.ReadKey();
      System.Environment.Exit(0);
    }

    private static void ObserveConfiguredConnectors()
    {
      var config = ListConfiguredConnectors();
      var scheduler = PrepareScheduler();
      foreach (var connectorConfiguration in config.ConnectorsConfiguration)
      {
        var connector = PluginsManager.Instance.GetConnector(connectorConfiguration);
        scheduler.Observe(connector);
      }

      scheduler.Start();
    }

    private static ApplicationConfiguration ListConfiguredConnectors()
    {
      var config = SerializationHelper.LoadConfiguration();

      Console.WriteLine("Configured observation connectors:");
      Console.ForegroundColor = ConsoleColor.DarkGray;
      foreach (var connector in config.ConnectorsConfiguration)
      {
        Console.WriteLine($"  {connector.Name}");
      }

      Console.ForegroundColor = ConsoleColor.Gray;
      Console.WriteLine();
      return config;
    }

    private static void CreateDummyData()
    {
      // create some dummy data for the configuration
      var connector = PluginsManager.Instance.CreateNewConnector(new ConnectorConfiguration("Soloplan.WhatsON.ServerHealth.ServerHealthPlugin", "Google", "Address", "google.com"));
      var connector2 = PluginsManager.Instance.CreateNewConnector(new ConnectorConfiguration("Soloplan.WhatsON.ServerHealth.ServerHealthPlugin", "Soloplan", "Address", "soloplan.de"));
      var connector3 = PluginsManager.Instance.CreateNewConnector(new ConnectorConfiguration("Soloplan.WhatsON.ServerHealth.ServerHealthPlugin", "GitHub", "Address", "github.com"));

      var jenkinsParameters = new List<ConfigurationItem>
      {
        new ConfigurationItem("Address","https://jenkins.mono-project.com"),
        new ConfigurationItem( "ProjectName", "test-mono-pipeline"),
      };

      // test jenkins api of publically available jenkins
      var connectorJenkins = PluginsManager.Instance.CreateNewConnector(new ConnectorConfiguration("Soloplan.WhatsON.Jenkins.JenkinsProjectPlugin", "Test Mono Pipeline", jenkinsParameters));

      var scheduler = PrepareScheduler();
      if (connector != null)
      {
        scheduler.Observe(connector);
        scheduler.Observe(connector2);
        scheduler.Observe(connector3);
        scheduler.Observe(connectorJenkins, 10);
      }

      scheduler.Start();

      // make sure to wait a bit so that we get the first status for each connector
      Thread.Sleep(10000);

      var config = new ApplicationConfiguration();
      config.ConnectorsConfiguration.Add(connector.ConnectorConfiguration);
      config.ConnectorsConfiguration.Add(connector2.ConnectorConfiguration);
      config.ConnectorsConfiguration.Add(connector3.ConnectorConfiguration);
      config.ConnectorsConfiguration.Add(connectorJenkins.ConnectorConfiguration);
      SerializationHelper.SaveConfiguration(config);
    }

    private static ObservationScheduler PrepareScheduler()
    {
      var scheduler = new ObservationScheduler();

      scheduler.StatusQueried += (s, sub) =>
      {
        lock (lockObj)
        {
          if (sub.CurrentStatus != null)
          {
            Console.Write($"{sub.ConnectorConfiguration.Name} [{sub.CurrentStatus.Time}]: {sub.CurrentStatus.Name} - ");
            var stateColor = ConsoleColor.DarkMagenta;
            switch (sub.CurrentStatus.State)
            {
              case ObservationState.Success:
                stateColor = ConsoleColor.DarkGreen;
                break;
              case ObservationState.Unstable:
                stateColor = ConsoleColor.DarkYellow;
                break;
              case ObservationState.Failure:
                stateColor = ConsoleColor.DarkRed;
                break;
              case ObservationState.Running:
                stateColor = ConsoleColor.DarkCyan;
                break;
            }

            Console.ForegroundColor = stateColor;
            Console.Write($"{sub.CurrentStatus.State}\n");
          }
          else
          {
            Console.WriteLine(sub);
          }

          Console.ForegroundColor = ConsoleColor.Gray;
        }
      };

      return scheduler;
    }
  }
}
