// <copyright file="ExceptionHandlingInitialization.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI
{
  using System;
  using System.Windows;
  using log4net;

  public static class ExceptionHandlingInitialization
  {
    private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static void Initialize()
    {
      AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
      {
        var exception = args.ExceptionObject as Exception;
        if (exception != null)
        {
          log.Fatal("An unhandled exception occurred in a non-main thread.", exception);
        }
        else
        {
          log.FatalFormat("An unhandled exception occurred in a non-main thread. No exception object available. Only this thing: {0}", args.ExceptionObject);
        }
      };

      Application.Current.DispatcherUnhandledException += (sender, e) =>
      {
        log.Fatal("An unhandled asynchronous exception occurred in the main thread:", e.Exception);
      };

      AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
      {
        log.Error("A first-chance exception occurred", args.Exception);
      };
    }
  }
}