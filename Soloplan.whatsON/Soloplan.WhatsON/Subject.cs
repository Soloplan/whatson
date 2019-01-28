namespace Soloplan.WhatsON
{
  using System.Collections.Generic;

  public abstract class Subject
  {
    private const int MaxSnapshotsDefault = 5;
    private readonly Queue<Snapshot> snapshots;

    protected Subject(string name)
    {
      this.snapshots = new Queue<Snapshot>();
      this.Configuration = new Dictionary<string, string>();
      this.MaxSnapshots = MaxSnapshotsDefault;
      this.Name = name;
    }

    public string Name { get; }

    public string Description { get; set; }

    public Category Category { get; set; }

    public Status CurrentStatus { get; protected set; }

    public IEnumerable<Snapshot> Snapshots => this.snapshots;

    public IDictionary<string, string> Configuration { get; }

    public int MaxSnapshots { get; set; }

    public abstract void QueryStatus(string[] args);

    public void AddSnapshot(Status status)
    {
      while (this.snapshots.Count >= this.MaxSnapshots)
      {
        this.snapshots.Dequeue();
      }

      this.snapshots.Enqueue(new Snapshot(status));
    }
  }
}