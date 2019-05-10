// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IConfigControlBuilder.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.View
{
  using System;
  using System.ComponentModel;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using System.Windows;
  using System.Windows.Controls;
  using MaterialDesignThemes.Wpf;
  using Soloplan.WhatsON.GUI.Config.ViewModel;
  using Soloplan.WhatsON.GUI.Config.Wizard;

  /// <summary>
  /// Interaction logic for SubjectsPage.xaml
  /// </summary>
  public partial class SubjectsPage : Page, INotifyPropertyChanged
  {
    /// <summary>
    /// The owner window.
    /// </summary>
    private readonly Window ownerWindow;

    /// <summary>
    /// The current subject.
    /// </summary>
    private SubjectViewModel currentSubject;

    /// <summary>
    /// The active subject supports wizard.
    /// </summary>
    private bool activeSubjectSupportsWizard;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubjectsPage"/> class.
    /// </summary>
    /// <param name="subjects">The subjects.</param>
    /// <param name="ownerWindow">The owner <see cref="Window"/>.</param>
    public SubjectsPage(SubjectViewModelCollection subjects, Window ownerWindow)
    {
      this.ownerWindow = ownerWindow;
      this.Subjects = subjects;
      this.DataContext = this;
      this.InitializeComponent();
      this.Subjects.Loaded -= this.SubjectsLoaded;
      this.Subjects.Loaded += this.SubjectsLoaded;
    }

    /// <summary>
    /// Occurs when a property was changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets the subjects.
    /// </summary>
    public SubjectViewModelCollection Subjects { get; private set; }

    /// <summary>
    /// Gets the current subject.
    /// </summary>
    public SubjectViewModel CurrentSubject => this.currentSubject ?? (SubjectViewModel)this.uxSubjects.SelectedItem;

    /// <summary>
    /// Gets or sets a value indicating whether active subject supports wizard.
    /// </summary>
    public bool ActiveSubjectSupportsWizard
    {
      get => this.activeSubjectSupportsWizard;
      set
      {
        this.activeSubjectSupportsWizard = value;
        this.OnPropertyChanged(nameof(this.ActiveSubjectSupportsWizard));
      }
    }

    /// <summary>
    /// Called when property was changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

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
      if (!Validation.IsValid(this) && e.RemovedItems.Count > 0)
      {
        e.Handled = true;
        try
        {
          this.uxSubjects.SelectionChanged -= this.UxSubjectsSelectionChanged;
          this.uxSubjects.SelectedItem = e.RemovedItems[0];
          return;
        }
        finally
        {
          this.uxSubjects.SelectionChanged += this.UxSubjectsSelectionChanged;
          this.currentSubject = null; // just mark that the backing field  should be initialized.
        }
      }

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
      this.ActiveSubjectSupportsWizard = this.CurrentSubject?.SourceSubjectPlugin?.SupportsWizard ?? false;
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
      if (this.uxSubjects.SelectedItem == null)
      {
        return;
      }

      await DialogHost.Show(new CreateEditSubjectDialog((SubjectViewModel)this.uxSubjects.SelectedItem, false), "SubjectsConfigPageHost");
    }

    /// <summary>
    /// Handles the request for subject rename.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
    private async void AddSubjectClick(object sender, System.Windows.RoutedEventArgs e)
    {
      if (!Validation.IsValid(this))
      {
        return;
      }

      this.currentSubject = new SubjectViewModel();
      var createEditDialod = new CreateEditSubjectDialog(this.currentSubject, true);
      var result = (bool)await DialogHost.Show(createEditDialod, "SubjectsConfigPageHost");
      if (result)
      {
        // TODO move to subject view model/model
        this.currentSubject.SourceSubjectPlugin = (ISubjectPlugin)createEditDialod.uxPluginType.SelectedItem;
        this.currentSubject.Load(null);
        this.Subjects.Add(this.currentSubject);
        this.uxSubjects.SelectedItem = this.currentSubject;
      }
    }

    /// <summary>
    /// Handles the start wizard button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
    private void StartWizardClick(object sender, System.Windows.RoutedEventArgs e)
    {
      var wizardController = new WizardController(this.ownerWindow);
      if (wizardController.Start(this.CurrentSubject.SourceSubjectPlugin))
      {
        var selectedProjects = wizardController.GetSelectedProjects();
        if (selectedProjects.Count != 1)
        {
          throw new InvalidOperationException("One selected project is required.");
        }

        var currentSubjectAsAssignable = this.CurrentSubject.SourceSubjectPlugin as IAssignServerProject;
        if (currentSubjectAsAssignable == null)
        {
          throw new InvalidOperationException("Subject does not support assign from server project.");
        }

        currentSubjectAsAssignable.AssignServerProject(selectedProjects[0], this.CurrentSubject, wizardController.ProposedServerAddress);
      }
    }
  }
}
