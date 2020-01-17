// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectSelectionWizardPage.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Controls.Primitives;
  using System.Windows.Data;

  /// <summary>
  /// Interaction logic for ProjectSelectionWizardPage.xaml.
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
        if (this.projectsTreeView.mainTreeView.Items.Count == 0)
        {
          return;
        }

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

    /// <summary>
    /// Handles the Loaded event of the uxGrouppingSettings control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void UxGrouppingSettingsLoaded(object sender, RoutedEventArgs e)
    {
      var bindingExpressionbase = BindingOperations.GetBindingExpressionBase((ComboBox)sender, Selector.SelectedItemProperty);
      bindingExpressionbase?.UpdateTarget();
    }
  }
}
