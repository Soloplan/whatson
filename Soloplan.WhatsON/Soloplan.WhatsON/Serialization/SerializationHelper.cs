namespace Soloplan.WhatsON.Serialization
{
  using System.IO;
  using Newtonsoft.Json;

  public static class SerializationHelper
  {
    public static readonly string ConfigFile = "configuration.json";

    public static void Save<T>(T subject, string file)
    {
      JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented };
      var json = JsonConvert.SerializeObject(subject, settings);
      File.WriteAllText(file, json);
    }

    public static T Load<T>(string file)
    {
      JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
      return JsonConvert.DeserializeObject<T>(File.ReadAllText(file), settings);
    }

    public static Configuration LoadConfiguration()
    {
      return Load<Configuration>(ConfigFile);
    }

    public static void SaveConfiguration(Configuration configuration)
    {
      Save(configuration, ConfigFile);
    }
  }
}