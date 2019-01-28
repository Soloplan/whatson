namespace Soloplan.WhatsON
{
  using System;
  using System.Collections.Generic;

  public class Status
  {
    public Status()
      : this(ObservationState.Unknown)
    {
    }

    public Status(ObservationState state)
    {
      this.Properties = new Dictionary<string, string>();
      this.State = state;
    }

    public string Name { get; set; }

    public string Detail { get; set; }

    public DateTime Time { get; set; }

    public ObservationState State { get; set; }

    public IDictionary<string, string> Properties { get; }

    public override string ToString()
    {
      return $"[{this.Time}]: {this.Name} - {this.State}";
    }
  }
}