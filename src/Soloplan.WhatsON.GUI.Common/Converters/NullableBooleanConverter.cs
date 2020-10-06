// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullableBooleanConverter.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.Converters
{
  using System;
  using System.Globalization;
  using System.Windows.Data;

  [ValueConversion(typeof(bool), typeof(bool))]
  public class NullableBooleanConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value != null)
      {
        return value;
      }

      return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value != null)
      {
        return value;
      }

      return null;
    }
  }
}
