namespace Soloplan.WhatsON
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;

  public class ObservationScheduler
  {
    private const int DefaultPollInterval = 5;
    private readonly IList<ObservationSubject> observedSubjects;

    /// <summary>
    /// Observation is stopping.
    /// </summary>
    private bool stopping;

    private CancellationTokenSource cancellationTokenSource;

    public ObservationScheduler()
    {
      this.observedSubjects = new List<ObservationSubject>();
    }

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

        this.cancellationTokenSource = new CancellationTokenSource();
        foreach (var observationSubject in this.observedSubjects)
        {
          this.Observe(observationSubject, this.cancellationTokenSource.Token);
        }
      }
    }

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

    public void UnobserveAll()
    {
      if (this.Running || this.stopping)
      {
        throw new InvalidOperationException("Can't stop observing while observation is running.");
      }

      this.observedSubjects.Clear();
    }

    private async Task Observe(ObservationSubject subject, CancellationToken token)
    {
      while (subject.Running)
      {
        if (DateTime.Now - subject.LastPoll > subject.Interval)
        {
          try
          {
            await this.ObserveSingle(subject, token);
          }
          catch (Exception e)
          {
            // Todo: should be logged.
          }
        }

        try
        {
          var remainingOfInterval = subject.Interval - (DateTime.Now - subject.LastPoll);
          if (remainingOfInterval.TotalMilliseconds > 0 && subject.Running)
          {
            int milisecondsToWait = remainingOfInterval.TotalMilliseconds < int.MaxValue ? (int)remainingOfInterval.TotalMilliseconds : int.MaxValue;
            await Task.Delay(milisecondsToWait, token);
          }
        }
        catch (Exception e)
        {
          // Todo: should be logged.
        }
      }
    }

    private async Task ObserveSingle(ObservationSubject subject, CancellationToken token)
    {
      await subject.Subject.QueryStatus(token);
      subject.LastPoll = DateTime.Now;
      if (subject.Running)
      {
        this.StatusQueried?.Invoke(this, subject.Subject);
      }
    }

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

    private class ObservationSubject
    {
      public ObservationSubject(Subject subject, int interval)
      {
        this.Subject = subject;
        this.Interval = TimeSpan.FromSeconds(interval);
      }

      public bool Running { get; set; }

      public Subject Subject { get; }

      public TimeSpan Interval { get; }

      public DateTime LastPoll { get; set; }
    }
  }
}
