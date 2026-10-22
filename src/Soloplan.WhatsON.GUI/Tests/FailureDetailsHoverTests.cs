// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FailureDetailsHoverTests.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System.Threading;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Threading;
  using NUnit.Framework;
  using Soloplan.WhatsON.GUI.Common.BuildServer;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  [TestFixture]
  [Apartment(ApartmentState.STA)]
  public class FailureDetailsHoverTests
  {
    [Test]
    public void OpeningLoadsOnlyTheHoveredRunAndClosingCancelsIt()
    {
      var first = new HoverStatus();
      var second = new HoverStatus();
      var content = new FailureDetailsControl { DataContext = first };
      var target = new Button();
      var window = new Window { Content = target, Left = -20000, Top = -20000, Width = 100, Height = 100, ShowActivated = false };
      var tooltip = new ToolTip { PlacementTarget = target, Content = content };
      Assert.That(first.Requests, Is.Zero);
      try
      {
        window.Show();
        tooltip.IsOpen = true;
        Drain();
        Assert.That(first.Requests, Is.EqualTo(1));
        Assert.That(first.Token.IsCancellationRequested, Is.False);
        Assert.That(first.Expanded, Is.False);
        var previewToken = first.Token;
        var expander = (Expander)content.FindName("FailuresExpander");
        expander.IsExpanded = true;
        Drain();
        Assert.That(first.Requests, Is.EqualTo(2));
        Assert.That(first.Expanded, Is.True);
        Assert.That(previewToken.IsCancellationRequested, Is.True);
        content.DataContext = second;
        Drain();
        Assert.That(first.Token.IsCancellationRequested, Is.True);
        Assert.That(second.Requests, Is.EqualTo(1));
        Assert.That(second.Expanded, Is.False);
        Assert.That(expander.IsExpanded, Is.False);
        tooltip.IsOpen = false;
        Drain();
        Assert.That(second.Token.IsCancellationRequested, Is.True);
      }
      finally
      {
        tooltip.IsOpen = false;
        window.Close();
        Drain();
      }
    }

    private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    private sealed class HoverStatus : BuildStatusViewModel
    {
      public HoverStatus()
        : base(new ConnectorViewModel())
      {
      }

      public int Requests { get; private set; }

      public CancellationToken Token { get; private set; }

      public bool Expanded { get; private set; }

      public override Task LoadFailureDetailsAsync(CancellationToken token) => this.LoadDetailsAsync(token, false);

      public override Task LoadExpandedFailureDetailsAsync(CancellationToken token) => this.LoadDetailsAsync(token, true);

      private async Task LoadDetailsAsync(CancellationToken token, bool expanded)
      {
        this.Requests++;
        this.Expanded = expanded;
        this.Token = token;
        await Task.Delay(Timeout.Infinite, token);
      }
    }
  }
}
