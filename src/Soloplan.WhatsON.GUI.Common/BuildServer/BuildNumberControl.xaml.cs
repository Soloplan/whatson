// <copyright file="BuildNumberControl.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System.Net.Mime;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Input;

  /// <summary>
  /// Interaction logic for BuildNumberControl.xaml.
  /// </summary>
  public partial class BuildNumberControl : UserControl
  {
    /// <summary>
    /// Dependency property for <see cref="MediaTypeNames.Text"/>.
    /// </summary>
    public static readonly DependencyProperty BuildNumberProperty = DependencyProperty.Register(nameof(BuildNumber), typeof(int?), typeof(BuildNumberControl));

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildNumberControl"/> class.
    /// </summary>
    public BuildNumberControl()
    {
      this.InitializeComponent();
      this.ControlToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
      this.ControlToolTip.PlacementTarget = this;
      this.DataContext = this;
      this.PreviewMouseDoubleClick += this.OnPreviewMouseDoubleClick;
    }

    /// <summary>
    /// Gets or sets controls bendable property.
    /// </summary>
    public int? BuildNumber
    {
      get => (int?)this.GetValue(BuildNumberProperty);
      set => this.SetValue(BuildNumberProperty, value);
    }

    /// <summary>
    /// Handles clicking on this control. Prevents the event being passed up.
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">Event args.</param>
    private async void OnPreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      Clipboard.SetText(this.BuildNumber.ToString());
      this.ToolTipText.Text = "Value copied.";
      this.ControlToolTip.IsOpen = true;
      e.Handled = true;
      await Task.Delay(1000);

      this.ControlToolTip.IsOpen = false;
      this.ToolTipText.Text = "Double click to copy.";
    }
  }
}
