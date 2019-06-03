namespace Soloplan.WhatsON.ServerBase
{
  public static class SubjectExtensions
  {
    public static string GetAddress(this ServerSubject subject)
    {
      return subject.SubjectConfiguration.GetConfigurationByKey(ServerSubject.ServerAddress).Value;
    }
  }
}