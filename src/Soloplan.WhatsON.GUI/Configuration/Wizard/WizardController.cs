// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WizardController.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Controls;
  using MaterialDesignThemes.Wpf;
  using NLog;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.Localization;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Controls the execution of a wizard which allows to create or edit a project connection.
  /// </summary>
  /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
  public partial class WizardController : INotifyPropertyChanged
  {
    /// <summary>
    /// The timeout used to detect timeouts when querying build servers.
    /// </summary>
    private const int QueryTimeout = 10000;

    private const string AssignToTargetGroup = "AssignToTargetGroup";

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The owner window.
    /// </summary>
    private readonly Window? ownerWindow;

    private readonly ApplicationConfiguration config;

    /// <summary>
    /// The wizard dialog settings.
    /// </summary>
    private readonly WindowSettings wizardDialogSettings;

    /// <summary>
    /// The wizard window.
    /// </summary>
    private WizardWindow? wizardWindow;

    /// <summary>
    /// The projects view model.
    /// </summary>
    private ProjectViewModelList? projects;

    /// <summary>
    /// The proposed server address.
    /// </summary>
    private string? proposedServerAddress;

    private ConnectorPlugin? selectedConnectorType;

    /// <summary>
    /// The connector view model.
    /// </summary>
    private ConnectorViewModel? editedConnectorViewModel;

    /// <summary>
    /// The force of <see cref="IsPreviousStepEnabled" /> flag.
    /// </summary>
    private bool? forceIsPreviousStepEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="WizardController" /> class.
    /// </summary>
    /// <param name="ownerWindow">The owner window.</param>
    /// <param name="config">The configuration.</param>
    /// <param name="wizardDialogSettings">The wizard dialog settings.</param>
    /// <param name="targetGroupName">The existing group to use by default, or null for normal grouping.</param>
    public WizardController(Window? ownerWindow, ApplicationConfiguration config, WindowSettings? wizardDialogSettings, string? targetGroupName = null)
    {
      this.ownerWindow = ownerWindow;
      this.config = config;
      this.wizardDialogSettings = wizardDialogSettings ?? new WindowSettings();
      this.TargetGroupName = targetGroupName;
      this.GroupingSettings = this.InitializeGrouppingSettings();
      this.SelectedGroupingSetting = this.GroupingSettings[0];
    }

    /// <summary>
    /// Gets the projects tree.
    /// </summary>
    public ProjectViewModelList Projects => this.projects ?? (this.projects = new ProjectViewModelList());

    /// <summary>
    /// Gets the projects tree.
    /// </summary>
    public IReadOnlyList<GrouppingSetting> GroupingSettings { get; }

    public string? TargetGroupName { get; }

    public bool IsAddingToTargetGroup => this.TargetGroupName != null && this.SelectedGroupingSetting.Id == AssignToTargetGroup;

    /// <summary>
    /// Gets or sets the selected grouping setting.
    /// </summary>
    public GrouppingSetting SelectedGroupingSetting { get; set; }

    /// <summary>
    /// Gets a value indicating whether wizard is NOT on it's first step.
    /// </summary>
    public bool IsNotFirstStep => this.CurrentPage != null && !(this.CurrentPage is ConnectionWizardPage);

    /// <summary>
    /// Gets a value indicating whether wizard is on it's last step.
    /// </summary>
    public bool IsLastStep => this.CurrentPage is ProjectSelectionWizardPage;

    /// <summary>
    /// Gets a value indicating whether wizard is NOT on it's last step.
    /// </summary>
    public bool IsNotLastStep => !this.IsLastStep;

    /// <summary>
    /// Gets a value indicating whether this the next step button is enabled.
    /// </summary>
    public bool IsNextStepEnabled => this.IsNotLastStep && !this.IsProposedAddressEmpty;

    /// <summary>
    /// Gets or sets a value indicating whether this the previous step button is enabled.
    /// </summary>
    public bool IsPreviousStepEnabled
    {
      get
      {
        if (this.forceIsPreviousStepEnabled.HasValue)
        {
          return this.forceIsPreviousStepEnabled.Value;
        }

        return this.IsNotFirstStep;
      }

      set => this.forceIsPreviousStepEnabled = value;
    }

    /// <summary>
    /// Gets a value indicating whether the proposed address is empty.
    /// </summary>
    /// <value>
    /// <c>true</c> if the proposed address is empty; otherwise, <c>false</c>.
    /// </value>
    public bool IsProposedAddressEmpty => string.IsNullOrWhiteSpace(this.ProposedServerAddress);

    /// <summary>
    /// Indicates if auto detection is enabled, when set also updates <seealso cref="IsAutoDetectionDisabled" />
    /// to keep the detection controls synchronized.
    /// </summary>
    public bool IsAutoDetectionEnabled { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether automatic detection is disabled.
    /// </summary>
    public bool IsAutoDetectionDisabled => !this.IsAutoDetectionEnabled;

    /// <summary>
    /// Gets or sets a value indicating whether multi selection mode is active.
    /// </summary>
    public bool MultiSelectionMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the proposed server address.
    /// </summary>
    public string? ProposedServerAddress
    {
      get
      {
        if (Uri.TryCreate(this.proposedServerAddress, UriKind.Absolute, out var address))
        {
          return address.AbsoluteUri;
        }

        return this.proposedServerAddress;
      }

      set => this.proposedServerAddress = value;
    }

    public List<string> AvailableServers => this.config.ConnectorsConfiguration
      .Where(configuration => this.IsAutoDetectionEnabled || configuration.Type == this.SelectedConnectorType?.Name)
      .Select(configuration => configuration.GetConfigurationByKey(Connector.ServerAddress).Value)
      .Select(address => Uri.TryCreate(address, UriKind.Absolute, out var uri) ? uri : null)
      .OfType<Uri>().Select(uri => uri.AbsoluteUri).Distinct().ToList();

    public List<ConnectorPlugin> AvailableConnectorTypes =>
      PluginManager.Instance.ConnectorPlugins.OrderByDescending(x => this.config.ConnectorsConfiguration.Count(y => y.Type == x.Name)).ToList();

    public ConnectorPlugin? SelectedConnectorType
    {
      get
      {
        if (this.selectedConnectorType == null)
        {
          this.selectedConnectorType = this.AvailableConnectorTypes.FirstOrDefault();
        }

        return this.selectedConnectorType;
      }

      set => this.selectedConnectorType = value;
    }

    /// <summary>
    /// Gets a value indicating whether any project is checked.
    /// </summary>
    public bool IsAnyProjectChecked { get; private set; }

    /// <summary>
    /// Gets a value indicating whether finish button should be enabled.
    /// </summary>
    /// <value>
    /// <c>true</c> if this the finish button should be enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsFinishEnabled { get; private set; }

    /// <summary>
    /// The current page.
    /// </summary>
    private Page? CurrentPage { get; set; }

    /// <summary>
    /// Gets the wizard frame.
    /// </summary>
    private Frame WizardFrame => this.wizardWindow?.Frame ?? throw new InvalidOperationException("The wizard has not been started.");

    /// <summary>
    /// Starts the wizard.
    /// </summary>
    /// <param name="connector">The connector view model.</param>
    /// <returns>
    /// True if the wizard was finished correctly and not canceled in any way.
    /// </returns>
    public Task<bool> StartAsync(ConnectorViewModel connector)
    {
      this.editedConnectorViewModel = connector;
      this.IsPreviousStepEnabled = false;
      return this.StartAsync(false);
    }

    /// <summary>
    /// Starts the wizard and applies the results to given configuration.
    /// Multiple, new connectors might be created.
    /// </summary>
    /// <param name="applyConfig">if set to <c>true</c> [apply configuration].</param>
    /// <returns>
    /// True if the wizard was finished correctly and not canceled in any way.
    /// </returns>
    public async Task<bool> StartAsync(bool applyConfig = true)
    {
      this.wizardWindow = new WizardWindow(this);
      this.wizardWindow.Owner = this.ownerWindow;
      this.wizardWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      if (this.editedConnectorViewModel == null)
      {
        this.GoToConnectionStep();
      }
      else
      {
        this.ProposedServerAddress = this.editedConnectorViewModel.GetConfigurationByKey(Connector.ServerAddress)?.Value;

        async void OnActivated(object? sender, EventArgs e)
        {
          this.wizardWindow.Activated -= OnActivated;
          await this.GoToProjectSelectionStep();
        }

        this.wizardWindow.Activated += OnActivated;
      }

      this.wizardDialogSettings.Apply(this.wizardWindow);
      if (this.TargetGroupName != null)
      {
        this.SelectedGroupingSetting = this.GroupingSettings[0];
      }

      var wizardShowResult = this.wizardWindow.ShowDialog();
      this.wizardDialogSettings.Parse(this.wizardWindow);
      if (wizardShowResult == true)
      {
        if (applyConfig)
        {
          await this.ApplyToConfiguration();
        }

        return true;
      }

      return false;
    }

    /// <summary>
    /// Retrieves selected projects.
    /// </summary>
    /// <returns>The selected projects.</returns>
    public IList<Project> GetSelectedProjects()
    {
      if (this.Projects == null || this.Projects.Count == 0)
      {
        return new List<Project>();
      }

      var serverProjects = new List<Project>();
      var checkedProjects = this.Projects.GetChecked();
      foreach (var checkedProject in checkedProjects.Where(p => p.Projects.Count == 0 && (!this.MultiSelectionMode || !p.AlreadyAdded)))
      {
        var newProject = new Project(
          checkedProject.Address,
          checkedProject.Name,
          checkedProject.DirectAddress,
          checkedProject.FullName,
          checkedProject.Description,
          this.Projects.PlugIn,
          this.CreateParentProjectStructure(checkedProject));
        serverProjects.Add(newProject);
      }

      return serverProjects;
    }

    /// <summary>
    /// Goes to next page of the wizard.
    /// </summary>
    public async Task GoToNextPageAsync()
    {
      if (this.WizardFrame.Content is ConnectionWizardPage)
      {
        await this.GoToProjectSelectionStep();
        return;
      }

      if (this.WizardFrame.Content is ProjectSelectionWizardPage)
      {
        this.Finish();
      }
    }

    /// <summary>
    /// Goes to previous page of the wizard.
    /// </summary>
    public void GoToPrevPage()
    {
      if (this.WizardFrame.Content is ProjectSelectionWizardPage)
      {
        this.GoToConnectionStep();
      }
    }

    /// <summary>
    /// Finishes this wizard.
    /// </summary>
    public void Finish()
    {
      if (this.IsAnyProjectChecked && this.wizardWindow != null)
      {
        this.wizardWindow.DialogResult = true;
        this.wizardWindow.Close();
      }
    }

    internal static async Task<(ConnectorPlugin Plugin, IList<Project> Projects)> DiscoverProjects(
      IEnumerable<ConnectorPlugin> plugins, string address, TimeSpan timeout)
    {
      var pending = plugins.Select(plugin => (Plugin: plugin, Task: QueryProjects(plugin, address))).ToList();
      var deadline = Task.Delay(timeout);
      while (pending.Count > 0)
      {
        var completed = await Task.WhenAny(pending.Select(candidate => (Task)candidate.Task).Append(deadline));
        if (completed == deadline)
        {
          throw new TimeoutException(Strings.DiscoveryTimedOut);
        }

        var candidate = pending.First(candidate => candidate.Task == completed);
        pending.Remove(candidate);
        var projects = await candidate.Task;
        if (projects != null)
        {
          return (candidate.Plugin, projects);
        }
      }

      throw new InvalidOperationException(Strings.InvalidServerOrPlugin);
    }

    private static async Task<IList<Project>?> QueryProjects(ConnectorPlugin plugin, string address)
    {
      try
      {
        return await plugin.GetProjects(address);
      }
      catch (Exception exception)
      {
        log.Debug(exception, "Project discovery did not match plugin {plugin}.", plugin.Name);
        return null;
      }
    }

    /// <summary>
    /// Called when property was changed.
    /// </summary>
    private void OnIsAutoDetectionEnabledChanged()
    {
      if (this.IsAutoDetectionEnabled)
      {
        this.SelectedConnectorType = null;
      }
    }

    /// <summary>
    /// Creates the group name from project tree.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="currentName">Name of the current.</param>
    /// <returns>Group name.</returns>
    private string? CreateGroupNameFromProjectTree(Project? project, string? currentName = null)
    {
      if (project == null)
      {
        return currentName;
      }

      if (currentName?.Length > 0)
      {
        currentName = currentName.Insert(0, "/");
      }

      currentName = project.Name + currentName;
      if (project.Parent == null)
      {
        return currentName;
      }

      return this.CreateGroupNameFromProjectTree(project.Parent, currentName);
    }

    /// <summary>
    /// Applies the results of the wizard to configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">At least one selected project is required.</exception>
    private async Task ApplyToConfiguration()
    {
      var selectedProjects = this.GetSelectedProjects();
      if (selectedProjects.Count < 1)
      {
        throw new InvalidOperationException(Strings.SelectedProjectRequired);
      }

      var configurationViewModel = new ConfigViewModel();
      configurationViewModel.Load(this.config);

      foreach (var selectedProject in selectedProjects)
      {
        var newConnector = new ConnectorViewModel();
        newConnector.SourceConnectorPlugin = selectedProject.Plugin;
        newConnector.Name = selectedProject.Name;
        newConnector.Load(null);
        if (this.SelectedGroupingSetting.Id == WizardWindow.AssignGroupsForAddedProjects)
        {
          var groupName = this.CreateGroupNameFromProjectTree(selectedProject.Parent);

          if (!string.IsNullOrWhiteSpace(groupName))
          {
            newConnector.GetConfigurationByKey(Connector.Category).Value = groupName;
          }
        }

        configurationViewModel.Connectors.Add(newConnector);

        (selectedProject.Plugin ?? throw new InvalidOperationException("The selected project has no plugin.")).Configure(selectedProject, newConnector, this.proposedServerAddress);
        if (this.IsAddingToTargetGroup)
        {
          newConnector.GetConfigurationByKey(Connector.Category).Value = this.TargetGroupName;
        }
      }

      if (configurationViewModel.ConfigurationIsModified)
      {
        configurationViewModel.Connectors.ApplyToConfiguration(this.config);
        await SerializationHelper.Instance.SaveConfigurationAsync(this.config);
      }
    }

    /// <summary>
    /// Goes to connection step of the wizard.
    /// </summary>
    private void GoToConnectionStep()
    {
      this.CurrentPage = new ConnectionWizardPage(this);
      this.CurrentPage.DataContext = this;
      this.WizardFrame.Content = this.CurrentPage;
      if (this.editedConnectorViewModel != null)
      {
        this.SelectedConnectorType = this.editedConnectorViewModel.SourceConnectorPlugin;
      }
    }

    private void ProcessServerSubProjects(IList<Project> projects, ProjectViewModel? projectViewModel)
    {
      foreach (var project in projects.OrderBy(x => x.Name))
      {
        var newProject = projectViewModel == null ? this.Projects.AddProject(project) : projectViewModel.AddProject(project);
        newProject.Parent = projectViewModel;
        newProject.Address = project.Address;
        newProject.DirectAddress = project.DirectAddress;

        var alreadyExists = this.config.ConnectorsConfiguration.Where(x =>
        {
          var address = x.GetConfigurationByKey(Connector.ServerAddress)?.Value;
          if (string.IsNullOrWhiteSpace(address))
          {
            return false;
          }

          return x.Type == this.Projects.PlugIn?.Name && Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.AbsoluteUri.Equals(this.ProposedServerAddress)
                                                           && x.GetConfigurationByKey(Connector.ProjectName)?.Value
                                                           == (!string.IsNullOrWhiteSpace(project.FullName) ? project.FullName : project.Name);
        }).ToList();

        newProject.AlreadyAdded = alreadyExists.Any();
        newProject.AddedProject = alreadyExists.Any()
          ? string.Join(" - ", alreadyExists.Select(x => $"{x.GetConfigurationByKey(Connector.Category).Value}/{x.Name}"))
          : null;

        this.ProcessServerSubProjects(project.Children, newProject);
      }
    }

    /// <summary>
    /// Prepares the projects list.
    /// </summary>
    /// <returns>The task.</returns>
    private async Task PrepareProjectsList()
    {
      IEnumerable<ConnectorPlugin> plugins = this.editedConnectorViewModel != null
        ? new[] { this.editedConnectorViewModel.SourceConnectorPlugin }.OfType<ConnectorPlugin>()
        : this.IsAutoDetectionEnabled ? PluginManager.Instance.ConnectorPlugins : new[] { this.SelectedConnectorType }.OfType<ConnectorPlugin>();
      var address = this.ProposedServerAddress;
      ArgumentException.ThrowIfNullOrWhiteSpace(address);
      var discovered = await DiscoverProjects(plugins, address.TrimEnd('/') + "/", TimeSpan.FromMilliseconds(QueryTimeout));
      this.projects = new ProjectViewModelList { MultiSelectionMode = this.MultiSelectionMode, PlugIn = discovered.Plugin };
      this.ProcessServerSubProjects(discovered.Projects, null);

      this.AttachToProjectsPropertyChanged();
    }

    /// <summary>
    /// Attaches action to each project PropertyChanged event.
    /// </summary>
    private void AttachToProjectsPropertyChanged()
    {
      var projectCheckedChangedAction = new Action(() => this.IsAnyProjectChecked = this.Projects.Any(p => p.IsAnyChecked()));
      foreach (var project in this.Projects)
      {
        this.AttachToProjectPropertyChanged(project, projectCheckedChangedAction);
      }
    }

    /// <summary>
    /// Attaches action to each project PropertyChanged event. Applies the same action tosub projects.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="action">The action.</param>
    private void AttachToProjectPropertyChanged(ProjectViewModel project, Action action)
    {
      project.PropertyChanged += (s, e) => action();
      foreach (var subProject in project.Projects)
      {
        this.AttachToProjectPropertyChanged(subProject, action);
      }
    }

    /// <summary>
    /// Goes to project selection step of the wizard.
    /// </summary>
    private async Task GoToProjectSelectionStep()
    {
      var window = this.wizardWindow;
      if (window == null)
      {
        return;
      }

      var error = false;
      var errorMessage = string.Empty;
      var waitControl = new WaitControl();
      var waitDailogTask = DialogHost.Show(waitControl, "WizardWaitDialogHostId");
      try
      {
        await this.PrepareProjectsList();
      }
      catch (Exception e)
      {
        error = true;
        errorMessage = Localizer.ProjectError(e.Message);
      }

      if (!window.IsVisible)
      {
        return;
      }

      DialogHost.CloseDialogCommand.Execute("WizardWaitDialogHostId", window.WizardWaitDialogHost);

      if (!error)
      {
        this.CurrentPage = new ProjectSelectionWizardPage(this);
        this.CurrentPage.DataContext = this;
        this.WizardFrame.Content = this.CurrentPage;
      }
      else if (window.IsVisible)
      {
        var errorDialog = new MessageControl(errorMessage);
        await DialogHost.Show(errorDialog, "WizardWaitDialogHostId");
        if (this.editedConnectorViewModel != null)
        {
          window.Close();
        }
      }
    }

    /// <summary>
    /// Initializes the groupping settings.
    /// </summary>
    /// <returns>The list with initlized groupping settings.</returns>
    private List<GrouppingSetting> InitializeGrouppingSettings()
    {
      var grouppingSettings = new List<GrouppingSetting>();
      if (this.TargetGroupName != null)
      {
        grouppingSettings.Add(new GrouppingSetting(
          () => string.IsNullOrEmpty(this.TargetGroupName) ? Strings.NoAutomaticGrouping : string.Format(Strings.AddJobsToGroupFormat, this.TargetGroupName),
          AssignToTargetGroup));
      }

      grouppingSettings.Add(new GrouppingSetting(() => Strings.AssignParentAsGroup, WizardWindow.AssignGroupsForAddedProjects));
      grouppingSettings.Add(new GrouppingSetting(() => Strings.AddHierarchyToProjectName, WizardWindow.AddProjectPathToProjectName));
      if (this.TargetGroupName != string.Empty)
      {
        grouppingSettings.Add(new GrouppingSetting(() => Strings.NoAutomaticGrouping, WizardWindow.DoNotAssignAnyGroups));
      }

      return grouppingSettings;
    }

    /// <summary>
    /// Creates the parent project structure.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    /// <returns>Parent project.</returns>
    private Project? CreateParentProjectStructure(ProjectViewModel viewModel)
    {
      if (viewModel.Parent == null)
      {
        return null;
      }

      var newParent = new Project(
        viewModel.Parent.Address,
        viewModel.Parent.Name,
        viewModel.Parent.DirectAddress,
        viewModel.Parent.FullName,
        viewModel.Parent.Description,
        this.Projects.PlugIn,
        this.CreateParentProjectStructure(viewModel.Parent));
      return newParent;
    }
  }
}
