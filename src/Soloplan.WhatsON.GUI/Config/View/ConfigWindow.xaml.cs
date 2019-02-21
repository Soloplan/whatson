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
    /// Initializes a new instance of the <see cref="ConfigWindow"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public ConfigWindow(Configuration configuration)
    {
      this.configurationSource = configuration;
      this.configurationViewModel.Load(configuration);
      this.DataContext = this.configurationViewModel;
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

      var selectedItemTag = (string)((ListBoxItem)e.AddedItems[0]).Tag;
      switch (selectedItemTag)
      {
        case MainListItemTag:
          this.ConfigFrame.Content = new MainConfigPage();
          return;
        case SubjectsListItemTag:
          this.ConfigFrame.Content = new SubjectsPage(this.configurationViewModel.Subjects);
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
      this.configurationViewModel.ApplyToSource();
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
