namespace Soloplan.WhatsON
{
  using System;

  public interface ISubjectPlugin
  {
    Type SubjectType { get; }

    SubjectTypeAttribute SubjectTypeAttribute { get; }

    Subject CreateNew(SubjectConfiguration configuration);
  }
}