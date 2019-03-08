namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;
  using System.Collections.ObjectModel;
  using Soloplan.WhatsON.GUI.Config.ViewModel;
  using Soloplan.WhatsON.Jenkins;

  public class SubjectViewModel : ViewModelBase
  {

    public SubjectViewModel()
    {

    }

    ObservableCollection<StatusViewModel> statusViewModels;

    private Subject subject;

    private string name;

    private string description;

    public Guid Identifier
    {
      get;
      private set;
    }

    public string Name
    {
      get => this.name;
      protected set
      {
        if (this.name != value)
        {
          this.name = value;
          this.OnPropertyChanged();
        }
      }
    }

    public string Description
    {
      get => this.description;
      protected set
      {
        if (this.description != value)
        {
          this.description = value;
          this.OnPropertyChanged();
        }
      }
    }

    public StatusViewModel CurrentStatus { get; private set; }

    //ToDo DGO: implement snapshot support.
    //public ObservableCollection<StatusViewModel> SubjectViewModels => this.statusViewModels ?? (this.statusViewModels = new ObservableCollection<StatusViewModel>());

    public void Init(Subject subject)
    {
      this.subject = subject;
      this.Identifier = subject.Identifier;
      this.CurrentStatus = this.GetViewModelForStatus(subject);
    }

    public virtual void Update(Subject changedSubject)
    {
      this.Name = changedSubject.Name;
      this.Description = changedSubject.Description;
      this.CurrentStatus.Update(changedSubject.CurrentStatus);
    }

    protected virtual StatusViewModel GetViewModelForStatus(Subject subject)
    {
      StatusViewModel result = new StatusViewModel();
      //result = subject is JenkinsProject ? new JenkinsStatusViewModel() : new StatusViewModel();

      result.Update(subject.CurrentStatus);

      return result;
    }
  }
}
