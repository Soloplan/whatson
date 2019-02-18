namespace Soloplan.WhatsON.GUI.Config.View
{
  using System.Collections.Generic;
  using System.Windows.Controls;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  /// <summary>
  /// Interaction logic for SubjectsPage.xaml
  /// </summary>
  public partial class SubjectsPage : Page
  {
    public SubjectsPage(IList<SubjectViewModel> subjects)
    {
      this.DataContext = subjects;
      this.InitializeComponent();
    }

    /// <summary>
    /// Handles the SelectionChanged event of the UxSubjects control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
    private void UxSubjectsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (!this.IsInitialized || e.AddedItems.Count != 1)
      {
        return;
      }

      this.SetSubjectFrameContent((SubjectViewModel)e.AddedItems[0]);
    }

    /// <summary>
    /// Handles the Initialized event of the Page control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    private void PageInitialized(object sender, System.EventArgs e)
    {
      this.SetSubjectFrameContent((SubjectViewModel)this.uxSubjects.SelectedItem);
    }

    private void SetSubjectFrameContent(SubjectViewModel subject)
    {
      this.uxSubjectFrame.Content = new SubjectConfigPage(subject);
    }
  }
}
