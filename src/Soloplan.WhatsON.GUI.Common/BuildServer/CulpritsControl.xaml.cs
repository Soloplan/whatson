// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CulpritsControl.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System.Windows;
  using System.Windows.Controls;

  /// <summary>
  /// Interaction logic for CulpritsControl.xaml
  /// </summary>
  public partial class CulpritsControl : UserControl
  {
    public CulpritsControl()
    {
      this.InitializeComponent();
      var converter = this.Resources["CountToVisibility"] as CountToVisibilityConvrter;
      converter.ValueForFalse = Visibility.Collapsed;
    }
  }
}
