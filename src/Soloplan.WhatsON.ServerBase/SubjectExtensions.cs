namespace Soloplan.WhatsON.ServerBase
{
  public static class SubjectExtensions
  {
    public static string GetAddress(this ServerSubject project)
    {
      return project.GetConfigurationByKey(ServerSubject.ServerAddress).Value;
    }

    public static string GetPort(this ServerSubject project)
    {
      return project.GetConfigurationByKey(ServerSubject.ServerPort).Value;
    }
  }
}