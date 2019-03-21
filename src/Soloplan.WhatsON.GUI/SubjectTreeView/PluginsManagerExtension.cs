namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  /// <summary>
  /// Extension class for accessing plugIns of <see cref="ITreeViewPresentationPlugIn"/> type.
  /// </summary>
  public static class PluginsManagerExtension
  {
    /// <summary>
    /// Gets all found plugIns.
    /// </summary>
    /// <param name="manager">Plugin manager.</param>
    /// <returns>List of all <see cref="ITreeViewPresentationPlugIn"/>.</returns>
    public static IEnumerable<ITreeViewPresentationPlugIn> GetPresentationPlugIns(this PluginsManager manager)
    {
      return manager.PlugIns.OfType<ITreeViewPresentationPlugIn>();
    }

    /// <summary>
    /// Gets plugIn for appropriate for presenting <paramref name="subjectType"/>.
    /// </summary>
    /// <param name="manager">Plugin manager.</param>
    /// <param name="subjectType">Type of subject.</param>
    /// <returns>Appropriate <see cref="ITreeViewPresentationPlugIn"/>.</returns>
    public static ITreeViewPresentationPlugIn GetPresentationPlugIn(this PluginsManager manager, Type subjectType)
    {
      var allPlugins = manager.GetPresentationPlugIns().ToList();
      var result = allPlugins.FirstOrDefault(plugIn => plugIn.SubjectType.ToString() == subjectType.ToString());
      if (result != null)
      {
        return result;
      }

      return allPlugins.FirstOrDefault(plugIn => plugIn.SubjectType.IsAssignableFrom(subjectType));
    }
  }
}