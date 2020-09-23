// <copyright file="TimeToApproximateTimeConverter.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.Converters
{
  using System;
  using System.Globalization;
  using System.Windows.Data;
  using Humanizer;
  public class TimeToApproximateTimeConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null)
      {
        return ((DateTime?)null).Humanize(false,null,CultureInfo.InvariantCulture);
      }

      if (value is DateTime date)
      {
        return date.Humanize(false);
      }

      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException("Can't convert from approximate time to exact time.");
    }
  }
}