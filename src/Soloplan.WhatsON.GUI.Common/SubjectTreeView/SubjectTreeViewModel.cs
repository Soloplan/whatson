// <copyright file="SubjectTreeViewModel.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Common.SubjectTreeView
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Windows;
  using System.Windows.Input;
  using GongSolutions.Wpf.DragDrop;
  using NLog;
  using Soloplan.WhatsON.GUI.Common.VisualConfig;
  using Soloplan.WhatsON.Serialization;

  /// <summary>
  /// Top level viewmodel used to bind to <see cref="SubjectTreeView"/>.
  /// </summary>
  public class SubjectTreeViewModel : NotifyPropertyChanged, IHandleDoubleClick, IDropTarget
  {
    /// <summary>
    /// The logger.
    /// </summary>
    private static readonly Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.ToString());

    /// <summary>
    /// Backing field for <see cref="SubjectGroups"/>.
    /// </summary>
    private ObservableCollection<SubjectGroupViewModel> subjectGroups;

    /// <summary>
    /// Flag indicating that <see cref="ConfigurationChanged"/> event is triggered - used to ignore updates of model.
    /// </summary>
    private bool configurationChanging;

    /// <summary>
    /// Backing field for <see cref="ItemPadding"/>.
    /// </summary>
    private int itemPadding;

    public event EventHandler ConfigurationChanged;

    /// <summary>
    /// Gets observable collection of subject groups, the top level object in tree view binding.
    /// </summary>
    public ObservableCollection<SubjectGroupViewModel> SubjectGroups => this.subjectGroups ?? (this.subjectGroups = this.CreateSubjectGroupViewModelCollection());

    /// <summary>
    /// Gets first group from <see cref="SubjectGroups"/>, used for binding when there is just one group <see cref="OneGroup"/>.
    /// </summary>
    public SubjectGroupViewModel FirstGroup => this.SubjectGroups.FirstOrDefault();

    /// <summary>
    /// Gets a value indicating whether there is only one group.
    /// </summary>
    public bool OneGroup => this.SubjectGroups.Count == 1;

    /// <summary>
    /// Gets or sets the padding of tree view items.
    /// </summary>
    public int ItemPadding
    {
      get => this.itemPadding;
      set
      {
        if (this.itemPadding != value)
        {
          this.itemPadding = value;
          this.OnPropertyChanged();
        }
      }
    }

    /// <summary>Updates the current drag state.</summary>
    /// <param name="dropInfo">Information about the drag.</param>
    /// <remarks>
    /// To allow a drop at the current drag position, the <see cref="P:GongSolutions.Wpf.DragDrop.DropInfo.Effects" /> property on
    /// <paramref name="dropInfo" /> should be set to a value other than <see cref="F:System.Windows.DragDropEffects.None" />
    /// and <see cref="P:GongSolutions.Wpf.DragDrop.DropInfo.Data" /> should be set to a non-null value.
    /// </remarks>
    public void DragOver(IDropInfo dropInfo)
    {
      if (object.ReferenceEquals(dropInfo.TargetItem, dropInfo.Data))
      {
        return;
      }

      if (dropInfo.Data is SubjectViewModel)
      {
        if (dropInfo.TargetItem is SubjectGroupViewModel)
        {
          dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
          dropInfo.Effects = DragDropEffects.Move;
        }
        else if (dropInfo.TargetItem is SubjectViewModel)
        {
          dropInfo.Effects = DragDropEffects.Move;
          dropInfo.DropTargetAdorner = (dropInfo.InsertPosition & RelativeInsertPosition.TargetItemCenter) != 0 ? DropTargetAdorners.Highlight : DropTargetAdorners.Insert;
        }
      }
      else if (dropInfo.Data is SubjectGroupViewModel)
      {
        if (dropInfo.TargetItem is SubjectGroupViewModel)
        {
          dropInfo.Effects = DragDropEffects.Move;
          dropInfo.DropTargetAdorner = (dropInfo.InsertPosition & RelativeInsertPosition.TargetItemCenter) != 0 ? DropTargetAdorners.Highlight : DropTargetAdorners.Insert;
        }
      }
    }

    /// <summary>Performs a drop.</summary>
    /// <param name="dropInfo">Information about the drop.</param>
    public void Drop(IDropInfo dropInfo)
    {
      if (dropInfo.Effects != DragDropEffects.Move)
      {
        log.Warn("Unexpected drop operation. {data}", new { Effect = dropInfo.Effects, dropInfo.Data, Target = dropInfo.TargetItem });
        return;
      }

      if (dropInfo.Data is SubjectGroupViewModel drggedGroup)
      {
        this.DropGrup(dropInfo, drggedGroup);
      }
      else if (dropInfo.Data is SubjectViewModel draggedSubject)
      {
        this.DropSubject(dropInfo, draggedSubject);
      }

      this.OnConfigurationChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Initializes the model.
    /// </summary>
    /// <param name="scheduler">Scheduler used for observation.</param>
    /// <param name="configuration">Configuration.</param>
    /// <param name="initialSubjectState">List of currently observed subjects - provide start data for model.</param>
    public void Init(ObservationScheduler scheduler, ApplicationConfiguration configuration, IList<Subject> initialSubjectState)
    {
      log.Trace("Initializing {name}", nameof(SubjectTreeViewModel));
      this.Update(configuration);
      foreach (var subject in initialSubjectState)
      {
        log.Trace("Applying status for subject {@subject}", subject);
        this.SchedulerStatusQueried(this, subject);
      }

      scheduler.StatusQueried -= this.SchedulerStatusQueried;
      scheduler.StatusQueried += this.SchedulerStatusQueried;
    }

    /// <summary>
    /// Updates the model.
    /// </summary>
    /// <param name="configuration">Changed configuration.</param>
    public void Update(ApplicationConfiguration configuration)
    {
      if (this.configurationChanging)
      {
        return;
      }

      log.Debug("Initializing {name}", nameof(SubjectTreeViewModel));
      var grouping = this.ParseConfiguration(configuration).ToList();
      int index = 0;
      foreach (var group in grouping)
      {
        log.Debug("Applying settings for group {group}", group.Key);
        var subjectGroupViewModel = this.SubjectGroups.FirstOrDefault(grp => grp.GroupName == group.Key);
        if (subjectGroupViewModel == null)
        {
          log.Debug("{model} doesn't exist, creating...", nameof(SubjectGroupViewModel));
          subjectGroupViewModel = new SubjectGroupViewModel();
          this.SubjectGroups.Insert(index, subjectGroupViewModel);
        }
        else
        {
          var oldIndex = this.SubjectGroups.IndexOf(subjectGroupViewModel);
          if (oldIndex != index)
          {
            this.SubjectGroups.Move(oldIndex, index);
          }
        }

        index++;
        subjectGroupViewModel.Init(group);
      }

      var noLongerAvailable = this.SubjectGroups.Where(grp => grouping.All(group => group.Key != grp.GroupName)).ToList();
      foreach (var subjectGroupViewModel in noLongerAvailable)
      {
        log.Debug("Removing group no longer present in configuration: {subjectGroupViewModelName}", subjectGroupViewModel.GroupName);
        this.SubjectGroups.Remove(subjectGroupViewModel);
      }

      switch (configuration.ViewStyle)
      {
        case ViewStyle.Normal:
          this.ItemPadding = 8;
          break;
        case ViewStyle.Compact:
          this.ItemPadding = 4;
          break;
        case ViewStyle.Pcked:
          this.ItemPadding = 0;
          break;
      }
    }

    /// <summary>
    /// Writes model settings to <paramref name="configuration"/>.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public void WriteToConfiguration(ApplicationConfiguration configuration)
    {
      var subjectConfigurations = configuration.SubjectsConfiguration.ToList();
      configuration.SubjectsConfiguration.Clear();
      foreach (var subjectGroupViewModel in this.SubjectGroups)
      {
        foreach (var subjectViewModel in subjectGroupViewModel.SubjectViewModels)
        {
          var config = subjectConfigurations.FirstOrDefault(cfg => cfg.Identifier == subjectViewModel.Identifier);
          if (config != null)
          {
            config.GetConfigurationByKey(Subject.Category).Value = subjectGroupViewModel.GroupName;
          }

          configuration.SubjectsConfiguration.Add(config);
        }
      }
    }

    public void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
      foreach (var subjectGroupViewModel in this.SubjectGroups)
      {
        subjectGroupViewModel.OnDoubleClick(sender, e);
      }
    }

    /// <summary>
    /// Pares current expansion state of groups to <see cref="GroupExpansionSettings"/> list.
    /// </summary>
    /// <returns>List of <see cref="GroupExpansionSettings"/>.</returns>
    public IList<GroupExpansionSettings> GetGroupExpansionState()
    {
      return this.SubjectGroups.Select(group => new GroupExpansionSettings
      {
        GroupName = group.GroupName,
        Expanded = group.IsNodeExpanded,
      }).ToList();
    }

    /// <summary>
    /// Applies <see cref="GroupExpansionSettings"/> for groups in this model.
    /// </summary>
    /// <param name="groupExpansion">Group expansion settings.</param>
    public void ApplyGroupExpansionState(IList<GroupExpansionSettings> groupExpansion)
    {
      if (groupExpansion == null)
      {
        return;
      }

      foreach (var expansion in groupExpansion)
      {
        var targetGroup = this.SubjectGroups.FirstOrDefault(group => group.GroupName == expansion.GroupName);
        if (targetGroup != null)
        {
          targetGroup.IsNodeExpanded = expansion.Expanded;
        }

        log.Debug("Parsing expansion state for non-existing group {@GroupExpansion}", expansion);
      }
    }

    /// <summary>
    /// Calls <see cref="ConfigurationChanged"/> event.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">Event args.</param>
    protected void OnConfigurationChanged(object sender, EventArgs args)
    {
      try
      {
        this.configurationChanging = true;
        this.ConfigurationChanged?.Invoke(sender, args);
      }
      finally
      {
        this.configurationChanging = false;
      }
    }

    /// <summary>
    /// Moves object given by <paramref name="source"/> to <paramref name="target"/>
    /// </summary>
    /// <param name="source">Information about source location of moved object.</param>
    /// <param name="target">Information about desired location of moved object.</param>
    /// <param name="insertPosition">Additional information about where in relation to <paramref name="target"/> the object should be placed.</param>
    private static void MoveObject(MovedObjectLocation source, MovedObjectLocation target, RelativeInsertPosition insertPosition)
    {
      var insertPositionInternal = insertPosition;
      if ((insertPositionInternal & RelativeInsertPosition.TargetItemCenter) != 0)
      {
        insertPositionInternal = RelativeInsertPosition.AfterTargetItem;
      }

      var targetIndex = target.Index;
      if ((insertPositionInternal & RelativeInsertPosition.AfterTargetItem) != 0)
      {
        targetIndex = targetIndex + 1;
      }

      if (object.ReferenceEquals(target.List, source.List))
      {
        if (targetIndex > source.Index)
        {
          targetIndex = targetIndex - 1;
        }

        if (source.Index == targetIndex)
        {
          return;
        }
      }

      var obj = source.List[source.Index];
      source.List.RemoveAt(source.Index);
      target.List.Insert(targetIndex, obj);
    }

    private IEnumerable<IGrouping<string, SubjectConfiguration>> ParseConfiguration(ApplicationConfiguration configuration)
    {
      return configuration.SubjectsConfiguration.GroupBy(config => config.GetConfigurationByKey(Subject.Category)?.Value?.Trim() ?? string.Empty);
    }

    /// <summary>
    /// Handles dropping of <see cref="SubjectViewModel"/>.
    /// </summary>
    /// <param name="dropInfo">All drop information.</param>
    /// <param name="droppedSubject">The dropped subject.</param>
    private void DropSubject(IDropInfo dropInfo, SubjectViewModel droppedSubject)
    {
      var currentSubjectGroupModel = this.GetSubjectGroup(droppedSubject);
      if (dropInfo.TargetItem is SubjectGroupViewModel model)
      {
        if (object.ReferenceEquals(currentSubjectGroupModel.List, model.SubjectViewModels))
        {
          return;
        }

        MoveObject(currentSubjectGroupModel, new MovedObjectLocation(model.SubjectViewModels, model.SubjectViewModels.Count - 1), RelativeInsertPosition.AfterTargetItem);
      }

      if (dropInfo.TargetItem is SubjectViewModel targetModel)
      {
        var targetGroup = this.GetSubjectGroup(targetModel);
        MoveObject(currentSubjectGroupModel, targetGroup, dropInfo.InsertPosition);
      }
    }

    /// <summary>
    /// Handles dropping of <see cref="SubjectGroupViewModel"/>.
    /// </summary>
    /// <param name="dropInfo">All drop information.</param>.
    /// <param name="droppedGroup">The dropped group.</param>
    private void DropGrup(IDropInfo dropInfo, SubjectGroupViewModel droppedGroup)
    {
      if (dropInfo.TargetItem is SubjectGroupViewModel targetModel)
      {
        var index = this.SubjectGroups.IndexOf(droppedGroup);
        var targetIndex = this.SubjectGroups.IndexOf(targetModel);
        MoveObject(new MovedObjectLocation(this.SubjectGroups, index), new MovedObjectLocation(this.SubjectGroups, targetIndex), dropInfo.InsertPosition);
      }
    }

    /// <summary>
    /// Gets information about location in parent collection.
    /// </summary>
    /// <param name="subjectViewModel">The subject view model.</param>
    /// <returns>The information about location in target collection.</returns>
    private MovedObjectLocation GetSubjectGroup(SubjectViewModel subjectViewModel)
    {
      foreach (var subjectGroupViewModel in this.SubjectGroups)
      {
        var index = subjectGroupViewModel.SubjectViewModels.IndexOf(subjectViewModel);
        if (index < 0)
        {
          continue;
        }

        return new MovedObjectLocation(subjectGroupViewModel.SubjectViewModels, index);
      }

      return null;
    }

    private void SchedulerStatusQueried(object sender, Subject e)
    {
      foreach (var subjectGroupViewModel in this.SubjectGroups)
      {
        if (subjectGroupViewModel.Update(e))
        {
          return;
        }
      }

      log.Warn("No viewmodel found for subject {@Subject}", e);
    }

    private ObservableCollection<SubjectGroupViewModel> CreateSubjectGroupViewModelCollection()
    {
      var subject = new ObservableCollection<SubjectGroupViewModel>();
      return subject;
    }

    /// <summary>
    /// Helper class with information about where the moved object is or should be in <see cref="List"/>.
    /// </summary>
    private class MovedObjectLocation
    {
      public MovedObjectLocation(IList list, int index)
      {
        this.List = list;
        this.Index = index;
      }

      /// <summary>
      /// Gets the source/target list.
      /// </summary>
      public IList List { get; }

      /// <summary>
      /// Gets the actual/desired location of object.
      /// </summary>
      public int Index { get; }
    }
  }
}