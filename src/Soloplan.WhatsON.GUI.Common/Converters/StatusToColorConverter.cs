// <copyright file="StatusToColorConverter.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.Converters
{
  using System;
  using System.Globalization;
  using System.Windows.Data;
  using System.Windows.Media;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.Model;

  public class StatusToColorConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is StatusViewModel status)
      {
        Color color = GetColor(status.State);
        return new SolidColorBrush(color);
      }

      if (value is ObservationState state)
      {
        return new SolidColorBrush(GetColor(state));
      }

      return new SolidColorBrush(System.Windows.Media.Colors.White);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return null;
    }

    private static Color GetColor(ObservationState state)
    {
      switch (state)
      {
        case ObservationState.Unstable:
          return System.Windows.Media.Colors.Orange;
          break;
        case ObservationState.Failure:
          return Color.FromRgb(201, 42, 60);
          break;
        case ObservationState.Success:
          return Color.FromRgb(66, 171, 20);
          break;
        case ObservationState.Running:
          return System.Windows.Media.Colors.DarkCyan;
          break;
        default:
          return Color.FromRgb(120, 144, 156);
          break;
      }
    }
  }
}