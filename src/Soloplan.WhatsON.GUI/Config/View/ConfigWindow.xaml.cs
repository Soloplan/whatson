// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigWindow.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.View
{
  using System;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Forms;
  using Soloplan.WhatsON.GUI.Config.ViewModel;
  using Soloplan.WhatsON.Serialization;
  using Application = System.Windows.Application;

  /// <summary>
  /// Interaction logic for ConfigWindow.xaml
  /// </summary>
  public partial class ConfigWindow : Window
  {
    /// <summary>
    /// The settings Main item tag.
    /// </summary>
    public const string MainListItemTag = "Main";

    /// <summary>
    /// The settings Subjects item tag.
    /// </summary>
    public const string SubjectsListItemTag = "Subjects";

    /// <summary>
    /// The settings About item tag.
    /// </summary>
    public const string AboutListItemTag = "About";

    /// <summary>
    /// The configuration view model.
    /// </summary>
    private readonly ConfigViewModel configurationViewModel = new ConfigViewModel();

    /// <summary>
    /// The configuration source.
    /// </summary>
    private ApplicationConfiguration configurationSource;

    /// <summary>
    /// The subject page.
    /// </summary>
    private Page subjectPage;

    /// <summary>
    /// The main page.
    /// </summary>
    private Page mainPage;

    /// <summary>
    /// The about page.
    /// </summary>
    private AboutPage aboutPage;

    /// <summary>
    /// The window shown flag.
    /// </summary>
    private bool windowShown;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigWindow"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public ConfigWindow(ApplicationConfiguration configuration)
    {
      this.configurationSource = configuration;
      this.configurationViewModel.Load(configuration);
      this.DataContext = this.configurationViewModel;
      GlobalConfigDataViewModel.Instance.UseConfiguration(this.configurationViewModel);
      this.InitializeComponent();
      this.ConfigTopicsListBox.SelectedIndex = 0;
      this.configurationViewModel.ConfigurationApplied += (s, e) =>
      {
        this.configurationSource = this.configurationViewModel.Configuration;
        this.ConfigurationApplied?.Invoke(s, e);
      };
      this.configurationViewModel.ConfigurationApplying += (s, e) => this.ConfigurationApplying?.Invoke(s, e);
    }

    /// <summary>
    /// Occurs when configuration was applied.
    /// </summary>
    public event EventHandler<ValueEventArgs<ApplicationConfiguration>> ConfigurationApplied;

    /// <summary>
    /// Occurs when configuration is about to be applied.
    /// </summary>
    public event EventHandler<EventArgs> ConfigurationApplying;

    /// <summary>
    /// Raises the <see cref="E:ContentRendered" /> event.
    /// </summary>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected override void OnContentRendered(EventArgs e)
    {
      base.OnContentRendered(e);

      this.Owner.Closing += this.OwnerClosing;
      if (this.windowShown)
      {
        return;
      }

      this.windowShown = true;
      ((App)Application.Current).ApplyTheme(this.configurationSource.DarkThemeEnabled);
    }

    /// <summary>
    /// Handles the SelectionChanged event of the ListBox control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
    private void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count != 1)
      {
        return;
      }

      if (!Validation.IsValid(this))
      {
        e.Handled = true;
        try
        {
          this.ConfigTopicsListBox.SelectionChanged -= this.ListBoxSelectionChanged;
          this.ConfigTopicsListBox.SelectedItem = e.RemovedItems[0];
          return;
        }
        finally
        {
          this.ConfigTopicsListBox.SelectionChanged += this.ListBoxSelectionChanged;
        }
      }

      var selectedItemTag = (string)((ListBoxItem)e.AddedItems[0]).Tag;
      switch (selectedItemTag)
      {
        case MainListItemTag:
          this.mainPage = this.mainPage ?? new MainConfigPage(this.configurationViewModel);
          this.ConfigFrame.Content = this.mainPage;
          return;
        case SubjectsListItemTag:
          this.subjectPage = this.subjectPage ?? new SubjectsPage(this.configurationViewModel.Subjects, this);
          this.ConfigFrame.Content = this.subjectPage;
          return;
        case AboutListItemTag:
          this.aboutPage = this.aboutPage ?? new AboutPage();
          this.ConfigFrame.Content = this.aboutPage;
          return;
      }
    }

    /// <summary>
    /// Handles the Closing event of the Window control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
    private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      if (!Validation.IsValid(this))
      {
        e.Cancel = true;
        return;
      }

      this.Owner.Closing -= this.OwnerClosing;
      this.configurationViewModel.ApplyToSourceAndSave();
    }

    /// <summary>
    /// Handles the ActionClick event of the SnackbarMessage control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void SnackbarMessageActionClick(object sender, RoutedEventArgs e)
    {
      this.configurationViewModel.Load(this.configurationSource);
    }

    /// <summary>
    /// Handles the Click event of the ImportExport button.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void ImportExportButtonClick(object sender, RoutedEventArgs e)
    {
      this.uxImportExportPopup.IsPopupOpen = !this.uxImportExportPopup.IsPopupOpen;
    }

    private string GetConfigFileFilter()
    {
      return $"{Properties.Resources.JsonFilesFilterName}|*.{SerializationHelper.ConfigFileExtension}";
    }

    /// <summary>
    /// Handles import button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void ImportButtonClick(object sender, RoutedEventArgs e)
    {
      var openFileDialog = new OpenFileDialog();
      openFileDialog.Filter = this.GetConfigFileFilter();
      if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        this.configurationViewModel.Import(openFileDialog.FileName);
      }
    }

    /// <summary>
    /// Handles export button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void ExportButtonClick(object sender, RoutedEventArgs e)
    {
      var saveFileDialog = new SaveFileDialog();
      saveFileDialog.Filter = this.GetConfigFileFilter();
      if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
      {
        this.configurationViewModel.Export(saveFileDialog.FileName);
      }
    }

    /// <summary>
    /// Prevents parent from closing when configuration is modified.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">Event args.</param>
    private void OwnerClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      e.Cancel = this.configurationViewModel.ConfigurationIsModified;
    }
  }
}
