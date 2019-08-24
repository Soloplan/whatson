namespace Soloplan.WhatsON.GUI.Configuration
{
  using System.Windows.Controls;

  /// <summary>
  /// Interaction logic for MessageControl.xaml
  /// </summary>
  public partial class MessageControl : UserControl
  {
    public MessageControl(string message)
    {
      this.InitializeComponent();
      this.uxMessgeTextBox.Text = message;
    }
  }
}
