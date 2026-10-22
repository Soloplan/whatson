// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BuildToolTip.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using System.Windows.Input;
  using System.Windows.Threading;

  internal static class BuildToolTip
  {
    private static readonly DependencyProperty closeSessionProperty = DependencyProperty.RegisterAttached(
      "closeSession", typeof(Action), typeof(BuildToolTip));

    public static void Attach(FrameworkElement owner)
    {
      owner.ToolTipOpening += (_, args) =>
      {
        if (!ReferenceEquals(args.OriginalSource, owner) || owner.ToolTip == null)
        {
          return;
        }

        // The tooltip service closes on every click; own the lifetime so names remain interactive.
        args.Handled = true;
        if (owner.ToolTip is not ToolTip tooltip)
        {
          tooltip = new ToolTip { Content = owner.ToolTip };
          owner.SetCurrentValue(FrameworkElement.ToolTipProperty, tooltip);
        }

        (tooltip.GetValue(closeSessionProperty) as Action)?.Invoke();
        if (tooltip.IsOpen)
        {
          tooltip.IsOpen = false;
        }

        tooltip.PlacementTarget = owner;
        tooltip.SetBinding(FrameworkElement.DataContextProperty, new Binding(nameof(owner.DataContext)) { Source = owner });
        if (tooltip.Visibility != Visibility.Visible)
        {
          return;
        }

        tooltip.StaysOpen = false;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        var window = Window.GetWindow(owner);
        void Close(object? sender, EventArgs e) => tooltip.IsOpen = false;
        void KeyDown(object sender, KeyEventArgs e)
        {
          if (e.Key == Key.Escape)
          {
            tooltip.IsOpen = false;
            e.Handled = true;
          }
        }

        void Cleanup()
        {
          timer.Stop();
          owner.Unloaded -= Close;
          tooltip.PreviewKeyDown -= KeyDown;
          tooltip.Closed -= Closed;
          if (window != null)
          {
            window.Deactivated -= Close;
          }

          if (Equals(tooltip.GetValue(closeSessionProperty), (Action)Cleanup))
          {
            tooltip.ClearValue(closeSessionProperty);
          }
        }

        void Closed(object sender, RoutedEventArgs e)
        {
          if (!tooltip.IsOpen)
          {
            Cleanup();
          }
        }

        timer.Tick += (_, _) =>
        {
          if (!tooltip.IsKeyboardFocusWithin && !owner.IsKeyboardFocusWithin
              && !new Rect(owner.RenderSize).Contains(Mouse.GetPosition(owner))
              && !new Rect(tooltip.RenderSize).Contains(Mouse.GetPosition(tooltip)))
          {
            tooltip.IsOpen = false;
          }
        };
        owner.Unloaded += Close;
        tooltip.PreviewKeyDown += KeyDown;
        tooltip.Closed += Closed;
        if (window != null)
        {
          window.Deactivated += Close;
        }

        tooltip.SetValue(closeSessionProperty, (Action)Cleanup);
        tooltip.IsOpen = true;
        timer.Start();
      };
    }
  }
}
