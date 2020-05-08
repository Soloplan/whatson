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


    /// <summary>
    /// Converts RGBA hex with hash color to RGB color hex with hash
    /// </summary>
    internal class RGBAToRGBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string input = (string)value;
            string output = "";
            for (int i = 0; i < 7; i++)
            {
                output += input[i];
            }

            return output;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}