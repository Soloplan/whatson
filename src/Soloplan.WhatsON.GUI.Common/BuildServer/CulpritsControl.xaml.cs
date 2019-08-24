// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CulpritsControl.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.BuildServer
{
  using System.Collections;
  using System.Windows;
  using System.Windows.Controls;

  /// <summary>
  /// Interaction logic for CulpritsControl.xaml.
  /// </summary>
  public partial class CulpritsControl : UserControl
  {
    /// <summary>
    /// Dependency property for <see cref="CulpritsProp"/>.
    /// </summary>
    public static readonly DependencyProperty CulpritsPropProperty = DependencyProperty.Register(nameof(CulpritsProp), typeof(IList), typeof(CulpritsControl));

    /// <summary>
    /// Dependency property for <see cref="CulpritsProp"/>.
    /// </summary>
    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(nameof(Caption), typeof(string), typeof(CulpritsControl));

    public CulpritsControl()
    {
      this.InitializeComponent();
      var converter = this.Resources["CountToVisibility"] as CountToVisibilityConvrter;
      converter.ValueForFalse = Visibility.Collapsed;
      this.DataContext = this;
    }

    public IList CulpritsProp
    {
      get => (IList)this.GetValue(CulpritsPropProperty);
      set => this.SetValue(CulpritsPropProperty, value);
    }

    public string Caption
    {
      get => (string)this.GetValue(CaptionProperty);
      set => this.SetValue(CaptionProperty, value);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
      if (e.Property.Name == nameof(this.CulpritsProp))
      {
      }

      base.OnPropertyChanged(e);
    }
  }
}
