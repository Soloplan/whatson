namespace Soloplan.WhatsON.ServerBase
{
  using Newtonsoft.Json;

  [ConfigurationItem(ServerAddress, typeof(string))]
  [ConfigurationItem(ServerPort, typeof(int))]
  public abstract class ServerSubject : Subject
  {
    public const string ServerAddress = "Address";
    public const string ServerPort = "Port";

    [JsonIgnore]
    public string Address
    {
      get => this.GetConfigurationByKey(ServerAddress).Value;
      set => this.GetConfigurationByKey(ServerAddress).Value = value;
    }

    [JsonIgnore]
    public virtual int Port
    {
      get
      {
        var configItem = this.GetConfigurationByKey(ServerPort);
        if (configItem != null)
        {
          return int.TryParse(configItem.Value, out var port) ? port : 0;
        }

        return 0;
      }

      set => this.GetConfigurationByKey(ServerPort).Value = value.ToString();
    }
  }
}
