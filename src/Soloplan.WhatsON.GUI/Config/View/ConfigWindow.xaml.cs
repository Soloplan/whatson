// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigWindow.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.View
{
  using System.Windows;
  using System.Windows.Controls;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

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
    /// The configuration view model.
    /// </summary>
    private readonly ConfigViewModel configurationViewModel = new ConfigViewModel();

    /// <summary>
    /// The configuration source.
    /// </summary>
    private readonly Configuration configurationSource;

    /// <summary>
    /// The subject page.
    /// </summary>
    private Page subjectPage;

    /// <summary>
    /// The main page.
    /// </summary>
    private Page mainPage;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigWindow"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public ConfigWindow(Configuration configuration)
    {
      this.configurationSource = configuration;
      this.configurationViewModel.Load(configuration);
      this.DataContext = this.configurationViewModel;
      GlobalConfigDataViewModel.Instance.UseConfiguration(this.configurationViewModel);
      this.InitializeComponent();
      this.ConfigTopicsListBox.SelectedIndex = 0;
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
          this.subjectPage = this.subjectPage ?? new SubjectsPage(this.configurationViewModel.Subjects);
          this.ConfigFrame.Content = this.subjectPage;
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
  }
}
