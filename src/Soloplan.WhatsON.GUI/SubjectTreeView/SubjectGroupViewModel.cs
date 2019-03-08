namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Reflection;
  using Soloplan.WhatsON.GUI.Config.ViewModel;
  using Soloplan.WhatsON.GUI.SubjectTreeView.ServerHealth;
  using Soloplan.WhatsON.Jenkins;

  public class SubjectGroupViewModel : ViewModelBase
  {
    private ObservationScheduler scheduler;
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
      if (subject is JenkinsProject)
      {
        var assembly = Assembly.LoadFrom("Soloplan.WhatsON.PluginGUIExtensions.dll");
        var type = assembly.GetType("Soloplan.WhatsON.PluginGUIExtensions.Jenkins.JenkinsProjectViewModel");
        var result = Activator.CreateInstance(type) as SubjectViewModel;
        if(result != null)
        {
          return result;
        }
        //return new ();
      }

      if (subject is Soloplan.WhatsON.ServerHealth.ServerHealth)
      {
        return new ServerHealthViewModel();
      }

      return new SubjectViewModel();
    }
  }
}