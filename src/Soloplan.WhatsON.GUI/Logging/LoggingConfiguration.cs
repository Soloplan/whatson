// <copyright file="LoggingConfiguration.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Logging
{
  using System;
  using System.IO;
  using System.Text;
  using NLog;
  using NLog.Config;
  using Soloplan.WhatsON.GUI.Properties;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Class for configuring logging.
  /// </summary>
  public class LoggingConfiguration
  {
    /// <summary>
    /// The default file extension of a log4net config file.
    /// </summary>
    private const string NLogExtension = ".nlog.xml";

    /// <summary>
    /// Gets a value indicating whether log is initialized.
    /// If is false after call to <see cref="Initialize"/> this means that logging initialization failed.
    /// </summary>
    public bool LogInitialized { get; private set; }

    /// <summary>
    /// Gets a list of supported log4net configuration file names. The first one found will be used.
    /// </summary>
    private static string ConfigFileName
    {
      get
      {
        var executableFileName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        return executableFileName + NLogExtension;
      }
    }

    public void Initialize()
    {
      var rootDir = SerializationHelper.ConfigFolder;
      var file = GetConfigFilePath(rootDir);
      if (string.IsNullOrEmpty(file))
      {
        return;
      }

      LogManager.Configuration = new XmlLoggingConfiguration(file);

      LogManager.Configuration.Variables["rootDir"] = rootDir;
      LogManager.Configuration.Variables["logFileDir"] = GetOrCreateLogFiledirectory(rootDir) ?? rootDir;
      this.LogInitialized = true;
    }

    private static string GetOrCreateLogFiledirectory(string rootDir)
    {
      var logsPath = Path.Combine(rootDir, "Log");
      try
      {
        if (!Directory.Exists(logsPath))
        {
          Directory.CreateDirectory(logsPath);
        }

        return logsPath;
      }
      catch (Exception)
      {
        return null;
      }
    }

    private static string GetConfigFilePath(string configDir)
    {
      try
      {
        if (Directory.Exists(configDir))
        {
          var checkedFilePath = Path.Combine(configDir, ConfigFileName);
          if (File.Exists(checkedFilePath))
          {
            return checkedFilePath;
          }
        }
        else
        {
          Directory.CreateDirectory(configDir);
        }

        var filePath = Path.Combine(configDir, ConfigFileName);
        File.WriteAllLines(filePath, new[] { Resources.LogConfig }, Encoding.UTF8);
        return filePath;
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}