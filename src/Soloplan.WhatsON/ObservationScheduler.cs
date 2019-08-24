// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NumericConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//    Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using NLog;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Main class of application. Executes specialized plugins in desired intervals.
  /// </summary>
  public class ObservationScheduler
  {
    /// <summary>
    /// Default interval for polling connectors for status.
    /// </summary>
    private const int DefaultPollInterval = 5;

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// List of currently observed connectors.
    /// </summary>
    private readonly IList<ObservationConnector> observedConnectors;

    /// <summary>
    /// Observation is stopping.
    /// </summary>
    private bool stopping;

    /// <summary>
    /// Cancellation token used when all open connections should be forcefully closed.
    /// </summary>
    private CancellationTokenSource cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservationScheduler"/> class.
    /// </summary>
    public ObservationScheduler()
    {
      log.Debug("Creating ObservationScheduler.");
      this.observedConnectors = new List<ObservationConnector>();
    }

    /// <summary>
    /// Fired when connectors status was polled.
    /// </summary>
    public event EventHandler<Connector> StatusQueried;

    public bool Running => this.observedConnectors.Any(connector => connector.Running);

    public void Start()
    {
      log.Debug("Start of observation requested.");
      if (!this.Running && !this.stopping)
      {
        log.Debug("Starting observation...");
        foreach (var observationConnector in this.observedConnectors)
        {
          observationConnector.Running = true;
        }

        if (this.cancellationTokenSource == null || this.cancellationTokenSource.IsCancellationRequested)
        {
          log.Debug("Creating new CancelationToken.");
          this.cancellationTokenSource = new CancellationTokenSource();
        }

        foreach (var observationConnector in this.observedConnectors)
        {
          log.Log(LogLevel.Debug, "Starting observation for {@connector}.", observationConnector);
          this.StartObserveSingle(observationConnector, this.cancellationTokenSource.Token);
        }

        log.Debug("Observation started.");
      }
    }

    /// <summary>
    /// Stops observation scheduler.
    /// </summary>
    /// <param name="force">If true forces all tasks to stop; otherwise allows tasks to finish their current run.</param>
    public void Stop(bool force)
    {
      this.Terminate(force);
    }

    public void Observe(Connector connector, int interval = DefaultPollInterval)
    {
      if (connector == null)
      {
        return;
      }

      if (this.observedConnectors.Any(s => s.Connector.Equals(connector)))
      {
        // connector is already being observed
        log.Warn("Connector {connector} is already being observed, skip adding.", new { Interval = interval, Name = connector.ConnectorConfiguration.Name, CurrentStatus = connector.CurrentStatus });
        return;
      }

      var observationConnector = new ObservationConnector(connector, interval);
      this.observedConnectors.Add(observationConnector);
      log.Log(LogLevel.Debug, "Observation connector {@connector} added.", observationConnector);
    }

    /// <summary>
    /// Stops observing all connectors. The scheduler should be stopped before calling this function.
    /// </summary>
    public void UnobserveAll()
    {
      if (this.Running || this.stopping)
      {
        throw new InvalidOperationException("Can't stop observing while observation is running.");
      }

      this.observedConnectors.Clear();
    }

    /// <summary>
    /// Observes single connector. Creates asynchronous loop.
    /// </summary>
    /// <param name="connector">Connector to observe.</param>
    /// <param name="token">Cancellation token passed to connector.</param>
    /// <remarks>See https://blogs.msdn.microsoft.com/benwilli/2016/06/30/asynchronous-infinite-loops-instead-of-timers/ for look idea description.</remarks>
    /// <returns>Not used.</returns>
    private async Task StartObserveSingle(ObservationConnector connector, CancellationToken token)
    {
      while (connector.Running)
      {
        try
        {
          if (DateTime.Now - connector.LastPoll > connector.Interval)
          {
            log.Log(LogLevel.Trace, "Observation of {@connector} started.", connector);
            connector.LastPoll = DateTime.Now;
            await connector.Connector.QueryStatus(token);
            if (connector.Running)
            {
              this.StatusQueried?.Invoke(this, connector.Connector);
            }

            log.Log(LogLevel.Trace, "Observation of {@connector} ended.", connector);
          }

          var remainingOfInterval = connector.Interval - (DateTime.Now - connector.LastPoll);
          if (remainingOfInterval.TotalMilliseconds > 0 && connector.Running)
          {
            int milisecondsToWait = remainingOfInterval.TotalMilliseconds < int.MaxValue ? (int)remainingOfInterval.TotalMilliseconds : int.MaxValue;
            await Task.Delay(milisecondsToWait, token);
          }
        }
        catch (Exception e)
        {
          log.Error(e, "Exception occurred when observing connector {connector}", new { Interval = connector.Interval, Name = connector.Connector.ConnectorConfiguration.Name, CurrentStatus = connector.Connector.CurrentStatus });
        }
      }

      log.Log(LogLevel.Debug, "Exiting observation loop for {@connector}.", connector);
    }

    /// <summary>
    /// Stops observation of connectors.
    /// </summary>
    /// <param name="force">If true cancellation token is used to cancel running observations;
    /// otherwise the observation are informed that they should stop, but are allowed to run until they end current operation/finish waiting for next poll interval.</param>
    private void Terminate(bool force)
    {
      if (!this.Running)
      {
        return;
      }

      try
      {
        log.Debug("Stopping observation. force = {force}.", force);
        this.stopping = true;
        foreach (var observationConnector in this.observedConnectors)
        {
          log.Log(LogLevel.Debug, "Stopping observation of connector {@connector}.", observationConnector);
          observationConnector.Running = false;
        }

        if (force)
        {
          log.Warn("Canceling any still running tasks.");
          this.cancellationTokenSource.Cancel();
          log.Warn("Task canceled.");
        }

        log.Debug("Scheduler stopped.");
      }
      finally
      {
        this.stopping = false;
      }
    }

    /// <summary>
    /// Helper class representing the connector being observed.
    /// </summary>
    private class ObservationConnector
    {
      /// <summary>
      /// Initializes a new instance of the <see cref="ObservationConnector"/> class.
      /// </summary>
      /// <param name="connector">The connector which should be observed.</param>
      /// <param name="interval">Observation interval.</param>
      public ObservationConnector(Connector connector, int interval)
      {
        this.Connector = connector;
        this.Interval = TimeSpan.FromSeconds(interval);
      }

      /// <summary>
      /// Gets or sets a value indicating whether the connector should currently be observed.
      /// </summary>
      /// <remarks>
      /// The connectors loop may be still running even though this property is false. The connector should not send any events nor trigger new requests
      /// when this is false.
      /// </remarks>
      public bool Running { get; set; }

      /// <summary>
      /// Gets the connector being observed.
      /// </summary>
      public Connector Connector { get; }

      /// <summary>
      /// Gets interval in which the connector should be polled for status.
      /// </summary>
      public TimeSpan Interval { get; }

      /// <summary>
      /// Gets or sets date the connector was last polled.
      /// </summary>
      public DateTime LastPoll { get; set; }
    }
  }
}
