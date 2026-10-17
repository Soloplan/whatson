// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectRowControl.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System.Windows;
  using System.Windows.Controls;

  public partial class ProjectRowControl : UserControl
  {
    public ProjectRowControl()
    {
      this.InitializeComponent();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
      base.OnRenderSizeChanged(sizeInfo);
      this.BuildHistory.Visibility = this.ActualWidth >= 520 ? Visibility.Visible : Visibility.Collapsed;
      this.BuildTiming.Visibility = this.ActualWidth >= 340 ? Visibility.Visible : Visibility.Collapsed;
    }
  }
}
