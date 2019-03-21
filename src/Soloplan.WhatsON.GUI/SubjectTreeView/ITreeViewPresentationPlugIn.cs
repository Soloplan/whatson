namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;
  using System.Xml;

  /// <summary>
  /// PlugIn which provides presentation of <see cref="SubjectType"/> subjects.
  /// </summary>
  public interface ITreeViewPresentationPlugIn : IPlugIn
  {
    /// <summary>
    /// Gets type of subject for which this PlugIn provides presentation.
    /// </summary>
    Type SubjectType { get; }

    /// <summary>
    /// Creates <see cref="SubjectViewModel"/> decedent.
    /// </summary>
    /// <returns><see cref="SubjectViewModel"/> decedent.</returns>
    SubjectViewModel CreateViewModel();

    /// <summary>
    /// Gets the XAML file defining DataTemplet for displaying view model created by <see cref="CreateViewModel"/>.
    /// </summary>
    /// <returns>Data template.</returns>
    XmlReader GetDataTempletXaml();
  }
}