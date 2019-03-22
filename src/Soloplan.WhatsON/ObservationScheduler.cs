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

    private bool running;

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

    public event EventHandler ObservationRunStarted;

    public event EventHandler ObservationRunEnded;

    public bool Running
    {
      get => this.running;
      private set => this.running = value;
    }

    public void Start()
    {
      if (!this.Running && !this.stopping)
      {
        this.Running = true;
        this.cancellationTokenSource = new CancellationTokenSource();
        foreach (var observationSubject in this.observedSubjects)
        {
          this.Observe(observationSubject, this.cancellationTokenSource.Token);
        }
      }
    }

    public void Stop()
    {
      this.Terminate();
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
      while (this.Running)
      {
        if (DateTime.Now - subject.LastPoll > subject.Interval)
        {
          this.ObservationRunStarted?.Invoke(this, EventArgs.Empty);
          await this.ObserveSingle(subject, token);
          this.ObservationRunEnded?.Invoke(this, EventArgs.Empty);
        }

        await Task.Delay(1000, token);
      }
    }

    private async Task ObserveSingle(ObservationSubject subject, CancellationToken token)
    {
      await subject.Subject.QueryStatus(token);
      subject.LastPoll = DateTime.Now;
      this.StatusQueried?.Invoke(this, subject.Subject);
    }

    private void Terminate()
    {
      if (!this.Running)
      {
        return;
      }

      try
      {
        this.stopping = true;
        this.Running = false;
        this.cancellationTokenSource.Cancel();
      }
      finally
      {
        this.stopping = false;
      }
    }

    public class ObservationSubject
    {
      public ObservationSubject(Subject subject, int interval)
      {
        this.Subject = subject;
        this.Interval = TimeSpan.FromSeconds(interval);
      }

      public Subject Subject { get; }

      public TimeSpan Interval { get; }

      public DateTime LastPoll { get; set; }
    }
  }
}
