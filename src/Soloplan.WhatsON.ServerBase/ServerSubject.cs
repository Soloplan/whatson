namespace Soloplan.WhatsON.ServerBase
{
  [ConfigurationItem(ServerAddress, typeof(string), Optional = false, Priority = 100)]
  public abstract class ServerSubject : Subject
  {
    public const string ServerAddress = "Address";

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
  }
}
