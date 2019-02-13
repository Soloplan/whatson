namespace Soloplan.WhatsON.GUI.Config
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Windows.Controls;
  using System.Windows.Media;
  using Soloplan.WhatsON.ServerHealth;

  /// <summary>
  /// Interaction logic for ServerHealthPage.xaml
  /// </summary>
  public partial class ServerHealthPage : Page
  {
    public static Type SubjectType => typeof(ServerHealth);

    public ServerHealthPage(Subject subject)
    {
      this.InitializeComponent();
      var subjectConfigAttributes = subject.GetType().GetCustomAttributes(typeof(ConfigurationItemAttribute), true).Cast<ConfigurationItemAttribute>().ToList();
      foreach (var configAttribute in subjectConfigAttributes)
      {
        var configItem = subject.Configuration.FirstOrDefault(ci => ci.Key == configAttribute.Key);
        var labelControl = new Label();
        labelControl.Content = configAttribute.Key + ":";

        var builder = ConfigControlBuilderFactory.Instance.GetControlBuilder(configAttribute.Type);
        if (builder == null)
        {
          // TODO log error - no defined control builder for type configAttribute.Type
          continue;
        }

        var editor = builder.GetControl(configItem, configAttribute);

        this.StackPanel.Children.Add(labelControl);
        this.StackPanel.Children.Add(editor);
      }
    }

    // TODO probably will be needed
    public IEnumerable<Visual> EnumVisual(Visual myVisual)
    {
      for (var i = 0; i < VisualTreeHelper.GetChildrenCount(myVisual); i++)
      {
        // Retrieve child visual at specified index value.
        var childVisual = (Visual)VisualTreeHelper.GetChild(myVisual, i);

        // Enumerate children of the child visual object.
        foreach (var childItem in this.EnumVisual(childVisual))
        {
          yield return childItem;
        }

        yield return childVisual;
      }
    }
  }
}
