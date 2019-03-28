namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;
  using System.Collections.ObjectModel;
  using System.Windows.Input;

  public class SubjectViewModel : NotifyPropertyChanged, IHandleDoubleClick
  {
    ObservableCollection<StatusViewModel> subjectSnapshots;

    private string name;

    private string description;

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

    public Guid Identifier { get; private set; }

    public ObservableCollection<StatusViewModel> SubjectSnapshots => this.subjectSnapshots ?? (this.subjectSnapshots = new ObservableCollection<StatusViewModel>());

    public Subject Subject { get; private set; }

    public void Init(Subject subject)
    {
      this.Subject = subject;
      this.Identifier = subject.SubjectConfiguration.Identifier;
      this.CurrentStatus = this.GetViewModelForStatus(subject.CurrentStatus);
      foreach (var subjectSnapshot in subject.Snapshots)
      {
        var subjectSnapshotViewModel = this.GetViewModelForStatus(subjectSnapshot.Status);
        this.SubjectSnapshots.Add(subjectSnapshotViewModel);
      }
    }

    public virtual void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
    }

    public virtual void Update(Subject changedSubject)
    {
      this.Name = changedSubject.SubjectConfiguration.Name;
      this.Description = changedSubject.Description;
      this.CurrentStatus.Update(changedSubject.CurrentStatus);

      int i = 0;
      bool clearList = false;
      foreach (var changedSubjectSnapshot in changedSubject.Snapshots)
      {
        if (i >= this.SubjectSnapshots.Count || this.SubjectSnapshots[i].Time.ToUniversalTime() != changedSubjectSnapshot.Status.Time)
        {
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
          var subjectSnapshotViewModel = this.GetViewModelForStatus(subjectSnapshot.Status);
          this.SubjectSnapshots.Add(subjectSnapshotViewModel);
        }
      }
    }

    protected virtual StatusViewModel GetViewModelForStatus(Status status)
    {
      StatusViewModel result = new StatusViewModel(this);
      result.Update(status);

      return result;
    }
  }
}
