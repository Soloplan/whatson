// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullOrDefaultVisibilityConverter.cs" company="Soloplan GmbH">
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

  public class NullOrDefaultVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var isDefault = value == null;
      if (!isDefault && value.GetType().IsValueType)
      {
        var def = Activator.CreateInstance(value.GetType());
        isDefault = value.Equals(def);
      }

      return isDefault ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}