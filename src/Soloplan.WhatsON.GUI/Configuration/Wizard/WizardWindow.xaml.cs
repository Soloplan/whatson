// <copyright file="WizardWindow.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System;
  using System.Linq;
  using System.Windows;
  using Soloplan.WhatsON.GUI.Common;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;

  /// <summary>
  /// Interaction logic for WizardWindow.xaml.
  /// </summary>
  public partial class WizardWindow : Window, IAdditionalWindowSettingsSupport
  {
    /// <summary>
    /// The do not create groups grouping setting identifier.
    /// </summary>
    public const string DoNotAssignAnyGroups = "DoNotAssignAnyGroups";

    /// <summary>
    /// The add project path to project name grouping setting identifier.
    /// </summary>
    public const string AddProjectPathToProjectName = "AddProjectPathToProjectName";

    /// <summary>
    /// The assign groups for added projects grouping setting  identifier.
    /// </summary>
    public const string AssignGroupsForAddedProjects = "AssignGroupsForAddedProjects";

    /// <summary>
    /// The last selected wizard project grouping setting identifier.
    /// </summary>
    public const string LastSelectedWizardProjectGroupingSettingId = "LastSelectedWizardProjectGroupingSettingId";

    /// <summary>
    /// The wizard controller.
    /// </summary>
    private readonly WizardController wizardController;

    /// <summary>
    /// The window shown flag.
    /// </summary>
    private bool windowShown;

    /// <summary>
    /// Initializes a new instance of the <see cref="WizardWindow" /> class.
    /// </summary>
    /// <param name="wizardController">The wizard controller.</param>
    public WizardWindow(WizardController wizardController)
    {
      this.wizardController = wizardController;
      this.DataContext = wizardController;
      this.InitializeComponent();
    }

    /// <summary>
    /// Applies the additional window settings to the <see cref="T:System.Windows.Window" /> instance.
    /// </summary>
    /// <param name="getAdditionalSettingValue">The get additional setting value.</param>
    public void Apply(WindowSettings.GetAdditionalSettingValueDelegate getAdditionalSettingValue)
    {
      var savedOrDefaultGroupingSettingId = getAdditionalSettingValue(nameof(LastSelectedWizardProjectGroupingSettingId), AddProjectPathToProjectName);
      var groupingSetting = this.wizardController.GroupingSettings.FirstOrDefault(g => g.Id == savedOrDefaultGroupingSettingId);
      this.wizardController.SelectedGroupingSetting = groupingSetting ?? this.wizardController.GroupingSettings.First();
    }

    /// <summary>
    /// Parses the specified additional setting values from <see cref="T:System.Windows.Window" /> instance on which the interface is implemented.
    /// </summary>
    /// <param name="setAdditionalSettingValue">The set additional setting value.</param>
    public void Parse(WindowSettings.SetAdditionalSettingValueDelegate setAdditionalSettingValue)
    {
      setAdditionalSettingValue(nameof(LastSelectedWizardProjectGroupingSettingId), this.wizardController.SelectedGroupingSetting.Id);
    }

    /// <summary>
    /// Raises the <see cref="E:ContentRendered" /> event.
    /// </summary>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected override void OnContentRendered(EventArgs e)
    {
      base.OnContentRendered(e);

      if (this.windowShown)
      {
        return;
      }

      this.windowShown = true;
    }

    /// <summary>
    /// Next page click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void NextClick(object sender, RoutedEventArgs e)
    {
      this.wizardController.GoToNextPage();
    }

    /// <summary>
    /// Previous page click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void PrevClick(object sender, RoutedEventArgs e)
    {
      this.wizardController.GoToPrevPage();
    }

    /// <summary>
    /// Finish click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void FinishClick(object sender, RoutedEventArgs e)
    {
      this.wizardController.Finish();
    }
  }
}
