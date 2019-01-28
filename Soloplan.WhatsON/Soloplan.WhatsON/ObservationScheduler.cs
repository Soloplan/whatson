namespace Soloplan.WhatsON
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;

  public class ObservationScheduler
  {
    private const int DefaultPollInterval = 5;
    private readonly IList<ObservationSubject> observedSubjects;
    private readonly Thread schedulerThread;

    private bool running;

    public ObservationScheduler()
    {
      this.observedSubjects = new List<ObservationSubject>();
      this.schedulerThread = new Thread(this.Observe);
      AppDomain.CurrentDomain.ProcessExit += (e, a) =>
      {
        this.Terminate();
      };
    }

    public void Start()
    {
      this.running = true;
      this.schedulerThread.Start();
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

    private void Observe()
    {
      while (this.running)
      {
        foreach (var subject in this.observedSubjects)
        {
          if (subject.Subject.CurrentStatus == null || DateTime.Now - subject.Subject.CurrentStatus.Time > subject.Interval)
          {
            subject.Subject.QueryStatus();
          }
        }

        Thread.Sleep(1000);
      }
    }

    private void Terminate()
    {
      this.running = false;
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
    }
  }
}
