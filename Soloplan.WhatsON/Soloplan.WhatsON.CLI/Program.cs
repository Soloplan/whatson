namespace Soloplan.WhatsON.CLI
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Threading;
  using CommandLine;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Serialization;
  using Soloplan.WhatsON.ServerHealth;

  class Program
  {
    private static List<ISubjectPlugin> plugins;

    static void Main(string[] args)
    {
      var results = Parser.Default.ParseArguments<Options>(args);
      if (results.Tag == ParserResultType.NotParsed || !(results is Parsed<Options> parsed))
      {
        Console.ReadKey();
        return;
      }

      var options = parsed.Value;

      LoadPlugins();

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
      }

      Console.ReadKey();
      System.Environment.Exit(0);
    }

    private static void ListConfiguredSubjects()
    {
      var config = SerializationHelper.LoadConfiguration();

      Console.WriteLine("Configured observation subjects:");
      Console.ForegroundColor = ConsoleColor.DarkGray;
      foreach (var subject in config.Subjects)
      {
        Console.WriteLine($"  {subject}");
      }

      Console.BackgroundColor = ConsoleColor.White;
    }

    private static void LoadPlugins()
    {
      Console.WriteLine("Searching available plugins...");
      Console.ForegroundColor = ConsoleColor.DarkGray;
      // basic test for loading SubjectFactories from other assemblies
      var found = PluginFinder.FindAllSubjectPlugins("Soloplan.WhatsON.ServerHealth.dll", "Soloplan.WhatsON.Jenkins.dll");
      plugins = new List<ISubjectPlugin>();
      foreach (var plugin in found)
      {
        if (plugin.SubjectType == null)
        {
          continue;
        }

        var typeDesc = plugin.SubjectTypeAttribute;
        if (typeDesc == null)
        {
          continue;
        }

        Console.WriteLine($"  Found: {typeDesc.Name} - {typeDesc.Description}");
        plugins.Add(plugin);
      }

      Console.WriteLine();
      Console.ForegroundColor = ConsoleColor.Gray;
    }

    private static void CreateDummyData()
    {
      // create some dummy data for the configuration
      var healthFactory = plugins.FirstOrDefault(x => x is ServerHealthPlugin);
      var jenkinsFactory = plugins.FirstOrDefault(x => x is JenkinsBuildJobPlugin);

      var subject = healthFactory?.CreateNew("Google", new Dictionary<string, string> { [ServerSubject.ServerAddress] = "google.com", });
      var subject2 = healthFactory?.CreateNew("Soloplan", new Dictionary<string, string> { [ServerSubject.ServerAddress] = "soloplan.de", });
      var subject3 = healthFactory?.CreateNew("GitHub", new Dictionary<string, string> { [ServerSubject.ServerAddress] = "github.com", });

      var jenkinsParameters = new Dictionary<string, string>
      {
        [ServerSubject.ServerAddress] = "https://jenkins.mono-project.com",
        [JenkinsBuildJob.JobName] = "test-mono-pipeline",
      };

      // test jenkins api of publically available jenkins
      var subjectJenkins = jenkinsFactory?.CreateNew("Test Mono Pipeline", jenkinsParameters);

      // initialize the scheduler a
      var scheduler = new ObservationScheduler();
      if (subject != null)
      {
        scheduler.Observe(subject);
        scheduler.Observe(subject2);
        scheduler.Observe(subject3);
        scheduler.Observe(subjectJenkins, 10);
      }

      scheduler.Start();

      Thread.Sleep(10000);

      var config = new Configuration();
      config.Subjects.Add(subject);
      config.Subjects.Add(subject2);
      config.Subjects.Add(subject3);
      config.Subjects.Add(subjectJenkins);
      SerializationHelper.SaveConfiguration(config);
    }
  }
}
