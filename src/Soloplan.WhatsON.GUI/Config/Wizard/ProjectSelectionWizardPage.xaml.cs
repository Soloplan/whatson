// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectSelectionWizardPage.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.Wizard
{
  using System.Windows.Controls;

  /// <summary>
  /// Interaction logic for ProjectSelectionWizardPage.xaml
  /// </summary>
  public partial class ProjectSelectionWizardPage : Page
  {
    /// <summary>
    /// The wizard controllers.
    /// </summary>
    private readonly WizardController wizardController;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectSelectionWizardPage" /> class.
    /// </summary>
    /// <param name="wizardController">The wizard controller.</param>
    public ProjectSelectionWizardPage(WizardController wizardController)
    {
      this.wizardController = wizardController;
      this.InitializeComponent();
      this.projectsTreeView.Loaded += (s, e) =>
      {
        var treeViewItem = (TreeViewItem)this.projectsTreeView.mainTreeView.ItemContainerGenerator.ContainerFromItem(this.projectsTreeView.mainTreeView.Items[0]);
        treeViewItem?.Focus();
      };
    }

    /// <summary>
    /// Handles the PreviewKeyUp event of the MainTreeView control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.Windows.Input.KeyEventArgs"/> instance containing the event data.</param>
    private void MainTreeViewPreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == System.Windows.Input.Key.Enter)
      {
        this.wizardController.GoToNextPage();
        e.Handled = true;
      }
    }
  }
}
