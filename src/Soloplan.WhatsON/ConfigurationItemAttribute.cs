namespace Soloplan.WhatsON
{
  using System;

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
  public class ConfigurationItemAttribute : Attribute
  {
    public ConfigurationItemAttribute(string key, Type type)
    {
      this.Key = key;
      this.Type = type;
      this.Optional = true;
    }

    public string Key { get; }

    public Type Type { get; }

    public bool Optional { get; set; }
  }
}
