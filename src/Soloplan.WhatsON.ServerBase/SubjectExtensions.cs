namespace Soloplan.WhatsON.ServerBase
{
  public static class SubjectExtensions
  {
    public static string GetAddress(this ServerSubject project)
    {
      return project.Configuration[ServerSubject.ServerAddress];
    }

    public static string GetPort(this ServerSubject project)
    {
      return project.Configuration[ServerSubject.ServerPort];
    }
  }
}