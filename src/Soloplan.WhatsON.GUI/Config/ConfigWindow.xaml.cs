namespace Soloplan.WhatsON.GUI.Config
{
  using System.Windows;
  using System.Windows.Controls;

  /// <summary>
  /// Interaction logic for ConfigWindow.xaml
  /// </summary>
  public partial class ConfigWindow : Window
  {
    private readonly Configuration config;

    public const string MainListItemTag = "Main";
    public const string SubjectsListItemTag = "Subjects";

    public ConfigWindow(Configuration config)
    {
      this.config = config;
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
          this.ConfigFrame.Content = new SubjectsPage(this.config.Subjects);
          return;
      }
    }
  }
}
