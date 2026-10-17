// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExceptionHandlingInitialization.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Logging
{
  using System;
  using System.Windows;
  using NLog;

  /// <summary>
  /// Logs unhandled application exceptions.
  /// </summary>
  public static class ExceptionHandlingInitialization
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Initializes logging exception.
    /// </summary>
    public static void Initialize()
    {
      AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
      {
        var exception = args.ExceptionObject as Exception;
        if (exception != null)
        {
          log.Fatal(exception, "An unhandled exception occurred in a non-main thread.");
        }
        else
        {
          log.Fatal(
            "An unhandled exception occurred in a non-main thread. No exception object available. Only this thing: {@exceptionObj}",
            args.ExceptionObject);
        }
      };

      Application.Current.DispatcherUnhandledException += (sender, e) =>
      {
        log.Fatal(e.Exception, "An unhandled asynchronous exception occurred in the main thread:");
      };
    }
  }
}
