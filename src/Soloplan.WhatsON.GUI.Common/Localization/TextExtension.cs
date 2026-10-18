// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextExtension.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.Localization
{
  using System;
  using System.Globalization;
  using System.Reflection;
  using System.Windows;
  using System.Windows.Data;
  using System.Windows.Markup;

  /// <summary>
  /// Reads a strongly typed resource property again when the language changes.
  /// </summary>
  public class TextExtension : StaticExtension, IValueConverter
  {
    private Func<string>? readValue;

    public TextExtension(string member)
      : base(member)
    {
    }

    private TextExtension(Func<string> readValue)
    {
      this.readValue = readValue;
    }

    public static Binding CreateBinding(Func<string> readValue) => new Binding(nameof(LanguageSource.Culture))
    {
      Source = LanguageSource.Instance,
      Mode = BindingMode.OneWay,
      Converter = new TextExtension(readValue),
    };

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      var separator = this.Member.LastIndexOf('.');
      var resolver = serviceProvider.GetService(typeof(IXamlTypeResolver)) as IXamlTypeResolver;
      var type = this.MemberType ?? (resolver ?? throw new InvalidOperationException("No XAML type resolver is available.")).Resolve(this.Member.Substring(0, separator));
      var property = type.GetProperty(this.Member.Substring(separator + 1), BindingFlags.Public | BindingFlags.Static)
        ?? throw new InvalidOperationException($"Unknown localized resource: {this.Member}");
      return CreateBinding(() => property.GetValue(null) as string ?? throw new InvalidOperationException($"Localized resource {this.Member} is not a string.")).ProvideValue(serviceProvider);
    }

    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) => (this.readValue ?? throw new InvalidOperationException("The localization converter has not been initialized."))();

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => DependencyProperty.UnsetValue;
  }
}
