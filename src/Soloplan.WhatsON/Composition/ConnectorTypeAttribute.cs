namespace Soloplan.WhatsON.Composition
{
  using System;

  [AttributeUsage(AttributeTargets.Class)]
  public class ConnectorTypeAttribute : Attribute
  {
    public ConnectorTypeAttribute(string name)
    {
      this.Name = name;
    }

    public string Name { get; }

    public string Description { get; set; }
  }
}
