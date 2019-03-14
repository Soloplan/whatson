namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System.Collections.ObjectModel;
  using System.Linq;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  public class SubjectGroupViewModel : ViewModelBase
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
      var changedViewModel = this.SubjectViewModels.FirstOrDefault(subject => subject.Identifier == changedSubject.Identifier);
      if (changedViewModel != null)
      {
        changedViewModel.Update(changedSubject);
        return true;
      }

      return false;
    }

    public void Init(IGrouping<string, Subject> subjectGroup)
    {
      this.GroupName = subjectGroup.Key ?? string.Empty;

      foreach (var subject in subjectGroup)
      {
        SubjectViewModel subjectViewModel = this.GetSubjectViewModel(subject);
        subjectViewModel.Init(subject);
        this.SubjectViewModels.Add(subjectViewModel);
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