// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OkCancelDialog.xaml.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.View
{
  using System.Windows.Controls;

  /// <summary>
  /// Interaction logic for UserControl1.xaml.
  /// </summary>
  public partial class OkCancelDialog : UserControl
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="OkCancelDialog"/> class.
    /// </summary>
    /// <param name="question">The question.</param>
    public OkCancelDialog(string question)
    {
      this.InitializeComponent();
      this.uxQuestionTextBox.Text = question;
    }
  }
}