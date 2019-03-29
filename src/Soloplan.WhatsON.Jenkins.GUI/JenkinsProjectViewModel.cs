namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System.Windows.Controls;
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.SubjectTreeView;
  using Soloplan.WhatsON.ServerBase;

  public class JenkinsProjectViewModel : SubjectViewModel
  {
    private OpenWebPageCommandData openWebPageParam;

    /// <summary>
    /// Gets command for opening builds webPage.
    /// </summary>
    public ICommand OpenWebPage { get; } = new OpenWebPageCommand();

    public OpenWebPageCommandData OpenWebPageParam
    {
      get => this.openWebPageParam;
      set
      {
        this.openWebPageParam = value;
        this.OnPropertyChanged(nameof(this.OpenWebPageParam));
      }
    }

    public override void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      base.OnDoubleClick(sender, e);
      var treeViewItem = sender as TreeViewItem;
      if (treeViewItem != null && treeViewItem.DataContext == this && this.OpenWebPage.CanExecute(this.OpenWebPageParam))
      {
        this.OpenWebPage.Execute(this.OpenWebPageParam);
      }
    }

    public override void Init(SubjectConfiguration configuration)
    {
      base.Init(configuration);

      OpenWebPageCommandData param = new OpenWebPageCommandData();
      if (bool.TryParse(configuration.GetConfigurationByKey(JenkinsProject.RedirectPlugin)?.Value, out var redirect) && redirect)
      {
        param.Redirect = true;
      }
      else
      {
        param.Redirect = false;
      }

      param.Address = configuration.GetConfigurationByKey(ServerSubject.ServerAddress).Value + "/job/" + configuration.GetConfigurationByKey(JenkinsProject.ProjectName).Value;

      this.OpenWebPageParam = param;

      this.SetAddressForState(this.CurrentStatus);

      foreach (var subjectSnapshot in this.SubjectSnapshots)
      {
        this.SetAddressForState(subjectSnapshot);
      }
    }

    protected override StatusViewModel GetViewModelForStatus()
    {
      var jenkinsModel = new JenkinsStatusViewModel(this);
      this.SetAddressForState(jenkinsModel);
      this.SetAddressForState(jenkinsModel);
      return jenkinsModel;
    }

    private void SetAddressForState(StatusViewModel model)
    {
      if (model is JenkinsStatusViewModel jenkinsModel)
      {
        jenkinsModel.SetJobAddress(this.OpenWebPageParam);
      }
    }
  }
}
