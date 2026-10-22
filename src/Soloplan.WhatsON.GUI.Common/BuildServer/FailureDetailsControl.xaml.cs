// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FailureDetailsControl.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System;
  using System.Threading;
  using System.Windows.Controls;
  using NLog;

  public partial class FailureDetailsControl : UserControl
  {
    private CancellationTokenSource? loading;

    public FailureDetailsControl()
    {
      this.InitializeComponent();
      this.Loaded += (_, _) => this.LoadDetails();
      this.Unloaded += (_, _) =>
      {
        this.loading?.Cancel();
        this.FailuresExpander.IsExpanded = false;
      };
      this.DataContextChanged += (_, _) =>
      {
        this.FailuresExpander.IsExpanded = false;
        this.LoadDetails();
      };
      this.FailuresExpander.Expanded += (_, _) => this.LoadDetails();
      this.FailuresExpander.Collapsed += (_, _) => this.loading?.Cancel();
    }

    private async void LoadDetails()
    {
      this.loading?.Cancel();
      if (!this.IsLoaded || this.DataContext is not BuildStatusViewModel status)
      {
        return;
      }

      using var request = new CancellationTokenSource();
      this.loading = request;
      try
      {
        if (this.FailuresExpander.IsExpanded)
        {
          await status.LoadExpandedFailureDetails(request.Token);
        }
        else
        {
          await status.LoadFailureDetails(request.Token);
        }
      }
      catch (OperationCanceledException) when (request.IsCancellationRequested)
      {
      }
      catch (Exception exception)
      {
        LogManager.GetCurrentClassLogger().Warn(exception, "Could not load optional build failure details.");
      }
      finally
      {
        if (ReferenceEquals(this.loading, request))
        {
          this.loading = null;
        }
      }
    }
  }
}
