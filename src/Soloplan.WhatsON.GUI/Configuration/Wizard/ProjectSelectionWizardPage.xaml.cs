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

  public partial class ProjectSelectionWizardPage : Page
  {
    private readonly WizardController wizardController;

    public ProjectSelectionWizardPage(WizardController wizardController)
    {
      this.wizardController = wizardController;
      this.InitializeComponent();
      this.projectsTreeView.DataContext = new ProjectBrowserViewModel(wizardController.Projects);
    }

    private async void ProjectsKeyDownAsync(object? sender, System.Windows.Input.KeyEventArgs e)
    {
      if (e.Key == System.Windows.Input.Key.Enter && e.OriginalSource is ListBoxItem)
      {
        e.Handled = true;
        await this.wizardController.GoToNextPageAsync();
      }
    }

    private void UxGrouppingSettingsLoaded(object? sender, RoutedEventArgs e)
    {
      var binding = BindingOperations.GetBindingExpressionBase(sender as ComboBox, Selector.SelectedItemProperty);
      binding?.UpdateTarget();
    }
  }
}
