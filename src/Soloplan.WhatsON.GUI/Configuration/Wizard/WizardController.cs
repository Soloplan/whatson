// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WizardController.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.Wizard
{
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.ComponentModel;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Controls;
  using MaterialDesignThemes.Wpf;
  using NLog;
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.Configuration;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Controls the execution of a wizard which allows to create or edit a project connection.
  /// </summary>
  /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
  public class WizardController : INotifyPropertyChanged
  {
    /// <summary>
    /// The timeout used to detect timeouts when querying build servers.
    /// </summary>
    private const int QueryTimeout = 10000;

    /// <summary>
    /// Logger instance used by this class.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// The owner window.
    /// </summary>
    private readonly Window ownerWindow;

    private readonly ApplicationConfiguration config;

    /// <summary>
    /// The wizard dialog settings.
    /// </summary>
    private readonly WindowSettings wizardDialogSettings;

    /// <summary>
    /// The wizard window.
    /// </summary>
    private WizardWindow wizardWindow;

    /// <summary>
    /// The current page.
    /// </summary>
    private Page currentPage;

    /// <summary>
    /// The projects view model.
    /// </summary>
    private ProjectViewModelList projects;

    /// <summary>
    /// Is any project checked.
    /// </summary>
    private bool isAnyProjectChecked;

    /// <summary>
    /// Is finish enabled.
    /// </summary>
    private bool isFinishEnabled;

    /// <summary>
    /// The proposed server address.
    /// </summary>
    private string proposedServerAddress;

    /// <summary>
    /// Is proposed address empty flag.
    /// </summary>
    private bool isProposedAddressEmpty = true;

    /// <summary>
    /// Is automatical connector type enabled.
    /// </summary>
    private bool isAutoDetectionEnabled = true;

    /// <summary>
    /// Is automatical connector type disabled.
    /// </summary>
    private bool isAutoDetectionDisabled = false;

    private ConnectorPlugin selectedConnectorType;

    /// <summary>
    /// The connector view model.
    /// </summary>
    private ConnectorViewModel editedConnectorViewModel;

    /// <summary>
    /// The force of <see cref="IsPreviousStepEnabled"/> flag.
    /// </summary>
    private bool? forceIsPreviousStepEnabled;

    /// <summary>
    /// The selected grouping setting.
    /// </summary>
    private GrouppingSetting selectedGroupingSetting;

    /// <summary>
    /// Initializes a new instance of the <see cref="WizardController" /> class.
    /// </summary>
    /// <param name="ownerWindow">The owner window.</param>
    /// <param name="config">The configuration.</param>
    /// <param name="wizardDialogSettings">The wizard dialog settings.</param>
    public WizardController(Window ownerWindow, ApplicationConfiguration config, WindowSettings wizardDialogSettings)
    {
      this.ownerWindow = ownerWindow;
      this.config = config;
      this.wizardDialogSettings = wizardDialogSettings;
      this.GroupingSettings = this.InitializeGrouppingSettings();
    }

    /// <summary>
    /// Occurs when property was changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Gets the projects tree.
    /// </summary>
    public ProjectViewModelList Projects => this.projects ?? (this.projects = new ProjectViewModelList());

    /// <summary>
    /// Gets the projects tree.
    /// </summary>
    public IReadOnlyList<GrouppingSetting> GroupingSettings
    {
      get;
    }

    /// <summary>
    /// Gets or sets the selected grouping setting.
    /// </summary>
    public GrouppingSetting SelectedGroupingSetting
    {
      get => this.selectedGroupingSetting;
      set
      {
        this.selectedGroupingSetting = value;
        this.OnPropertyChanged(nameof(this.SelectedGroupingSetting));
      }
    }

    /// <summary>
    /// Gets a value indicating whether wizard is NOT on it's first step.
    /// </summary>
    public bool IsNotFirstStep => this.currentPage != null && !(this.currentPage is ConnectionWizardPage);

    /// <summary>
    /// Gets a value indicating whether wizard is on it's last step.
    /// </summary>
    public bool IsLastStep => this.currentPage is ProjectSelectionWizardPage;

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
    ///   <c>true</c> if the proposed address is empty; otherwise, <c>false</c>.
    /// </value>
    public bool IsProposedAddressEmpty
    {
      get => this.isProposedAddressEmpty;
      private set
      {
        this.isProposedAddressEmpty = value;
        this.OnPropertyChanged(nameof(this.IsNextStepEnabled));
      }
    }

    /// <summary>
    /// Indicates if auto detection is enabled, when set also updates <seealso cref="IsAutoDetectionDisabled"/>
    /// trough public setter so the OnPropertyChanged on both are called and view is notified.
    /// </summary>
    public bool IsAutoDetectionEnabled
    {
      get => this.isAutoDetectionEnabled;
      set
      {
        this.isAutoDetectionEnabled = value;
        this.IsAutoDetectionDisabled = !value;
        this.OnPropertyChanged(nameof(this.isAutoDetectionEnabled));
      }
    }

    /// <summary>
    /// Indicates if auto detection is disabled. When set also changes <seealso cref="isAutoDetectionDisabled"/>
    /// through private setter.
    /// </summary>
    public bool IsAutoDetectionDisabled
    {
      get => this.isAutoDetectionDisabled;
      set
      {
        this.isAutoDetectionDisabled = value;
        this.isAutoDetectionEnabled = !value;
        this.OnPropertyChanged(nameof(this.isAutoDetectionDisabled));
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether multi selection mode is active.
    /// </summary>
    public bool MultiSelectionMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the proposed server address.
    /// </summary>
    public string ProposedServerAddress
    {
      get
      {
        if (!this.IsProposedAddressEmpty)
        {
          return new Uri(this.proposedServerAddress).AbsoluteUri;
        }

        return this.proposedServerAddress;
      }

      set
      {
        this.proposedServerAddress = value;
        this.IsProposedAddressEmpty = string.IsNullOrWhiteSpace(value);
        this.OnPropertyChanged(nameof(this.IsProposedAddressEmpty));
      }
    }

    public List<string> AvailableServers
    {
      get
      {
        if (this.isAutoDetectionEnabled)
        {
          return this.config.ConnectorsConfiguration.Where(x =>
          x.GetConfigurationByKey(Connector.ServerAddress) != null &&
          !string.IsNullOrEmpty(x.GetConfigurationByKey(Connector.ServerAddress).Value)).Select(x => new Uri(x.GetConfigurationByKey(Connector.ServerAddress).Value).AbsoluteUri).Distinct().ToList();
        }

        if (this.SelectedConnectorType == null)
        {
          return new List<string>();
        }

        return this.config.ConnectorsConfiguration.Where(x =>
          x.Type == this.SelectedConnectorType.Name &&
          x.GetConfigurationByKey(Connector.ServerAddress) != null &&
          !string.IsNullOrEmpty(x.GetConfigurationByKey(Connector.ServerAddress).Value)).Select(x => new Uri(x.GetConfigurationByKey(Connector.ServerAddress).Value).AbsoluteUri).Distinct().ToList();
      }
    }

    public List<ConnectorPlugin> AvailableConnectorTypes
    {
      get
      {
        return PluginManager.Instance.ConnectorPlugins.OrderByDescending(x => this.config.ConnectorsConfiguration.Count(y => y.Type == x.Name)).ToList();
      }
    }

    public ConnectorPlugin SelectedConnectorType
    {
      get
      {
        if (this.selectedConnectorType == null)
        {
          this.selectedConnectorType = this.AvailableConnectorTypes.FirstOrDefault();
        }

        return this.selectedConnectorType;
      }

      set
      {
        this.selectedConnectorType = value;
        this.OnPropertyChanged(nameof(this.SelectedConnectorType));
        this.OnPropertyChanged(nameof(this.AvailableServers));
      }
    }

    /// <summary>
    /// Gets a value indicating whether any project is checked.
    /// </summary>
    public bool IsAnyProjectChecked
    {
      get => this.isAnyProjectChecked;
      private set
      {
        this.isAnyProjectChecked = value;
        this.OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets a value indicating whether finish button should be enabled.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this the finish button should be enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsFinishEnabled
    {
      get => this.isFinishEnabled;
      private set
      {
        this.isFinishEnabled = value;
        this.OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets the wizard frame.
    /// </summary>
    private Frame WizardFrame => this.wizardWindow?.Frame;

    /// <summary>
    /// Starts the wizard.
    /// </summary>
    /// <param name="connector">The connector view model.</param>
    /// <returns>
    /// True if the wizard was finished correctly and not canceled in any way.
    /// </returns>
    public bool Start(ConnectorViewModel connector)
    {
      this.editedConnectorViewModel = connector;
      this.IsPreviousStepEnabled = false;
      return this.Start(false);
    }

    /// <summary>
    /// Starts the wizard and applies the results to given configuration.
    /// Multiple, new connectors might be created.
    /// </summary>
    /// <param name="applyConfig">if set to <c>true</c> [apply configuration].</param>
    /// <returns>
    /// True if the wizard was finished correctly and not canceled in any way.
    /// </returns>
    public bool Start(bool applyConfig = true)
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
        void OnActivated(object sender, EventArgs e)
        {
          this.GoToProjectSelectionStep();
          this.wizardWindow.Activated -= OnActivated;
        }

        this.wizardWindow.Activated += OnActivated;
      }

      this.wizardDialogSettings.Apply(this.wizardWindow);
      var wizardShowResult = this.wizardWindow.ShowDialog();
      this.wizardDialogSettings.Parse(this.wizardWindow);
      if (wizardShowResult == true)
      {
        if (applyConfig)
        {
          this.ApplyToConfiguration();
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
      foreach (var checkedProject in checkedProjects.Where(p => p.Projects.Count == 0))
      {
        var newProject = new Project(checkedProject.Address, checkedProject.Name, checkedProject.DirectAddress, checkedProject.FullName, checkedProject.Description, this.Projects.PlugIn, this.CreateParentProjectStructure(checkedProject));
        serverProjects.Add(newProject);
      }

      return serverProjects;
    }

    /// <summary>
    /// Goes to next page of the wizard.
    /// </summary>
    public void GoToNextPage()
    {
      if (this.WizardFrame.Content is ConnectionWizardPage)
      {
        this.GoToProjectSelectionStep();
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
      if (this.IsAnyProjectChecked)
      {
        this.wizardWindow.DialogResult = true;
        this.wizardWindow.Close();
      }
    }

    /// <summary>
    /// Called when property was changed.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      if (propertyName == nameof(this.isAutoDetectionEnabled))
      {
        if (this.isAutoDetectionEnabled)
        {
          this.selectedConnectorType = null;
        }
      }
    }

    /// <summary>
    /// Creates the group name from project tree.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="currentName">Name of the current.</param>
    /// <returns>Group name.</returns>
    private string CreateGroupNameFromProjectTree(Project project, string currentName = null)
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
    private void ApplyToConfiguration()
    {
      var selectedProjects = this.GetSelectedProjects();
      if (selectedProjects.Count < 1)
      {
        throw new InvalidOperationException("At least one selected project is required.");
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

        selectedProject.Plugin.Configure(selectedProject, newConnector, this.proposedServerAddress);
      }

      if (configurationViewModel.ConfigurationIsModified)
      {
        configurationViewModel.Connectors.ApplyToConfiguration(this.config);
        SerializationHelper.Instance.SaveConfiguration(this.config);
      }
    }

    /// <summary>
    /// Called when page was changed.
    /// </summary>
    private void OnPageChanged()
    {
      this.OnPropertyChanged(nameof(this.IsNotFirstStep));
      this.OnPropertyChanged(nameof(this.IsLastStep));
      this.OnPropertyChanged(nameof(this.IsNotLastStep));
      this.OnPropertyChanged(nameof(this.IsFinishEnabled));
      this.OnPropertyChanged(nameof(this.IsNextStepEnabled));
      this.OnPropertyChanged(nameof(this.IsPreviousStepEnabled));
    }

    /// <summary>
    /// Goes to connection step of the wizard.
    /// </summary>
    private void GoToConnectionStep()
    {
      this.currentPage = new ConnectionWizardPage(this);
      this.currentPage.DataContext = this;
      this.WizardFrame.Content = this.currentPage;
      if (this.editedConnectorViewModel != null)
      {
        this.SelectedConnectorType = this.editedConnectorViewModel.SourceConnectorPlugin;
      }

      this.OnPageChanged();
    }

    private void ProcessServerSubProjects(IList<Project> projects, ProjectViewModel projectViewModel)
    {
      foreach (var project in projects.OrderBy(x => x.Name))
      {
        var newProject = projectViewModel.AddProject(project);
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

          return x.Type == this.SelectedConnectorType.Name
          && new Uri(address).AbsoluteUri.Equals(this.ProposedServerAddress)
          && x.GetConfigurationByKey(Connector.ProjectName)?.Value == (!string.IsNullOrWhiteSpace(project.FullName) ? project.FullName : project.Name);
        }).ToList();

        newProject.AlreadyAdded = alreadyExists.Any();
        newProject.AddedProject = alreadyExists.Any() ? string.Join(" - ", alreadyExists.Select(x => $"{x.GetConfigurationByKey(Connector.Category).Value}/{x.Name}")) : null;

        this.ProcessServerSubProjects(project.Children, newProject);
      }
    }

    /// <summary>
    /// Prepares the projects list.
    /// </summary>
    /// <returns>The task.</returns>
    private async Task PrepareProjectsList()
    {
      Tuple<ConnectorPlugin, ProjectViewModelList> pluginToQueryWithModel;
      if (this.editedConnectorViewModel != null)
      {
        pluginToQueryWithModel = new Tuple<ConnectorPlugin, ProjectViewModelList>(this.editedConnectorViewModel.SourceConnectorPlugin, new ProjectViewModelList { MultiSelectionMode = this.MultiSelectionMode, PlugIn = this.editedConnectorViewModel.SourceConnectorPlugin });
      }
      else
      {
        var plugin = PluginManager.Instance.ConnectorPlugins.FirstOrDefault(x => x.Name.Equals(this.SelectedConnectorType.Name));
        pluginToQueryWithModel = new Tuple<ConnectorPlugin, ProjectViewModelList>(plugin, new ProjectViewModelList { MultiSelectionMode = this.MultiSelectionMode, PlugIn = plugin });
      }

      var taskList = new Dictionary<Task, ProjectViewModelList>();
      var timeoutTask = Task.Delay(QueryTimeout);
      taskList.Add(timeoutTask, null);
      var task = this.LoadProjectsFromPlugin(pluginToQueryWithModel);
      taskList.Add(task, pluginToQueryWithModel.Item2);

      while (taskList.Count > 0)
      {
        var completedTask = await Task.WhenAny(taskList.Keys.ToArray());
        if (completedTask == timeoutTask)
        {
          throw new Exception("Discovery of suitable plugin or server query timed out");
        }

        if (completedTask.Status == TaskStatus.RanToCompletion)
        {
          this.projects = taskList.First(tkv => tkv.Key == completedTask).Value;
          this.AttachToProjectsPropertyChanged();
          break;
        }

        log.Debug($"Projects discovery for a plugin task completed not successfully. Status:{completedTask.Status}; Exception: {completedTask.Exception}");
        taskList.Remove(completedTask);
      }

      if (taskList.Count == 0)
      {
        throw new Exception("Couldn't find suitable plugin or the address is invalid");
      }
    }

    /// <summary>
    /// Prepares the projects list. Uses all available plugins to try to get correct result from the server.
    /// </summary>
    /// <returns>The task.</returns>
    private async Task PrepareProjectsListWithTypeDetection()
    {
      Collection<Tuple<ConnectorPlugin, ProjectViewModelList>> pluginToQueryWithModels = new Collection<Tuple<ConnectorPlugin, ProjectViewModelList>>();
      if (this.editedConnectorViewModel != null)
      {
        pluginToQueryWithModels.Add(new Tuple<ConnectorPlugin, ProjectViewModelList>(this.editedConnectorViewModel.SourceConnectorPlugin, new ProjectViewModelList { MultiSelectionMode = this.MultiSelectionMode, PlugIn = this.editedConnectorViewModel.SourceConnectorPlugin }));
      }
      else
      {
        foreach (var plugin in PluginManager.Instance.ConnectorPlugins)
        {
          pluginToQueryWithModels.Add(new Tuple<ConnectorPlugin, ProjectViewModelList>(plugin, new ProjectViewModelList { MultiSelectionMode = this.MultiSelectionMode, PlugIn = plugin }));
        }
      }

      var taskList = new Dictionary<Task, ProjectViewModelList>();
      var timeoutTask = Task.Delay(QueryTimeout);
      taskList.Add(timeoutTask, null);
      foreach (var pluginToQueryWithModel in pluginToQueryWithModels)
      {
        var task = this.LoadProjectsFromPlugin(pluginToQueryWithModel);
        taskList.Add(task, pluginToQueryWithModel.Item2);
      }

      while (taskList.Count > 0)
      {
        try
        {
          var completedTask = await Task.WhenAny(taskList.Keys.ToArray());
          if (completedTask == timeoutTask)
          {
            throw new Exception("Discovery of suitable plugin or server query timed out");
          }

          if (completedTask.Status == TaskStatus.RanToCompletion)
          {
            this.projects = taskList.First(tkv => tkv.Key == completedTask).Value;
            this.AttachToProjectsPropertyChanged();
            break;
          }

          log.Debug($"Projects discovery for a plugin task completed not successfully. Status:{completedTask.Status}; Exception: {completedTask.Exception}");
          taskList.Remove(completedTask);
        }
        catch (Exception ex)
        {
        }
      }

      if (taskList.Count == 0)
      {
        throw new Exception("Couldn't find suitable plugin or the address is invalid");
      }
    }

    /// <summary>
    /// Loads the projects from plugin.
    /// </summary>
    /// <param name="listQueryingPlugin">The list querying plugin.</param>
    /// <returns>The task.</returns>
    private async Task LoadProjectsFromPlugin(Tuple<ConnectorPlugin, ProjectViewModelList> listQueryingPlugin)
    {
      try
      {
        var serverProjects = await listQueryingPlugin.Item1.GetProjects(this.ProposedServerAddress.Last() != '/' ? this.ProposedServerAddress += '/' : this.ProposedServerAddress);
        foreach (var serverProject in serverProjects.OrderBy(x => x.Name))
        {
          var newProject = listQueryingPlugin.Item2.AddProject(serverProject);
          newProject.Address = serverProject.Address;
          newProject.DirectAddress = serverProject.DirectAddress;
          this.ProcessServerSubProjects(serverProject.Children, newProject);
        }
      }
      catch (Exception ex)
      {
        throw ex;
      }

      return;
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
    private async void GoToProjectSelectionStep()
    {
      var error = false;
      var errorMessage = string.Empty;
      var waitControl = new WaitControl();
      var waitDailogTask = DialogHost.Show(waitControl, "WizardWaitDialogHostId");
      try
      {
        if (this.isAutoDetectionEnabled)
        {
          await this.PrepareProjectsListWithTypeDetection();
        }
        else
        {
          await this.PrepareProjectsList();
        }
      }
      catch (Exception e)
      {
        error = true;
        errorMessage = $"There was a project error,{Environment.NewLine}details: {e.Message}"; // TODO load from resources
      }

      DialogHost.CloseDialogCommand.Execute("WizardWaitDialogHostId", this.wizardWindow.WizardWaitDialogHost);

      if (!error)
      {
        this.currentPage = new ProjectSelectionWizardPage(this);
        this.currentPage.DataContext = this;
        this.WizardFrame.Content = this.currentPage;
        this.OnPageChanged();
      }
      else if (this.wizardWindow.IsVisible)
      {
        var errorDialog = new MessageControl(errorMessage);
        await DialogHost.Show(errorDialog, "WizardWaitDialogHostId");
        if (this.editedConnectorViewModel != null)
        {
          this.wizardWindow.Close();
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
      grouppingSettings.Add(new GrouppingSetting("Assign parent as group", WizardWindow.AssignGroupsForAddedProjects));
      grouppingSettings.Add(new GrouppingSetting("Add hierarchy to project name", WizardWindow.AddProjectPathToProjectName));
      grouppingSettings.Add(new GrouppingSetting("No automatic grouping", WizardWindow.DoNotAssignAnyGroups));
      return grouppingSettings;
    }

    /// <summary>
    /// Creates the parent project structure.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    /// <returns>Parent project.</returns>
    private Project CreateParentProjectStructure(ProjectViewModel viewModel)
    {
      if (viewModel.Parent == null)
      {
        return null;
      }

      var newParent = new Project(viewModel.Parent.Address, viewModel.Parent.Name, viewModel.Parent.DirectAddress, viewModel.Parent.FullName, viewModel.Parent.Description, this.Projects.PlugIn, this.CreateParentProjectStructure(viewModel.Parent));
      return newParent;
    }
  }
}