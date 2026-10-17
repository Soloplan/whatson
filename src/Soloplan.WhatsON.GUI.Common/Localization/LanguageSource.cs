// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LanguageSource.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.Localization
{
  using System.ComponentModel;
  using System.Globalization;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using System.Windows.Markup;

  public sealed partial class LanguageSource : INotifyPropertyChanged
  {
    private LanguageSource()
    {
      Localizer.LanguageChanged += (sender, args) =>
      {
        var culture = CultureInfo.CurrentUICulture;
        if (Application.Current is Application app)
        {
          app.Dispatcher.Invoke(() => this.Culture = culture);
        }
        else
        {
          this.Culture = culture;
        }
      };

      // Popups have their own inheritance tree, so bind their formatting language too.
      foreach (var type in new[] { typeof(Window), typeof(ToolTip), typeof(ContextMenu) })
      {
        EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent, new RoutedEventHandler(this.ElementLoaded));
      }
    }

    public static LanguageSource Instance { get; } = new LanguageSource();

    public CultureInfo Culture { get; private set; } = CultureInfo.CurrentUICulture;

    public XmlLanguage Language => XmlLanguage.GetLanguage(this.Culture.IetfLanguageTag);

    private void ElementLoaded(object? sender, RoutedEventArgs args)
    {
      if (ReferenceEquals(sender, args.OriginalSource))
      {
        ((FrameworkElement)sender).SetBinding(
          FrameworkElement.LanguageProperty,
          new Binding(nameof(this.Language)) { Source = this, Mode = BindingMode.OneWay });
      }
    }
  }
}
