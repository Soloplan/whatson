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

  /// <summary>
  /// Main class of application. Executes specialized plugins in desired intervals.
  /// </summary>
  public class ObservationScheduler
  {
    /// <summary>
    /// Default interval for polling subjects for status.
    /// </summary>
    private const int DefaultPollInterval = 5;

    /// <summary>
    /// List of currently observed subjects.
    /// </summary>
    private readonly IList<ObservationSubject> observedSubjects;

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
      this.observedSubjects = new List<ObservationSubject>();
    }

    /// <summary>
    /// Fired when subjects status was polled.
    /// </summary>
    public event EventHandler<Subject> StatusQueried;

    public bool Running
    {
      get => this.observedSubjects.Any(subject => subject.Running);
    }

    public void Start()
    {
      if (!this.Running && !this.stopping)
      {
        foreach (var observationSubject in this.observedSubjects)
        {
          observationSubject.Running = true;
        }

        if (this.cancellationTokenSource == null || this.cancellationTokenSource.IsCancellationRequested)
        {
          this.cancellationTokenSource = new CancellationTokenSource();
        }

        foreach (var observationSubject in this.observedSubjects)
        {
          this.StartObserveSingle(observationSubject, this.cancellationTokenSource.Token);
        }
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

    public void Observe(Subject subject, int interval = DefaultPollInterval)
    {
      if (subject == null)
      {
        return;
      }

      if (this.observedSubjects.Any(s => s.Subject.Equals(subject)))
      {
        // subject is already being observed
        return;
      }

      this.observedSubjects.Add(new ObservationSubject(subject, interval));
    }

    /// <summary>
    /// Stops observing all subjects. The scheduler should be stopped before calling this function.
    /// </summary>
    public void UnobserveAll()
    {
      if (this.Running || this.stopping)
      {
        throw new InvalidOperationException("Can't stop observing while observation is running.");
      }

      this.observedSubjects.Clear();
    }

    /// <summary>
    /// Observes single subject. Creates asynchronous loop.
    /// </summary>
    /// <param name="subject">Subject to observe.</param>
    /// <param name="token">Cancellation token passed to subject.</param>
    /// <remarks>See https://blogs.msdn.microsoft.com/benwilli/2016/06/30/asynchronous-infinite-loops-instead-of-timers/ for look idea description.</remarks>
    /// <returns>Not used.</returns>
    private async Task StartObserveSingle(ObservationSubject subject, CancellationToken token)
    {
      while (subject.Running)
      {
        if (DateTime.Now - subject.LastPoll > subject.Interval)
        {
          await subject.Subject.QueryStatus(token);
          subject.LastPoll = DateTime.Now;
          if (subject.Running)
          {
            this.StatusQueried?.Invoke(this, subject.Subject);
          }
        }

        var remainingOfInterval = subject.Interval - (DateTime.Now - subject.LastPoll);
        if (remainingOfInterval.TotalMilliseconds > 0 && subject.Running)
        {
          int milisecondsToWait = remainingOfInterval.TotalMilliseconds < int.MaxValue ? (int)remainingOfInterval.TotalMilliseconds : int.MaxValue;
          await Task.Delay(milisecondsToWait, token);
        }
      }
    }

    /// <summary>
    /// Stops observation of subjects.
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
        this.stopping = true;
        foreach (var observationSubject in this.observedSubjects)
        {
          observationSubject.Running = false;
        }

        if (force)
        {
          this.cancellationTokenSource.Cancel();
        }
      }
      finally
      {
        this.stopping = false;
      }
    }

    /// <summary>
    /// Helper class representing the subject being observed.
    /// </summary>
    private class ObservationSubject
    {
      /// <summary>
      /// Initializes a new instance of the <see cref="ObservationSubject"/> class.
      /// </summary>
      /// <param name="subject">The subject which should be observed.</param>
      /// <param name="interval">Observation interval.</param>
      public ObservationSubject(Subject subject, int interval)
      {
        this.Subject = subject;
        this.Interval = TimeSpan.FromSeconds(interval);
      }

      /// <summary>
      /// Gets or sets a value indicating whether the subject should currently be observed.
      /// </summary>
      /// <remarks>
      /// The subjects loop may be still running even though this property is false. The subject should not send any events nor trigger new requests
      /// when this is false.
      /// </remarks>
      public bool Running { get; set; }

      /// <summary>
      /// Gets the subject being observed.
      /// </summary>
      public Subject Subject { get; }

      /// <summary>
      /// Gets interval in which the subject should be polled for status.
      /// </summary>
      public TimeSpan Interval { get; }

      /// <summary>
      /// Gets or sets date the subject was last polled.
      /// </summary>
      public DateTime LastPoll { get; set; }
    }
  }
}
