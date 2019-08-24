// <copyright file="MessageControl.xaml.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System.Windows.Controls;

  /// <summary>
  /// Interaction logic for MessageControl.xaml.
  /// </summary>
  public partial class MessageControl : UserControl
  {
    public MessageControl(string message)
    {
      this.InitializeComponent();
      this.uxMessgeTextBox.Text = message;
    }
  }
}
