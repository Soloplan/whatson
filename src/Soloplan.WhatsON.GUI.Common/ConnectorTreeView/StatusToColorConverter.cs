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
      if (value is StatusViewModel status)
      {
        Color color = GetColor(status.State);
        if (status.Age != 0)
        {
          var desaturation = (status.Age / (double)Connector.MaxSnapshots) * 0.7;
          color = Desaturate(color, desaturation);
        }

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
          return System.Windows.Media.Colors.Gray;
          break;
      }
    }

    private static Color Desaturate(Color color, double percent)
    {
      // https://stackoverflow.com/questions/13328029/how-to-desaturate-a-color
      var l = (0.3 * color.R) + (0.6 * color.G) + (0.1 * color.B);
      var newRed = color.R + (percent * (l - color.R));
      var newGreen = color.G + (percent * (l - color.G));
      var newBlue = color.B + (percent * (l - color.B));

      return Color.FromRgb((byte)newRed, (byte)newGreen, (byte)newBlue);
    }
  }
}