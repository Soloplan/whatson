// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.View
{
  using System.Windows.Controls;
  using MaterialDesignThemes.Wpf;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  /// <summary>
  /// Interaction logic for SubjectsPage.xaml
  /// </summary>
  public partial class SubjectsPage : Page
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="SubjectsPage"/> class.
    /// </summary>
    /// <param name="subjects">The subjects.</param>
    public SubjectsPage(SubjectViewModelCollection subjects)
    {
      this.Subjects = subjects;
      this.DataContext = this;
      this.InitializeComponent();
      this.Subjects.Loaded -= this.SubjectsLoaded;
      this.Subjects.Loaded += this.SubjectsLoaded;
    }

    /// <summary>
    /// Gets the subjects.
    /// </summary>
    public SubjectViewModelCollection Subjects { get; private set; }

    /// <summary>
    /// Gets the current subject.
    /// </summary>
    public SubjectViewModel CurrentSubject => (SubjectViewModel)this.uxSubjects.SelectedItem;

    /// <summary>
    /// Handles the Loaded event of the Subjects control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
    private void SubjectsLoaded(object sender, System.EventArgs e)
    {
      if (this.uxSubjects.Items.Count > 0 && this.uxSubjects.SelectedItem == null)
      {
        this.uxSubjects.SelectedItem = this.uxSubjects.Items[0];
      }
    }

    /// <summary>
    /// Handles the SelectionChanged event of the UxSubjects control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
    private void UxSubjectsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (!this.IsInitialized)
      {
        return;
      }

      this.SetSubjectFrameContent(e.AddedItems.Count != 0 ? (SubjectViewModel)e.AddedItems[0] : null);
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

    /// <summary>
    /// Sets the content of the subject frame.
    /// </summary>
    /// <param name="subject">The subject.</param>
    private void SetSubjectFrameContent(SubjectViewModel subject)
    {
      this.uxSubjectFrame.Content = new SubjectConfigPage(subject);
    }

    /// <summary>
    /// Handles the request for subject deletion.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
    private async void DeleteSubjectClick(object sender, System.Windows.RoutedEventArgs e)
    {
      var result = (bool)await DialogHost.Show(new OkCancelDialog("Are you sure you want to delete this subject?"), "SubjectsConfigPageHost"); // TODO translate
      if (result)
      {
        this.Subjects.Remove(this.CurrentSubject);
      }
    }

    /// <summary>
    /// Handles the request for subject rename.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
    private async void RenameSubjectClick(object sender, System.Windows.RoutedEventArgs e)
    {
      await DialogHost.Show(new CreateEditSubjectDialog((SubjectViewModel)this.uxSubjects.SelectedItem), "SubjectsConfigPageHost");
    }
  }
}
