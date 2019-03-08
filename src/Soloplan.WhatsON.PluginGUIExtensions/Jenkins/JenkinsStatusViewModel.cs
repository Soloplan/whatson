namespace Soloplan.WhatsON.PluginGUIExtensions.Jenkins
{
  using System;
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.SubjectTreeView;

  // ToDo DGO: The class should be in Soloplan.WhatsON.Jenknis, but I want first to get some workign prototype.
  class JenkinsStatusViewModel : StatusViewModel
  {
    private TimeSpan estimatedDuration;
    private TimeSpan duration;
    private bool building;
    private int buildNumber;

    private int progres;

    public int BuildNumber
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
      if (newStatus.Properties.TryGetValue(BuildPropertyKeys.Number, out var buildNubmerString) && int.TryParse(buildNubmerString, out var buildNubmer))
      {
        this.BuildNumber = buildNubmer;
      }

      if (newStatus.Properties.TryGetValue(BuildPropertyKeys.Building, out var buildingString) && bool.TryParse(buildingString, out var isBuilding))
      {
        this.Building = isBuilding;
      }

      if (newStatus.Properties.TryGetValue(BuildPropertyKeys.Duration, out var durationString) && long.TryParse(durationString, out var durationInMs))
      {
        this.Duration = new TimeSpan(durationInMs * 10000);
      }

      if (newStatus.Properties.TryGetValue(BuildPropertyKeys.EstimatedDuration, out var estimatedDurationString) && long.TryParse(estimatedDurationString, out var estimatedDurationInMs))
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

    //TODO DGO: Remove this is duplicated code
    public static class BuildPropertyKeys
    {
      public const string Number = "BuildNumber";
      public const string Building = "Building";
      public const string Duration = "Duration";
      public const string EstimatedDuration = "EstimatedDuration";
    }
  }
}
