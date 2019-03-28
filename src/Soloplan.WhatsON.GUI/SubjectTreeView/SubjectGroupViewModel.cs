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

    private bool isNodeExpanded;

    public SubjectGroupViewModel()
    {
      this.IsNodeExpanded = true;
    }

    public ObservableCollection<SubjectViewModel> SubjectViewModels => this.statusViewModels ?? (this.statusViewModels = new ObservableCollection<SubjectViewModel>());

    public bool IsNodeExpanded
    {
      get => this.isNodeExpanded;
      set
      {
        if (this.isNodeExpanded != value)
        {
          this.isNodeExpanded = value;
          this.OnPropertyChanged();
        }
      }
    }

    public string GroupName
    {
      get => this.groupName;
      set
      {
        if (this.groupName != value)
        {
          this.groupName = value;
          this.OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// Called when subject changes, ex new status.
    /// </summary>
    /// <param name="changedSubject">The subject which changed.</param>
    /// <returns>True if something was changed; false otherwise.</returns>
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

    /// <summary>
    /// Should be called when configuration changed.
    /// </summary>
    /// <param name="subjectGroup">Grouping of subjects by group name.</param>
    public void Init(IGrouping<string, SubjectConfiguration> subjectGroup)
    {
      this.GroupName = subjectGroup.Key ?? string.Empty;

      var subjectsNoLongerPresent = this.SubjectViewModels.Where(model => subjectGroup.All(configurationSubject => configurationSubject.Identifier != model.Identifier)).ToList();
      var newSubjects = subjectGroup.Where(configurationSubject => this.SubjectViewModels.All(viewModel => configurationSubject.Identifier != viewModel.Identifier));
      foreach (var noLongerPresentSubjectViewModel in subjectsNoLongerPresent)
      {
        this.SubjectViewModels.Remove(noLongerPresentSubjectViewModel);
      }

      foreach (var newSubject in newSubjects)
      {
        this.CreateViewModelForSubjectConfiguration(newSubject);
      }
    }

    private void CreateViewModelForSubjectConfiguration(SubjectConfiguration subjectConfiguration)
    {
      var subject = PluginsManager.Instance.GetSubject(subjectConfiguration);
      SubjectViewModel subjectViewModel = this.GetSubjectViewModel(subject);
      subjectViewModel.Init(subject);
      this.SubjectViewModels.Add(subjectViewModel);
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