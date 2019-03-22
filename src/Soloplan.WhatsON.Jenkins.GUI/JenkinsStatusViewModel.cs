namespace Soloplan.WhatsON.Jenkins.GUI
{
  using System;
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.SubjectTreeView;

  class JenkinsStatusViewModel : StatusViewModel
  {
    private TimeSpan estimatedDuration;
    private TimeSpan duration;
    private bool building;
    private int? buildNumber;

    private int progres;

    /// <summary>
    /// Backing field for <see cref="OpenWebPage"/>.
    /// </summary>
    private OpenWebPageCommand openBuildPage;

    public JenkinsStatusViewModel(JenkinsProjectViewModel model)
      : base(model)
    {
    }

    /// <summary>
    /// Command for opening builds webPage.
    /// </summary>
    public ICommand OpenBuildPage => this.openBuildPage ?? (this.openBuildPage = new OpenWebPageCommand());

    public int? BuildNumber
    {
      get
      {
        return this.buildNumber;
      }

      protected set
      {
        if (this.buildNumber != value)
        {
          this.buildNumber = value;
          this.OnPropertyChanged();
        }
      }
    }

    public bool Building
    {
      get
      {
        return this.building;
      }

      protected set
      {
        if (this.building != value)
        {
          this.building = value;
          this.OnPropertyChanged();
        }
      }
    }

    public TimeSpan Duration
    {
      get
      {
        return this.duration;
      }

      protected set
      {
        if (this.duration != value)
        {
          this.duration = value;
          this.OnPropertyChanged();
        }
      }
    }

    public TimeSpan EstimatedDuration
    {
      get
      {
        return this.estimatedDuration;
      }

      protected set
      {
        if (this.estimatedDuration != value)
        {
          this.estimatedDuration = value;
          this.OnPropertyChanged();
        }
      }
    }

    public int Progres
    {
      get
      {
        return this.progres;
      }

      protected set
      {
        if (this.progres != value)
        {
          this.progres = value;
          this.OnPropertyChanged();
        }
      }
    }

    public override void Update(Status newStatus)
    {
      base.Update(newStatus);
      if (newStatus == null)
      {
        this.BuildNumber = null;
        return;
      }

      if (newStatus.Properties.TryGetValue(JenkinsProject.BuildPropertyKeys.Number, out var buildNubmerString) && int.TryParse(buildNubmerString, out var buildNubmer))
      {
        this.BuildNumber = buildNubmer;
      }

      if (newStatus.Properties.TryGetValue(JenkinsProject.BuildPropertyKeys.Building, out var buildingString) && bool.TryParse(buildingString, out var isBuilding))
      {
        this.Building = isBuilding;
      }

      if (newStatus.Properties.TryGetValue(JenkinsProject.BuildPropertyKeys.Duration, out var durationString) && long.TryParse(durationString, out var durationInMs))
      {
        this.Duration = new TimeSpan(durationInMs * 10000);
      }

      if (newStatus.Properties.TryGetValue(JenkinsProject.BuildPropertyKeys.EstimatedDuration, out var estimatedDurationString) && long.TryParse(estimatedDurationString, out var estimatedDurationInMs))
      {
        this.EstimatedDuration = new TimeSpan(estimatedDurationInMs * 10000);
      }

      if (this.State == ObservationState.Running)
      {
        var elapsedSinceStart = (DateTime.UtcNow - this.Time).TotalSeconds;
        this.Progres = (int)((100 * elapsedSinceStart) / this.EstimatedDuration.TotalSeconds);
      }
      else
      {
        this.Progres = 100;
      }
    }

    public void SetJobAddress(string address)
    {
      if (this.openBuildPage == null)
      {
        this.openBuildPage = new OpenWebPageCommand();
      }

      this.openBuildPage.Address = address + "/" + this.BuildNumber;
    }

    private class OpenWebPageCommand : ICommand
    {
      private string address;

      public event EventHandler CanExecuteChanged;

      public string Address
      {
        get
        {
          return this.address;
        }

        set
        {
          if (this.address != value)
          {
            this.address = value;
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
          }
        }
      }

      public bool CanExecute(object parameter)
      {
        return !string.IsNullOrEmpty(this.address);
      }

      public void Execute(object parameter)
      {
        System.Diagnostics.Process.Start(this.Address);
      }
    }
  }
}
