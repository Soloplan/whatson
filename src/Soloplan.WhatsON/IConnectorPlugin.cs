namespace Soloplan.WhatsON
{
  using System;

  public interface ISubjectPlugin : IPlugIn
  {
    Type SubjectType { get; }

    SubjectTypeAttribute SubjectTypeAttribute { get; }

    /// <summary>
    /// Gets a value indicating whether this plugin supports wizards.
    /// </summary>
    bool SupportsWizard { get; }

    Subject CreateNew(SubjectConfiguration configuration);
  }
}