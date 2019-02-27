namespace Soloplan.WhatsON.GUI.Config.View
{
  using System.Windows.Controls;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  /// <summary>
  /// Interaction logic for SubjectConfigPage.xaml
  /// </summary>
  public partial class SubjectConfigPage : Page
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="SubjectConfigPage"/> class.
    /// </summary>
    /// <param name="subject">The subject.</param>
    public SubjectConfigPage(SubjectViewModel subject)
    {
      this.DataContext = subject;
      this.InitializeComponent();
      this.CreateConfigurationControls(subject);
    }

    /// <summary>
    /// Dynamically creates the configuration controls.
    /// </summary>
    /// <param name="subject">The subject view model.</param>
    private void CreateConfigurationControls(SubjectViewModel subject)
    {
      if (subject == null)
      {
        this.StackPanel.Children.Clear();
        return;
      }

      var subjectConfigAttributes = subject.GetSubjectConfigAttributes();
      foreach (var configAttribute in subjectConfigAttributes)
      {
        var configItem = subject.GetConfigurationByKey(configAttribute.Key);
        var builder = ConfigControlBuilderFactory.Instance.GetControlBuilder(configAttribute.Type);
        if (builder == null)
        {
          // TODO log error - no defined control builder for type configAttribute.Type
          continue;
        }

        var editor = builder.GetControl(configItem, configAttribute);
        this.StackPanel.Children.Add(editor);
      }
    }
  }
}
