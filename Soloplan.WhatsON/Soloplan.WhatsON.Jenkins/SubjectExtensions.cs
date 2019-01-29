namespace Soloplan.WhatsON.Jenkins
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

    public static string GetProject(this JenkinsProject project)
    {
      return project.Configuration[JenkinsProject.ProjectName];
    }
  }
}