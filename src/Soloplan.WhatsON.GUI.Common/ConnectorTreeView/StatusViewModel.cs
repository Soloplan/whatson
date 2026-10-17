// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StatusViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.ComponentModel;
  using NLog;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Base ViewModel used for any kind of <see cref="Soloplan.WhatsON.Connector" />.
  /// </summary>
  public partial class StatusViewModel : INotifyPropertyChanged
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public StatusViewModel(ConnectorViewModel connector)
    {
      this.Parent = connector;
    }

    public bool First { get; set; }

    public int Size => this.First ? 8 : 6;

    public string? Name { get; protected set; }

    public virtual string? Label { get; protected set; }

    public string? Details { get; protected set; }

    public ObservationState State { get; set; }

    public virtual DateTime Time { get; protected set; }

    public bool Failure { get; set; }

    public bool Unknown { get; set; }

    public bool Succees { get; set; }

    public bool Unstable { get; set; }

    public string? ErrorMessage { get; set; }

    public ConnectorViewModel Parent { get; }

    public virtual void Update(Status? newStatus)
    {
      log.Trace("Updating status model {model}", new { this.Name, this.Details });
      if (newStatus == null)
      {
        this.Name = Strings.Loading;
        this.Details = Strings.Loading;
        this.State = ObservationState.Unknown;
        return;
      }

      this.Name = newStatus.Name;
      this.Details = newStatus.Details;
      this.Time = newStatus.Time;
      this.State = newStatus.State;
    }

    /// <summary>
    /// Updates flags used to control visibility of controls based on <see cref="State" />.
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
