namespace Soloplan.WhatsON.GUI.SubjectTreeView
{
  using System;
  using System.Globalization;
  using System.Windows.Data;

  public class TimeToAproximateTimeConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is DateTime date)
      {
        var timeDifference = DateTime.Now - date;
        if (timeDifference.Days > 7)
        {
          return $"{timeDifference.Days / 7} weeks ago";
        }

        if (timeDifference.Days >= 1)
        {
          return $"{timeDifference.Days} weeks ago";
        }

        if (timeDifference.Hours > 1)
        {
          return $"{timeDifference.Hours} hours ago";
        }

        if (timeDifference.Minutes > 1)
        {
          return $"{timeDifference.Minutes} minutes ago";
        }

        return "just now";
      }

      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return null;
    }
  }
}