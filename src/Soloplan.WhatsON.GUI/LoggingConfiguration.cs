// <copyright file="LoggingConfiguration.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Windows;
  using log4net;
  using log4net.Config;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Class for configuring logging.
  /// </summary>
  public class LoggingConfiguration
  {
    public void Initialize()
    {
      string error = "Log configuration wasn't run.";
      var file = Path.Combine(SerializationHelper.ConfigFolder, "loggingConfiguration.xml");
      if (File.Exists(file))
      {
        GlobalContext.Properties["LogFilePath"] = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Soloplan GmbH", "WhatsOn");
        XmlConfigurator.ConfigureAndWatch(new FileInfo(file));
        error = string.Empty;

        if (!log4net.LogManager.GetRepository().Configured)
        {
          var builder = new StringBuilder();

          // log4net not configured
          foreach (log4net.Util.LogLog message in log4net.LogManager.GetRepository().ConfigurationMessages.Cast<log4net.Util.LogLog>())
          {
            // evaluate configuration message
            builder.AppendLine(message.Message);
          }

          error = builder.ToString();
        }
      }
      else
      {
        error = $"Logging is disabled. File {file} does not exist.";
      }

      if (!string.IsNullOrEmpty(error))
      {
        MessageBox.Show(error, "Failed to initialize Log4Net", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}