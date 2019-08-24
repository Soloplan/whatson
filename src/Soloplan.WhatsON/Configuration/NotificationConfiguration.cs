// // --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationConfiguration.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.Configuration
{
  using System;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// The notification configuration used for global options and as base type for <see cref="ConnectorNotificationConfiguration"/>.
  /// </summary>
  public class NotificationConfiguration
  {
    /// <summary>
    /// Gets or sets a value indicating whether unstable notifications are enabled.
    /// </summary>
    public bool UnstableNotificationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether running notifications are enabled.
    /// </summary>
    public bool RunningNotificationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether failure notifications are enabled.
    /// </summary>
    public bool FailureNotificationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether success notifications are enabled.
    /// </summary>
    public bool SuccessNotificationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether unstable notifications are enabled.
    /// </summary>
    public bool UnknownNotificationEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the notification should be executed only if status is changed.
    /// </summary>
    public bool OnlyIfChanged { get; set; } = false;

    /// <summary>
    /// RTeturns flag of <see cref="ObservationState"/> enabled state in the configuration.
    /// </summary>
    /// <param name="observationState">State of the observation.</param>
    /// <returns>True if given <see cref="ObservationState"/> is activated in the configuration, otherwise false.</returns>
    public bool AsObservationStateFlag(ObservationState observationState)
    {
      switch (observationState)
      {
        case ObservationState.Unstable:
          return this.UnstableNotificationEnabled;
        case ObservationState.Failure:
          return this.FailureNotificationEnabled;
        case ObservationState.Success:
          return this.SuccessNotificationEnabled;
        case ObservationState.Running:
          return this.RunningNotificationEnabled;
        case ObservationState.Unknown:
          return this.UnknownNotificationEnabled;
        default:
          throw new ArgumentOutOfRangeException(nameof(observationState), observationState, null);
      }
    }

    /// <summary>
    /// Assigns from configuration setting based on <see cref="ObservationState"/>.
    /// </summary>
    /// <param name="observationState">The <see cref="ObservationState"/>.</param>
    /// <param name="isEnabled">if set to <c>true</c> setting related to given <see cref="observationState"/> will be enabled; otherwise disabled.</param>
    public void AssignFromObeservationStateActivity(ObservationState observationState, bool isEnabled)
    {
      switch (observationState)
      {
        case ObservationState.Failure:
          this.FailureNotificationEnabled = isEnabled;
          break;
        case ObservationState.Running:
          this.RunningNotificationEnabled = isEnabled;
          break;
        case ObservationState.Success:
          this.SuccessNotificationEnabled = isEnabled;
          break;
        case ObservationState.Unstable:
          this.UnstableNotificationEnabled = isEnabled;
          break;
        case ObservationState.Unknown:
          this.UnknownNotificationEnabled = isEnabled;
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(observationState), observationState, null);
      }
    }
  }
}