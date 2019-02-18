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

    private readonly ConfigViewModel configViewModel = new ConfigViewModel();

    public ConfigWindow(Configuration config)
    {
      this.configViewModel.Load(config);
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
          this.ConfigFrame.Content = new SubjectsPage(this.configViewModel.Subjects);
          return;
      }
    }
  }
}
