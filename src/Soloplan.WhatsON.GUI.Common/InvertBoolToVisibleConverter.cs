// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InvertBoolToVisibleConverter.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common
{
  using System;
  using System.Globalization;
  using System.Windows;
  using System.Windows.Data;

  /// <summary>
  /// Negates the value before converting it to visibility.
  /// </summary>
  public class InvertBoolToVisibleConverter : IValueConverter
  {
    /// <summary>Converts a Boolean value to a <see cref="T:System.Windows.Visibility" /> enumeration value.</summary>
    /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
    /// <param name="targetType">This parameter is not used.</param>
    /// <param name="parameter">This parameter is not used.</param>
    /// <param name="culture">This parameter is not used.</param>
    /// <returns>
    /// <see cref="F:System.Windows.Visibility.Visible" /> if <paramref name="value" /> is <see langword="true" />; otherwise, <see cref="F:System.Windows.Visibility.Collapsed" />.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var flag = false;
      if (value is bool val)
      {
        flag = val;
      }

      return (Visibility)(flag ? 2 : 0);
    }

    /// <summary>Converts a <see cref="T:System.Windows.Visibility" /> enumeration value to a Boolean value.</summary>
    /// <param name="value">A <see cref="T:System.Windows.Visibility" /> enumeration value. </param>
    /// <param name="targetType">This parameter is not used.</param>
    /// <param name="parameter">This parameter is not used.</param>
    /// <param name="culture">This parameter is not used.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="value" /> is <see cref="F:System.Windows.Visibility.Visible" />; otherwise, <see langword="false" />.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is Visibility visibility)
      {
        return visibility != Visibility.Visible;
      }

      return false;
    }
  }
}