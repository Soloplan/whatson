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
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;
  using Soloplan.WhatsON.Model;

  /// <summary>
  /// Controls the execution of a wizard which allows to create or edit a project connection.
  /// </summary>
  /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
  public class WizardController : INotifyPropertyChanged
  {
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
    /// The connector plugin.
    /// </summary>
    private ConnectorPlugin connectorPlugin;

    /// <summary>
    /// The proposed server address.
    /// </summary>
    private string proposedServerAddress;

    /// <summary>
    /// Is proposed address empty flag.
    /// </summary>
    private bool isProposedAddressEmpty = true;

    private string selectedConnectorType;

    /// <summary>
    /// Initializes a new instance of the <see cref="WizardController"/> class.
    /// </summary>
    /// <param name="ownerWindow">The owner window.</param>
    public WizardController(Window ownerWindow, ApplicationConfiguration config)
    {
      this.ownerWindow = ownerWindow;
      this.config = config;
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
    /// Gets a value indicating whether wizard is NOT on it's first step.
    /// </summary>
    public bool IsNotFirstStep => !(this.currentPage is ConnectionWizardPage);

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
    /// <value>
    ///   <c>true</c> if the next step button is enabled; otherwise, <c>false</c>.
    /// </value>
    public bool IsNextStepEnabled => this.IsNotLastStep && !this.IsProposedAddressEmpty;

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
    /// Gets or sets a value indicating whether multi selection mode is active.
    /// </summary>
    public bool MultiSelectionMode { get; set; }

    /// <summary>
    /// Gets or sets the proposed server address.
    /// </summary>
    public string ProposedServerAddress
    {
      get => this.proposedServerAddress;
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
        if (this.SelectedConnectorType == null)
        {
          return new List<string>();
        }

        return this.config.ConnectorsConfiguration.Where(x => x.Type == this.SelectedConnectorType).Select(x => x.GetConfigurationByKey(Connector.ServerAddress).Value).Distinct().ToList();
      }
    }

    public List<string> AvailableConnectorTypes
    {
      get
      {
        return PluginManager.Instance.ConnectorPlugins.Select(x => x.Name).ToList();
      }
    }

    public string SelectedConnectorType
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
    /// Gets the step description text block control.
    /// </summary>
    private TextBlock StepDescriptionTextBlock => this.wizardWindow.StepDescription;

    /// <summary>
    /// Starts the wizard.
    /// </summary>
    /// <param name="plugin">The connector plugin.</param>
    /// <returns>True if the wizard was finished correctly and not canceled in any way.</returns>
    public bool Start(ConnectorPlugin plugin)
    {
      this.connectorPlugin = plugin;
      return this.Start(false);
    }

    /// <summary>
    /// Starts the wizard and applies the results to given configuration.
    /// Multiple, new connectors might be created.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>
    /// True if the wizard was finished correctly and not canceled in any way.
    /// </returns>
    public bool Start(bool applyConfig = true)
    {
      this.wizardWindow = new WizardWindow(this);
      this.wizardWindow.Owner = this.ownerWindow;
      this.wizardWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
      this.GoToConnectionStep();
      if (this.wizardWindow.ShowDialog() == true)
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
        var newProject = new Project { Address = checkedProject.Address, Name = checkedProject.Name, FullName = checkedProject.FullName, Description = checkedProject.Description, Plugin = this.Projects.PlugIn };
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
    }

    /// <summary>
    /// Applies the results of the wizard to configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
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
        configurationViewModel.Connectors.Add(newConnector);

        selectedProject.Plugin.Configure(selectedProject, newConnector, this.ProposedServerAddress);
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
    }

    /// <summary>
    /// Goes to connection step of the wizard.
    /// </summary>
    private void GoToConnectionStep()
    {
      this.StepDescriptionTextBlock.Text = "Connection"; // TODO load from resources
      this.currentPage = new ConnectionWizardPage(this);
      this.currentPage.DataContext = this;
      this.WizardFrame.Content = this.currentPage;
      this.OnPageChanged();
    }

    private void ProcessServerSubProjects(IList<Project> projects, ProjectViewModel projectViewModel)
    {
      foreach (var project in projects.OrderBy(x => x.Name))
      {
        var newProject = projectViewModel.AddProject(project);
        newProject.Address = project.Address;

        var alreadyExists = this.config.ConnectorsConfiguration.Where(x => x.Type == this.SelectedConnectorType
                                                                         && x.GetConfigurationByKey(Connector.ServerAddress)?.Value == this.ProposedServerAddress
                                                                         && x.GetConfigurationByKey(Connector.ProjectName)?.Value == (!string.IsNullOrWhiteSpace(project.FullName) ? project.FullName : project.Name)).ToList();

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
      if (this.connectorPlugin != null)
      {
        pluginToQueryWithModel = new Tuple<ConnectorPlugin, ProjectViewModelList>(this.connectorPlugin, new ProjectViewModelList { MultiSelectionMode = false, PlugIn = this.connectorPlugin });
      }
      else
      {
        var plugin = PluginManager.Instance.ConnectorPlugins.FirstOrDefault(x => x.Name.Equals(this.SelectedConnectorType));
        pluginToQueryWithModel = new Tuple<ConnectorPlugin, ProjectViewModelList>(plugin, new ProjectViewModelList { MultiSelectionMode = true, PlugIn = plugin });
      }

      var taskList = new Dictionary<Task, ProjectViewModelList>();
      var timeoutTask = Task.Delay(25000);
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
    /// Loads the projects from plugin.
    /// </summary>
    /// <param name="listQueryingPlugin">The list querying plugin.</param>
    /// <returns>The task.</returns>
    private async Task LoadProjectsFromPlugin(Tuple<ConnectorPlugin, ProjectViewModelList> listQueryingPlugin)
    {
      var serverProjects = await listQueryingPlugin.Item1.GetProjects(this.ProposedServerAddress);
      foreach (var serverProject in serverProjects.OrderBy(x => x.Name))
      {
        var newProject = listQueryingPlugin.Item2.AddProject(serverProject);
        newProject.Address = serverProject.Address;
        this.ProcessServerSubProjects(serverProject.Children, newProject);
      }
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
      var waitDailogTask = DialogHost.Show(this.wizardWindow.WizardWaitDialogHost.DialogContent, "WizardWaitDialogHostId");
      try
      {
        await this.PrepareProjectsList();
      }
      catch (Exception e)
      {
        error = true;
        errorMessage = $"There was a connection error,{Environment.NewLine}details: {e.Message}"; // TODO load from resources
      }

      DialogHost.CloseDialogCommand.Execute("WizardWaitDialogHostId", this.wizardWindow.WizardWaitDialogHost);

      if (!error)
      {
        this.StepDescriptionTextBlock.Text = "Project selection"; // TODO load from resources
        this.currentPage = new ProjectSelectionWizardPage(this);
        this.currentPage.DataContext = this;
        this.WizardFrame.Content = this.currentPage;
        this.OnPageChanged();
      }
      else if (this.wizardWindow.IsVisible)
      {
        var errorDialog = new MessageControl(errorMessage);
        await DialogHost.Show(errorDialog, "WizardWaitDialogHostId");
      }
    }
  }
}