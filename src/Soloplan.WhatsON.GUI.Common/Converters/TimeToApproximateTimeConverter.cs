// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TimeToApproximateTimeConverter.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.Converters
{
  using System;
  using System.Globalization;
  using System.Windows.Data;
  using Humanizer;
  using Soloplan.WhatsON.Localization;

  public class TimeToApproximateTimeConverter : IValueConverter, IMultiValueConverter
  {
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
      if (value == null)
      {
        return Strings.Never;
      }

      if (value is DateTime date)
      {
        return date.Humanize(false, culture: CultureInfo.CurrentUICulture);
      }

      return value;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException("Can't convert from approximate time to exact time.");

    object IMultiValueConverter.Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
      var text = this.Convert(values[0], targetType, parameter, culture);
      return parameter is "Finished" ? string.Format(CultureInfo.CurrentUICulture, Strings.TooltipFinishedFormat, text) : text;
    }

    object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
  }
}
