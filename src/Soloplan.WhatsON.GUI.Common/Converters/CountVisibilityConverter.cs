// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CountToVisibilityConverter.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Soloplan.WhatsON.GUI.Common.Converters
{
  using System;
  using System.Collections;
  using System.Globalization;
  using System.Windows;
  using System.Windows.Data;

  /// <summary>
  /// Converts count to visibility. Count ==  0 -> Hidden, Count > 0 -> Visible.
  /// </summary>
  internal class CountVisibilityConverter : IValueConverter
  {
    public Visibility ValueForFalse { get; set; } = Visibility.Hidden;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is IList list)
      {
        return list.Count > 0 ? Visibility.Visible : this.ValueForFalse;
      }

      return this.ValueForFalse;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return null;
    }
  }
}