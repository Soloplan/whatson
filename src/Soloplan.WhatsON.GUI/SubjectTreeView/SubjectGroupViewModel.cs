namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  public class SubjectGroupViewModel : ViewModelBase, IHandleDoubleClick
  {
    private string groupName;

    ObservableCollection<SubjectViewModel> statusViewModels;

    public ObservableCollection<SubjectViewModel> SubjectViewModels => this.statusViewModels ?? (this.statusViewModels = new ObservableCollection<SubjectViewModel>());

    public string GroupName
    {
      get
      {
        return this.groupName;
      }

      set
      {
        if (this.groupName != value)
        {
          this.groupName = value;
          this.OnPropertyChanged();
        }
      }
    }

    public bool Update(Subject changedSubject)
    {
      var changedViewModel = this.SubjectViewModels.FirstOrDefault(subject => subject.Identifier == changedSubject.SubjectConfiguration.Identifier);
      if (changedViewModel != null)
      {
        changedViewModel.Update(changedSubject);
        return true;
      }

      return false;
    }

    public void Init(IGrouping<string, SubjectConfiguration> subjectGroup)
    {
      this.GroupName = subjectGroup.Key ?? string.Empty;

      foreach (var subjectConfiguration in subjectGroup)
      {
        var subject = PluginsManager.Instance.GetSubject(subjectConfiguration);
        SubjectViewModel subjectViewModel = this.GetSubjectViewModel(subject);
        subjectViewModel.Init(subject);
        this.SubjectViewModels.Add(subjectViewModel);
      }
    }

    public void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      foreach (var subjectViewModel in this.SubjectViewModels)
      {
        subjectViewModel.OnDoubleClick(sender, e);
      }
    }

    private SubjectViewModel GetSubjectViewModel(Subject subject)
    {
      var presentationPlugIn = PluginsManager.Instance.GetPresentationPlugIn(subject.GetType());
      if (presentationPlugIn != null)
      {
        return presentationPlugIn.CreateViewModel();
      }

      return new SubjectViewModel();
    }
  }
}