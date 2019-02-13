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

    public ConfigWindow(Configuration config)
    {
      this.config = config;
      this.InitializeComponent();
    }

    /// <summary>
    /// Handles the SelectionChanged event of the ListBox control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
    private void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      this.ConfigFrame.Content = new SubjectsPage(this.config.Subjects);
    }
  }
}
