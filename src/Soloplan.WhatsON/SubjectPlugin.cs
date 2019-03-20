namespace Soloplan.WhatsON
{
  using System;
  using System.Reflection;

  public abstract class SubjectPlugin : ISubjectPlugin
  {
    protected SubjectPlugin(Type subjectType)
    {
      this.SubjectType = subjectType;
      this.SubjectTypeAttribute = this.SubjectType.GetCustomAttribute<SubjectTypeAttribute>();
    }

    public Type SubjectType { get; }

    public SubjectTypeAttribute SubjectTypeAttribute { get; }

    public abstract Subject CreateNew(SubjectConfiguration configuration);
  }
}
