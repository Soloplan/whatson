// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProgressBarTooltipControl.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System.Windows;
  using System.Windows.Controls;

  /// <summary>
  /// Control for project build progress.
  /// </summary>
  public partial class ProgressBarControl : UserControl
  {
    /// <summary>
    /// Dependency property for <see cref="CulpritsProp"/>.
    /// </summary>
    public static readonly DependencyProperty ControlOrientationProperty = DependencyProperty.Register(nameof(ControlOrientation), typeof(Orientation), typeof(ProgressBarControl), new PropertyMetadata(Orientation.Vertical));

    public static readonly DependencyProperty CompactDisplayProperty = DependencyProperty.Register(nameof(CompactDisplay), typeof(bool), typeof(ProgressBarControl), new PropertyMetadata(false, new PropertyChangedCallback(OnCurrentReadingChanged)));

    public ProgressBarControl()
    {
      this.InitializeComponent();
    }

    private static void OnCurrentReadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ProgressBarTooltipControl control)
      {
        control.UpdateTexts();
      }
    }

    public Orientation ControlOrientation
    {
      get => (Orientation)this.GetValue(ControlOrientationProperty);
      set => this.SetValue(ControlOrientationProperty, value);
    }

    public bool CompactDisplay
    {
      get => (bool)this.GetValue(CompactDisplayProperty);
      set => this.SetValue(CompactDisplayProperty, value);
    }

    public void UpdateTexts()
    {
      if (this.CompactDisplay)
      {
        this.EstimatedRemainingText.Text = " ETA";
        this.PercentSignText.Text = "%/";
      }
      else
      {
        this.EstimatedRemainingText.Text = " estimated remaining";
        this.PercentSignText.Text = "% ";
      }
    }
  }
}
