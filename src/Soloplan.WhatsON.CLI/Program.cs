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
          ListConfiguredSubjects();
          break;
        case "observe":
          ObserveConfiguredSubjects();
          break;
      }

      Console.ReadKey();
      System.Environment.Exit(0);
    }

    private static void ObserveConfiguredSubjects()
    {
      var config = ListConfiguredSubjects();
      var scheduler = PrepareScheduler();
      foreach (var subjectConfiguration in config.SubjectsConfiguration)
      {
        var subject = PluginsManager.Instance.GetSubject(subjectConfiguration);
        scheduler.Observe(subject);
      }

      scheduler.Start();
    }

    private static ApplicationConfiguration ListConfiguredSubjects()
    {
      var config = SerializationHelper.LoadConfiguration();

      Console.WriteLine("Configured observation subjects:");
      Console.ForegroundColor = ConsoleColor.DarkGray;
      foreach (var subject in config.SubjectsConfiguration)
      {
        Console.WriteLine($"  {subject.Name}");
      }

      Console.ForegroundColor = ConsoleColor.Gray;
      Console.WriteLine();
      return config;
    }

    private static void CreateDummyData()
    {
      // create some dummy data for the configuration
      var subject = PluginsManager.Instance.CreateNewSubject(new SubjectConfiguration("Soloplan.WhatsON.ServerHealth.ServerHealthPlugin", "Google", "Address", "google.com"));
      var subject2 = PluginsManager.Instance.CreateNewSubject(new SubjectConfiguration("Soloplan.WhatsON.ServerHealth.ServerHealthPlugin", "Soloplan", "Address", "soloplan.de"));
      var subject3 = PluginsManager.Instance.CreateNewSubject(new SubjectConfiguration("Soloplan.WhatsON.ServerHealth.ServerHealthPlugin", "GitHub", "Address", "github.com"));

      var jenkinsParameters = new List<ConfigurationItem>
      {
        new ConfigurationItem("Address","https://jenkins.mono-project.com"),
        new ConfigurationItem( "ProjectName", "test-mono-pipeline"),
      };

      // test jenkins api of publically available jenkins
      var subjectJenkins = PluginsManager.Instance.CreateNewSubject(new SubjectConfiguration("Soloplan.WhatsON.Jenkins.JenkinsProjectPlugin", "Test Mono Pipeline", jenkinsParameters));

      var scheduler = PrepareScheduler();
      if (subject != null)
      {
        scheduler.Observe(subject);
        scheduler.Observe(subject2);
        scheduler.Observe(subject3);
        scheduler.Observe(subjectJenkins, 10);
      }

      scheduler.Start();

      // make sure to wait a bit so that we get the first status for each subject
      Thread.Sleep(10000);

      var config = new ApplicationConfiguration();
      config.SubjectsConfiguration.Add(subject.SubjectConfiguration);
      config.SubjectsConfiguration.Add(subject2.SubjectConfiguration);
      config.SubjectsConfiguration.Add(subject3.SubjectConfiguration);
      config.SubjectsConfiguration.Add(subjectJenkins.SubjectConfiguration);
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
            Console.Write($"{sub.SubjectConfiguration.Name} [{sub.CurrentStatus.Time}]: {sub.CurrentStatus.Name} - ");
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
