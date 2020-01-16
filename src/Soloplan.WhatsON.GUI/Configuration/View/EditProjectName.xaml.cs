namespace Soloplan.WhatsON.GUI.Configuration.View
{
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Input;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;

  /// <summary>
  /// Interaction logic for EditProjectName.xaml.
  /// </summary>
  public partial class EditProjectName : UserControl
  {
    public EditProjectName()
    {
      this.InitializeComponent();
    }

    public EditProjectName(ConnectorViewModel model)
    : this()
    {
      this.DataContext = model;
    }

    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
    }

    private void CancelButtonClick(object sender, RoutedEventArgs e)
    {
    }

    private void CanCloseDialog(object sender, CanExecuteRoutedEventArgs e)
    {
      if (e.Parameter is bool param)
      {
        e.CanExecute = !param || !string.IsNullOrEmpty(((ConnectorViewModel)this.DataContext).Name);
        e.Handled = true;
      }
    }
  }
}
