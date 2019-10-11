// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectsDataInterchange.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System.Collections.Generic;
  using System.Windows.Forms;
  using Soloplan.WhatsON.Configuration;

  /// <summary>
  /// The projects import/export.
  /// </summary>
  public class ProjectsDataInterchange
  {
    /// <summary>
    /// Exports the specified connector configuration.
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    public void Export(ConnectorConfiguration connectorConfiguration)
    {
      var itemsList = new List<ConnectorConfiguration>();
      itemsList.Add(connectorConfiguration);
      this.Export(itemsList);
    }

    /// <summary>
    /// Exports the specified connectors configuration.
    /// </summary>
    /// <param name="connectorsConfiguration">The connectors configuration.</param>
    public void Export(IList<ConnectorConfiguration> connectorsConfiguration)
    {
      var filePath = this.GetExportFilePath();
      if (string.IsNullOrWhiteSpace(filePath))
      {
        return;
      }

      SerializationHelper.Save(connectorsConfiguration, filePath, false);
    }

    public bool Import(ApplicationConfiguration appConfiguration)
    {
      var filePath = this.GetImportFilePath();
      if (filePath == null)
      {
        return false;
      }

      var importedProjectsConfig = SerializationHelper.Load<IList<ConnectorConfiguration>>(filePath);
      foreach (var importedProjecConfig in importedProjectsConfig)
      {
        appConfiguration.ConnectorsConfiguration.Add(importedProjecConfig);
      }

      return true;
    }

    private string GetProjectsInterchangeFileFilter()
    {
      return $"{Properties.Resources.JsonFilesFilterName}|*.{SerializationHelper.ConfigFileExtension}";
    }

    /// <summary>
    /// Gets the export file path.
    /// </summary>
    /// <returns>The export file path.</returns>
    private string GetExportFilePath()
    {
      using (var saveFileDialog = new SaveFileDialog())
      {
        saveFileDialog.Filter = this.GetProjectsInterchangeFileFilter();
        if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          return saveFileDialog.FileName;
        }
      }

      return null;
    }

    /// <summary>
    /// Gets the import file path.
    /// </summary>
    /// <returns>The import file path.</returns>
    private string GetImportFilePath()
    {
      using (var openFileDialog = new OpenFileDialog())
      {
        openFileDialog.Filter = this.GetProjectsInterchangeFileFilter();
        if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          return openFileDialog.FileName;
        }
      }

      return null;
    }
  }
}