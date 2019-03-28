namespace Soloplan.WhatsON.ServerBase
{
  [ConfigurationItem(ServerAddress, typeof(string), Optional = false, Priority = 100)]
  [ConfigurationItem(ServerPort, typeof(int), Priority = 200)]
  public abstract class ServerSubject : Subject
  {
    public const string ServerAddress = "Address";
    public const string ServerPort = "Port";

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerSubject"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    protected ServerSubject(SubjectConfiguration configuration)
      : base(configuration)
    {
    }

    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    public string Address
    {
      get => this.SubjectConfiguration.GetConfigurationByKey(ServerAddress).Value;
      set => this.SubjectConfiguration.GetConfigurationByKey(ServerAddress).Value = value;
    }

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    public virtual int Port
    {
      get
      {
        var configItem = this.SubjectConfiguration.GetConfigurationByKey(ServerPort);
        if (configItem != null)
        {
          return int.TryParse(configItem.Value, out var port) ? port : 0;
        }

        return 0;
      }

      set => this.SubjectConfiguration.GetConfigurationByKey(ServerPort).Value = value.ToString();
    }
  }
}
