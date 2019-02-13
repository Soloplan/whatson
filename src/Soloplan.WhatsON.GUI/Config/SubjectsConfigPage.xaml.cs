namespace Soloplan.WhatsON.GUI.Config
{
  using System.Collections.Generic;
  using System.Linq;
  using System.Windows.Controls;

  /// <summary>
  /// Interaction logic for SubjectsPage.xaml
  /// </summary>
  public partial class SubjectsPage : Page
  {
    public SubjectsPage(List<Subject> subjects)
    {
      this.InitializeComponent();
      this.LoadSubjects(subjects);
    }

    /// <summary>
    /// Loads the subjects to the <see cref="uxSubjects"/> control.
    /// </summary>
    /// <param name="subjects">The subjects.</param>
    private void LoadSubjects(List<Subject> subjects)
    {
      this.uxSubjects.Items.Clear();

      foreach (var subject in subjects)
      {
        var newItem = new ListBoxItem();
        newItem.Content = subject.Name;
        newItem.Tag = subject;
        this.uxSubjects.Items.Add(newItem);
      }

      if (this.uxSubjects.Items.Count > 0)
      {
        this.uxSubjects.SelectedIndex = 0;
      }
    }

    /// <summary>
    /// Handles the SelectionChanged event of the UxSubjects control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
    private void UxSubjectsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count != 1)
      {
        return;
      }

      var subject = (Subject)((ListBoxItem)e.AddedItems[0]).Tag;
      this.uxSubjectFrame.Content = new ServerHealthPage(subject);
    }
  }
}
