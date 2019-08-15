namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System;
  using System.Globalization;
  using System.Windows.Data;
  using System.Windows.Media;

  public class StatusToColorConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is ObservationState state)
      {
        switch (state)
        {
          case ObservationState.Unknown:
            return new SolidColorBrush(System.Windows.Media.Colors.Gray);
          case ObservationState.Unstable:
            return new SolidColorBrush(System.Windows.Media.Colors.Orange);
          case ObservationState.Failure:
            return new SolidColorBrush(Color.FromRgb(201, 42, 60));
          case ObservationState.Success:
            return new SolidColorBrush(Color.FromRgb(66, 171, 20));
          case ObservationState.Running:
            return new SolidColorBrush(System.Windows.Media.Colors.DarkCyan);
        }
      }

      return new SolidColorBrush(System.Windows.Media.Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return null;
    }
  }
}