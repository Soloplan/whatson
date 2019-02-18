namespace Soloplan.WhatsON
{
  using System.Collections.Generic;

  public class Configuration
  {
    public Configuration()
    {
      this.Subjects = new List<Subject>();
    }

    public IList<Subject> Subjects { get; }
  }
}
