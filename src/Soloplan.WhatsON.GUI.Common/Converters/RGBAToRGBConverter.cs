// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullOrDefaultToVisibilityConverter.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    /// <summary>
    /// Converts given value from rgba to rgb.
    /// </summary>
    public class RGBToRGBAConverter : IValueConverter
    {
        /// <summary>Converts SolidColorBrush from RGBA to RGB, if RGB given it returns RGB. F ex, when given "#FFDDFFDD" returns "#DDFFDD".</summary>
        /// <param name="value">SolidColorBrush is the given brush to convert from rgba to rgb.</param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        /// <returns>
        ///  String with rgb />.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush)
            {
                string input = ((SolidColorBrush)value).ToString();
                if (input.Length == 7)
                {
                    return input;
                }

                string output=string.Empty;
                for (int i = 0; i < input.Length; i++)
                {
                    if (i == 1) { i = 3; }
                    output += input[i];
                }

                return output;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}