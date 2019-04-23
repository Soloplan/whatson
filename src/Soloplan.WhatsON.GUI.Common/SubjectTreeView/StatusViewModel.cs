// <copyright file="StatusViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.SubjectTreeView
{
  using System;
  using NLog;

  /// <summary>
  /// Base ViewModel used for any kind of <see cref="Subject"/>;
  /// </summary>
  public class StatusViewModel : NotifyPropertyChanged
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    public StatusViewModel(SubjectViewModel subject)
    {
      this.Parent = subject;
    }

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

    public SubjectViewModel Parent { get; }

    public virtual void Update(Status newStatus)
    {
      log.Trace("Updating status model {model}", new { Name = this.Name, Details = this.Details });
      if (newStatus == null)
      {
        this.Name = "Loading...";
        this.Details = "Loading...";
        this.state = ObservationState.Unknown;
        return;
      }

      this.Name = newStatus.Name;
      this.Details = newStatus.Detail;
      this.Time = newStatus.Time.ToLocalTime();
      this.State = newStatus.State;
    }
  }
}