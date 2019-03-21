namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  /// <summary>
  /// Base ViewModel used for any kind of <see cref="Subject"/>;
  /// </summary>
  //Todo DGO: Move to some more general location
  public class StatusViewModel : ViewModelBase
  {
    private string name;

    private string details;

    private DateTime time;

    private ObservationState state;

    public string Name
    {
      get
      {
        return this.name;
      }

      protected set
      {
        if (this.name != value)
        {
          this.name = value;
          this.OnPropertyChanged();
        }
      }
    }

    public string Details
    {
      get
      {
        return this.details;
      }

      protected set
      {
        if (this.details != value)
        {
          this.details = value;
          this.OnPropertyChanged();
        }
      }
    }

    public ObservationState State
    {
      get => this.state;

      set
      {
        if (this.state != value)
        {
          this.state = value;
          this.OnPropertyChanged();
        }
      }
    }

    public DateTime Time
    {
      get
      {
        return this.time;
      }

      protected set
      {
        if (this.time != value)
        {
          this.time = value;
          this.OnPropertyChanged();
        }
      }
    }

    public virtual void Update(Status newStatus)
    {
      if (newStatus == null)
      {
        this.Name = "Loading...";
        this.Details = "Loading...";
        this.state = ObservationState.Unknown;
        return;
      }

      this.Name = newStatus.Name;
      this.Details = newStatus.Detail;
      this.Time = newStatus.Time;
      this.State = newStatus.State;
    }
  }
}