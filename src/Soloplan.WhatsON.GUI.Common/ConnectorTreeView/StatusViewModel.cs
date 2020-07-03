// <copyright file="StatusViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using NLog;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Base ViewModel used for any kind of <see cref="Soloplan.WhatsON.Connector"/>.
  /// </summary>
  public class StatusViewModel : NotifyPropertyChanged
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    private string name;

    private string details;

    private DateTime time;

    private ObservationState state;

    private bool failure;

    private bool unknown;

    private bool succees;

    private bool unstable;

    private bool first;

    private string label;

    private string errorMessage;

    public StatusViewModel(ConnectorViewModel connector)
    {
      this.Parent = connector;
    }

    public bool First
    {
      get => this.first;
      set
      {
        if (this.first != value)
        {
          this.first = value;
          this.OnPropertyChanged();
        }
      }
    }

    public int Size
    {
      get
      {
        return this.First ? 6 : 4;
      }
    }

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

    public virtual string Label
    {
      get
      {
        return this.label;
      }

      protected set
      {
        if (this.label != value)
        {
          this.label = value;
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

    public virtual DateTime Time
    {
      get
      {
        return this.time;
      }

      protected  set
      {
        this.time = value;
        this.OnPropertyChanged();
      }
    }

    public bool Failure
    {
      get => this.failure;
      set
      {
        this.failure = value;
        this.OnPropertyChanged();
      }
    }

    public bool Unknown
    {
      get => this.unknown;
      set
      {
        this.unknown = value;
        this.OnPropertyChanged();
      }
    }

    public bool Succees
    {
      get => this.succees;
      set
      {
        this.succees = value;
        this.OnPropertyChanged();
      }
    }

    public bool Unstable
    {
      get => this.unstable;
      set
      {
        this.unstable = value;
        this.OnPropertyChanged();
      }
    }

    public string ErrorMessage
    {
      get => this.errorMessage;
      set
      {
        this.errorMessage = value;
        this.OnPropertyChanged();
      }
    }

    public ConnectorViewModel Parent { get; }

    public virtual void Update(Status newStatus)
    {
      log.Trace("Updating status model {model}", new { this.Name, Details = this.Details });
      if (newStatus == null)
      {
        this.Name = "Loading...";
        this.Details = "Loading...";
        this.state = ObservationState.Unknown;
        return;
      }

      this.Name = newStatus.Name;
      this.Details = newStatus.Details;
      this.Time = newStatus.Time;
      this.State = newStatus.State;
    }

    /// <summary>
    /// Updates flags used to control visibility of controls based on <see cref="State"/>.
    /// </summary>
    protected virtual void UpdateStateFlags()
    {
      this.Succees = false;
      this.Failure = false;
      this.Unknown = false;
      this.Unstable = false;

      switch (this.State)
      {
        case ObservationState.Unknown:
          this.Unknown = true;
          break;
        case ObservationState.Unstable:
          this.Unstable = true;
          break;
        case ObservationState.Failure:
          this.Failure = true;
          break;
        case ObservationState.Success:
          this.Succees = true;
          break;
      }
    }
  }
}