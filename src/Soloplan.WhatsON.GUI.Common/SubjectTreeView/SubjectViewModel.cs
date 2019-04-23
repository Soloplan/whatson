namespace Soloplan.WhatsON.GUI.Common.SubjectTreeView
{
  using System;
  using System.Collections.ObjectModel;
  using System.Windows.Input;
  using NLog;

  public class SubjectViewModel : NotifyPropertyChanged, IHandleDoubleClick
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private ObservableCollection<StatusViewModel> subjectSnapshots;

    private string name;

    private string description;

    private StatusViewModel currentStatus;

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

    public StatusViewModel CurrentStatus
    {
      get => this.currentStatus;
      private set
      {
        if (!object.ReferenceEquals(this.currentStatus, value))
        {
          this.currentStatus = value;
          this.OnPropertyChanged();
        }
      }
    }

    public Guid Identifier { get; private set; }

    public ObservableCollection<StatusViewModel> SubjectSnapshots => this.subjectSnapshots ?? (this.subjectSnapshots = new ObservableCollection<StatusViewModel>());

    public Subject Subject { get; private set; }

    public virtual void Init(SubjectConfiguration configuration)
    {
      this.Identifier = configuration.Identifier;
      this.Name = configuration.Name;
      this.CurrentStatus = this.GetViewModelForStatus();
      log.Debug("Initializing {type}, {instance}.", this.GetType(), new { Name = this.Name, Identifier = this.Identifier });
    }

    public virtual void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
    }

    public virtual void Update(Subject changedSubject)
    {
      log.Trace("Updating model {model}", new { Name = this.Name, Identifier = this.Identifier });

      if (this.Subject == null)
      {
        this.Subject = changedSubject;
      }

      this.Description = changedSubject.Description;
      this.CurrentStatus.Update(changedSubject.CurrentStatus);

      int i = 0;
      bool clearList = false;
      foreach (var changedSubjectSnapshot in changedSubject.Snapshots)
      {
        if (i >= this.SubjectSnapshots.Count || this.SubjectSnapshots[i].Time.ToUniversalTime() != changedSubjectSnapshot.Status.Time)
        {
          log.Debug("Rebuilding list of history builds for model {type}, {instance}.", this.GetType(), new { Name = this.Name, Identifier = this.Identifier });
          clearList = true;
          break;
        }

        i++;
      }

      if (clearList)
      {
        this.SubjectSnapshots.Clear();
        foreach (var subjectSnapshot in changedSubject.Snapshots)
        {
          var subjectSnapshotViewModel = this.GetViewModelForStatus();
          subjectSnapshotViewModel.Update(subjectSnapshot.Status);
          this.SubjectSnapshots.Add(subjectSnapshotViewModel);
        }
      }
    }

    protected virtual StatusViewModel GetViewModelForStatus()
    {
      StatusViewModel result = new StatusViewModel(this);

      return result;
    }
  }
}
