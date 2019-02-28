namespace Soloplan.WhatsON
{
  using System;
  using System.Collections.Generic;

  public interface ISubjectPlugin
  {
    Type SubjectType { get; }

    SubjectTypeAttribute SubjectTypeAttribute { get; }

    Subject CreateNew(string name, IList<ConfigurationItem> configuration);
  }
}