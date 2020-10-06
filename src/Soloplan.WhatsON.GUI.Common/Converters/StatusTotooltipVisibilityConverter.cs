// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InvertBooleanConverter.cs" company="Soloplan GmbH">
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
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// This converter is only for debug purposes to check what is going on in xaml.
  /// </summary>
  public class StatusTotooltipVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return ((ObservationState)value) != ObservationState.Unknown ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return value;
    }

  }
}