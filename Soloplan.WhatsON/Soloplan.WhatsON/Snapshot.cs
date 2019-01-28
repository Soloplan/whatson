namespace Soloplan.WhatsON
{
  public class Snapshot
  {
    public Snapshot(Status status)
    {
      this.Status = status;
    }

    public string Name { get; set; }

    public Status Status { get; }
  }
}
