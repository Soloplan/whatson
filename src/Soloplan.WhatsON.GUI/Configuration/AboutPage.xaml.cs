// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AboutPage.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Forms;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.Localization;

  /// <summary>
  /// Interaction logic for AboutPage.xaml.
  /// </summary>
  public partial class AboutPage : Page
  {
    /// <summary>
    /// The configuration view model.
    /// </summary>
    private readonly ConfigViewModel configurationViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutPage" /> class.
    /// </summary>
    public AboutPage(ConfigViewModel configurationViewModel)
    {
      this.InitializeComponent();
      this.configurationViewModel = configurationViewModel;
      try
      {
        var assembly = Assembly.GetExecutingAssembly();
        this.VersionLabel.Text = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
          ?? assembly.GetName().Version?.ToString() ?? string.Empty;
      }
      catch (Exception)
      {
        this.VersionLabel.Text = Properties.Resources.NotInstalled;
      }

      var materialDesignAssembly = typeof(MaterialDesignThemes.Wpf.DialogHost).Assembly;
      this.MDIXVersionLabel.Text = materialDesignAssembly.GetName().Version?.ToString() ?? string.Empty;
      this.ConfigFile.Text = SerializationHelper.Instance.ConfigFile;
      this.PluginList.ItemsSource = this.Plugins;
    }

    public IList<PluginViewModel> Plugins =>
      PluginManager.Instance.Plugins.Select(x =>
      {
        var newPlugin = new PluginViewModel { Name = x.GetType().Name, Version = x.GetType().Assembly.GetName().Version };
        if (x is ConnectorPlugin connector)
        {
          newPlugin.Description = $"{connector.Name}: {connector.Description}";
        }

        return newPlugin;
      }).ToList();

    private void OpenLink(object? sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
      System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
      e.Handled = true;
    }

    /// <summary>
    /// Handles import button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
    private async void ImportButtonClickAsync(object? sender, RoutedEventArgs e)
    {
      var window = Window.GetWindow(this) ?? throw new InvalidOperationException("The settings page has no window.");
      window.IsEnabled = false;
      try
      {
        using (var openFileDialog = new OpenFileDialog())
        {
          openFileDialog.Filter = this.GetConfigFileFilter();
          openFileDialog.FileName = !string.IsNullOrWhiteSpace(this.ConfigFile.Text) ? Path.GetFileName(this.ConfigFile.Text) : string.Empty;
          if (openFileDialog.ShowDialog() == DialogResult.OK)
          {
            var (result, errorMessage) = await this.configurationViewModel.ImportAsync(openFileDialog.FileName);
            if (!result)
            {
              System.Windows.MessageBox.Show(errorMessage, Strings.ImportError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
          }
        }
      }
      finally
      {
        window.IsEnabled = true;
      }
    }

    /// <summary>
    /// Handles export button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
    private async void ExportButtonClickAsync(object? sender, RoutedEventArgs e)
    {
      var window = Window.GetWindow(this) ?? throw new InvalidOperationException("The settings page has no window.");
      window.IsEnabled = false;
      try
      {
        using (var saveFileDialog = new SaveFileDialog())
        {
          saveFileDialog.Filter = this.GetConfigFileFilter();
          saveFileDialog.FileName = !string.IsNullOrWhiteSpace(this.ConfigFile.Text) ? Path.GetFileName(this.ConfigFile.Text) : string.Empty;
          if (saveFileDialog.ShowDialog() == DialogResult.OK)
          {
            await this.configurationViewModel.ExportAsync(saveFileDialog.FileName);
          }
        }
      }
      finally
      {
        window.IsEnabled = true;
      }
    }

    private string GetConfigFileFilter() => $"{Properties.Resources.JsonFilesFilterName}|*.{SerializationHelper.ConfigFileExtension}";
  }
}
