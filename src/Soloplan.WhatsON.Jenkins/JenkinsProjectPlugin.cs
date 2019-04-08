namespace Soloplan.WhatsON.Jenkins
{
  public class JenkinsProjectPlugin : SubjectPlugin
  {
    public JenkinsProjectPlugin()
      : base(typeof(JenkinsProject))
    {
    }

    public override Subject CreateNew(SubjectConfiguration configuration)
    {
      var jenkinsProject = new JenkinsProject(configuration, new JenkinsApi());
      return jenkinsProject;
    }
  }
}
