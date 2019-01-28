namespace Soloplan.WhatsON
{
  using System.Collections.Generic;

  public abstract class Subject
  {
    private const int MaxSnapshotsDefault = 5;

    protected Subject(string name)
    {
      this.Snapshots = new Queue<Snapshot>();
      this.Configuration = new Dictionary<string, string>();
      this.MaxSnapshots = MaxSnapshotsDefault;
      this.Name = name;
    }

    public string Type => this.GetType().Name;

    public string Name { get; }

    public string Description { get; set; }

    public string Category { get; set; }

    public IDictionary<string, string> Configuration { get; }

    public Status CurrentStatus { get; protected set; }

    public int MaxSnapshots { get; set; }

    public Queue<Snapshot> Snapshots { get; }

    public void QueryStatus(params string[] args)
    {
      this.ExecuteQuery(args);

      if (this.CurrentStatus != null && this.ShouldTakeSnapshot(this.CurrentStatus))
      {
        this.AddSnapshot(this.CurrentStatus);
      }
    }

    public void AddSnapshot(Status status)
    {
      while (this.Snapshots.Count >= this.MaxSnapshots)
      {
        this.Snapshots.Dequeue();
      }

      this.Snapshots.Enqueue(new Snapshot(status));
    }

    protected abstract void ExecuteQuery(params string[] args);

    protected virtual bool ShouldTakeSnapshot(Status status)
    {
      return false;
    }
  }
}