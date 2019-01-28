namespace Soloplan.WhatsON.CLI
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Threading;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Jenkins;
  using Soloplan.WhatsON.Serialization;
  using Soloplan.WhatsON.ServerHealth;

  class Program
  {
    static void Main(string[] args)
    {
      // basic test for loading SubjectFactories from other assemblies
      var factories = PluginFinder.FindAllSubjectPlugins("Soloplan.WhatsON.ServerHealth.dll", "Soloplan.WhatsON.Jenkins.dll").ToList();
      foreach (var factory in factories)
      {
        if (factory.SubjectType == null)
        {
          continue;
        }

        var typeDesc = factory.SubjectTypeAttribute;
        if (typeDesc == null)
        {
          continue;
        }

        Console.WriteLine($"Found subject factory: {typeDesc.Name} - {typeDesc.Description}");
      }

      var healthFactory = factories.FirstOrDefault(x => x is ServerHealthSubjectPlugin);
      var jenkinsFactory = factories.FirstOrDefault(x => x is JenkinsBuildJobSubjectPlugin);

      var subject = healthFactory?.CreateNew("Build", new Dictionary<string, string> { [ServerSubject.ServerAddress] = "build.soloplan.de", });
      var subject2 = healthFactory?.CreateNew("Swarm", new Dictionary<string, string> { [ServerSubject.ServerAddress] = "swarm.soloplan.de", });
      var subject3 = healthFactory?.CreateNew("Artifacts", new Dictionary<string, string> { [ServerSubject.ServerAddress] = "artifacts.soloplan.de", });

      var jenkinsParameters = new Dictionary<string, string>
      {
        [ServerSubject.ServerAddress] = "https://jenkins.mono-project.com",
        [JenkinsBuildJobSubject.JobName] = "test-mono-pipeline",
      };

      // test jenkins api of publically available jenkins
      var subjectJenkins = jenkinsFactory?.CreateNew("Test Mono Pipeline", jenkinsParameters);

      // initialize the scheduler a
      var scheduler = new ObservationScheduler();
      if (subject != null)
      {
        scheduler.Observe(subject);
        //scheduler.Observe(subject2);
        //scheduler.Observe(subject3);
        scheduler.Observe(subjectJenkins);
      }

      scheduler.Start();

      Thread.Sleep(10000);

      var config = new Configuration();
      config.Subjects.Add(subject);
      config.Subjects.Add(subject2);
      config.Subjects.Add(subject3);
      config.Subjects.Add(subjectJenkins);

      SerializationHelper.SaveConfiguration(config);

      Thread.Sleep(20000);
      Console.ReadKey();
    }
  }
}
