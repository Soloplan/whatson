// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectsDataInterchange.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Forms;
  using NLog;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.Localization;

  /// <summary>
  /// The projects import/export.
  /// </summary>
  public class ProjectsDataInterchange
  {
    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Exports the specified connector configuration.
    /// </summary>
    /// <param name="connectorConfiguration">The connector configuration.</param>
    public async Task ExportAsync(ConnectorConfiguration connectorConfiguration, string exportName)
    {
      var itemsList = new List<ConnectorConfiguration>();
      itemsList.Add(connectorConfiguration);
      await this.ExportAsync(itemsList, exportName);
    }

    /// <summary>
    /// Exports the specified connectors configuration.
    /// </summary>
    /// <param name="connectorsConfiguration">The connectors configuration.</param>
    public async Task ExportAsync(IList<ConnectorConfiguration> connectorsConfiguration, string exportName)
    {
      var filePath = this.GetExportFilePath(exportName);
      if (string.IsNullOrWhiteSpace(filePath))
      {
        return;
      }

      await SerializationHelper.Instance.SaveAsync(connectorsConfiguration, filePath, false);
    }

    /// <summary>
    /// Imports the projects configuration.
    /// </summary>
    /// <param name="appConfiguration">The application configuration.</param>
    /// <returns>True if import was successfull.</returns>
    public async Task<bool> ImportAsync(ApplicationConfiguration appConfiguration)
    {
      var filePath = this.GetImportFilePath();
      if (filePath == null)
      {
        return false;
      }

      try
      {
        await this.ImportAsync(appConfiguration, filePath);
      }
      catch (Exception e)
      {
        var errorMessage = Localizer.ProjectImportFailed(filePath, e.Message);
        log.Error(errorMessage);
        log.Error(e);
        System.Windows.MessageBox.Show(errorMessage, Strings.ImportError, MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }

      return true;
    }

    internal async Task ImportAsync(ApplicationConfiguration appConfiguration, string filePath)
    {
      var importedProjectsConfig = await SerializationHelper.LoadAsync<IList<ConnectorConfiguration>>(filePath);
      if (importedProjectsConfig == null || importedProjectsConfig.Any(project => project == null))
      {
        throw new InvalidDataException();
      }

      var originalCount = appConfiguration.ConnectorsConfiguration.Count;
      try
      {
        foreach (var project in importedProjectsConfig)
        {
          appConfiguration.ConnectorsConfiguration.Add(project);
        }

        await SerializationHelper.Instance.SaveConfigurationAsync(appConfiguration);
      }
      catch
      {
        while (appConfiguration.ConnectorsConfiguration.Count > originalCount)
        {
          appConfiguration.ConnectorsConfiguration.RemoveAt(appConfiguration.ConnectorsConfiguration.Count - 1);
        }

        throw;
      }
    }

    private string GetProjectsInterchangeFileFilter() => $"{Properties.Resources.JsonFilesFilterName}|*.{SerializationHelper.ConfigFileExtension}";

    /// <summary>
    /// Gets the export file path.
    /// </summary>
    /// <returns>The export file path.</returns>
    private string? GetExportFilePath(string fileName)
    {
      using (var saveFileDialog = new SaveFileDialog())
      {
        saveFileDialog.Filter = this.GetProjectsInterchangeFileFilter();

        saveFileDialog.FileName = $"whatson_{string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()))}.json";
        if (saveFileDialog.ShowDialog() == DialogResult.OK)
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
    private string? GetImportFilePath()
    {
      using (var openFileDialog = new OpenFileDialog())
      {
        openFileDialog.Filter = this.GetProjectsInterchangeFileFilter();
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
          return openFileDialog.FileName;
        }
      }

      return null;
    }
  }
}
