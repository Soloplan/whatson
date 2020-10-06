// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainStatusToVisibilityConverter.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.Converters
{
  using System;
  using System.Globalization;
  using System.Windows;
  using System.Windows.Data;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  /// <summary>
  /// Converter for projects DataTemplates. Determines visibility of a project tooltips. Uses StatusViewmodel method to find out if tooltip should be visible or not.
  /// </summary>
  public class MainStatusToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is StatusViewModel status)
      {
        return status.Parent.ShouldDisplayTooltip() ? Visibility.Visible : Visibility.Hidden;
      }

      if (value is ConnectorViewModel connector)
      {
        return connector.ShouldDisplayTooltip() ? Visibility.Visible : Visibility.Hidden;
      }

      return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}