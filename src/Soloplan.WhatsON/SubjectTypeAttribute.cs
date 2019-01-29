namespace Soloplan.WhatsON
{
  using System;

  [AttributeUsage(AttributeTargets.Class)]
  public class SubjectTypeAttribute : Attribute
  {
    public SubjectTypeAttribute(string name)
    {
      this.Name = name;
    }

    public string Name { get; }

    public string Description { get; set; }
  }
}
