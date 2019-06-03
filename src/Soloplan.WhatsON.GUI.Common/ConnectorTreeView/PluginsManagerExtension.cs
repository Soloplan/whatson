namespace Soloplan.WhatsON.GUI.Common.SubjectTreeView
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
    /// Gets plugIn for appropriate for presenting <paramref name="connectorType"/>.
    /// </summary>
    /// <param name="manager">Plugin manager.</param>
    /// <param name="connectorType">Type of connector.</param>
    /// <returns>Appropriate <see cref="ITreeViewPresentationPlugIn"/>.</returns>
    public static ITreeViewPresentationPlugIn GetPresentationPlugIn(this PluginsManager manager, Type connectorType)
    {
      var allPlugins = manager.GetPresentationPlugIns().ToList();
      var result = allPlugins.FirstOrDefault(plugIn => plugIn.ConnectorType.ToString() == connectorType.ToString());
      if (result != null)
      {
        return result;
      }

      return allPlugins.FirstOrDefault(plugIn => plugIn.ConnectorType.IsAssignableFrom(connectorType));
    }
  }
}