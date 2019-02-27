namespace Soloplan.WhatsON.CLI
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using CommandLine;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Serialization;
  using Soloplan.WhatsON.ServerBase;
  using Soloplan.WhatsON.ServerHealth;

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
      foreach (var subject in config.Subjects)
      {
        scheduler.Observe(subject);
      }

      scheduler.Start();
    }

    private static Configuration ListConfiguredSubjects()
    {
      var config = SerializationHelper.LoadConfiguration();

      Console.WriteLine("Configured observation subjects:");
      Console.ForegroundColor = ConsoleColor.DarkGray;
      foreach (var subject in config.Subjects)
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
      var plugins = PluginsManager.Instance.SubjectPlugins;
      var healthFactory = plugins.FirstOrDefault(x => x is ServerHealthPlugin);
      var jenkinsFactory = plugins.FirstOrDefault(x => x is JenkinsProjectPlugin);

      var subject = healthFactory?.CreateNew("Google", new Dictionary<string, string> { [ServerSubject.ServerAddress] = "google.com", });
      var subject2 = healthFactory?.CreateNew("Soloplan", new Dictionary<string, string> { [ServerSubject.ServerAddress] = "soloplan.de", });
      var subject3 = healthFactory?.CreateNew("GitHub", new Dictionary<string, string> { [ServerSubject.ServerAddress] = "github.com", });

      var jenkinsParameters = new Dictionary<string, string>
      {
        [ServerSubject.ServerAddress] = "https://jenkins.mono-project.com",
        [JenkinsProject.ProjectName] = "test-mono-pipeline",
      };

      // test jenkins api of publically available jenkins
      var subjectJenkins = jenkinsFactory?.CreateNew("Test Mono Pipeline", jenkinsParameters);

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

      var config = new Configuration();
      config.Subjects.Add(subject);
      config.Subjects.Add(subject2);
      config.Subjects.Add(subject3);
      config.Subjects.Add(subjectJenkins);
      SerializationHelper.SaveConfiguration(config);
    }

    private static ObservationScheduler PrepareScheduler()
    {
      var scheduler = new ObservationScheduler();
      scheduler.ObservationRunStarted += (s, e) =>
      {
        Console.WriteLine();
      };

      scheduler.StatusQueried += (s, sub) =>
      {
        lock (lockObj)
        {
          if (sub.CurrentStatus != null)
          {
            Console.Write($"{sub.Name} [{sub.CurrentStatus.Time}]: {sub.CurrentStatus.Name} - ");
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
