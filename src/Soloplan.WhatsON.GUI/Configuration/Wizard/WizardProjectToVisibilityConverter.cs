// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WizardProjectToVisibilityConverter.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System;
  using System.Globalization;
  using System.Windows;
  using System.Windows.Data;

  /// <summary>
  /// Converter for wizard project tooltips. Uses method from ProjectViewModel to determine visibility.
  /// </summary>
  public class WizardProjectToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is Soloplan.WhatsON.GUI.Configuration.Wizard.ProjectViewModel project)
      {
        return project.ShouldDisplayTooltip() ? Visibility.Visible : Visibility.Hidden;
      }

      return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}