namespace Soloplan.WhatsON.Serialization
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using Newtonsoft.Json;

  public static class SerializationHelper
  {
    public static readonly string ConfigFile = "configuration.json";

    public static void Save<T>(T subject, string file)
    {
      var json = JsonConvert.SerializeObject(subject, Formatting.Indented);
      File.WriteAllText(file, json);
    }

    public static T Load<T>(string file)
    {
      return JsonConvert.DeserializeObject<T>(File.ReadAllText(file));
    }

    public static IEnumerable<Subject> LoadSubjects()
    {
      throw new NotImplementedException();
    }

    public static void SaveConfiguration(Configuration configuration)
    {
      Save(configuration, ConfigFile);
    }
  }
}