namespace Soloplan.WhatsON.PluginGUIExtensions.Jenkins
{
  using Soloplan.WhatsON.GUI.SubjectTreeView;

  public class JenkinsProjectViewModel : SubjectViewModel
  {

    public JenkinsProjectViewModel()
    {

    }

    protected override StatusViewModel GetViewModelForStatus(Subject subject)
    {
      var viewModel = new JenkinsStatusViewModel();
      viewModel.Update(subject.CurrentStatus);
      return viewModel;
    }
  }
}
