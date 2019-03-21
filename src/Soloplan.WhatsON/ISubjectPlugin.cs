namespace Soloplan.WhatsON
{
  using System;

  public interface ISubjectPlugin : IPlugIn
  {
    Type SubjectType { get; }

    SubjectTypeAttribute SubjectTypeAttribute { get; }

    Subject CreateNew(SubjectConfiguration configuration);
  }
}