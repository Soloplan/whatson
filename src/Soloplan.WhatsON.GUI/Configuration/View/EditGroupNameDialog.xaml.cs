// <copyright file="EditGroupNameDialog.xaml.cs" company="Soloplan GmbH">
// Copyright (c) Soloplan GmbH. All rights reserved.
// Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>

namespace Soloplan.WhatsON.GUI.Configuration.View
{
  using System.Windows.Controls;
  using System.Windows.Data;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;

  /// <summary>
  /// Interaction logic for EditGroupNameDialog.xaml.
  /// </summary>
  public partial class EditGroupNameDialog : UserControl
  {
    public EditGroupNameDialog()
    {
      this.InitializeComponent();
      this.DataContext = new GroupViewModel();
    }

    public EditGroupNameDialog(GroupViewModel model)
    : this()
    {
      this.DataContext = model;
    }

    private void OkButtonClick(object sender, System.Windows.RoutedEventArgs e)
    {
      foreach (var be in BindingOperations.GetSourceUpdatingBindings(this))
      {
        be.UpdateSource();
      }

      if (string.IsNullOrEmpty(((GroupViewModel)this.DataContext).Error))
      {
        MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(true, this);
      }
    }

    private void CancelButtonClick(object sender, System.Windows.RoutedEventArgs e)
    {
      foreach (var be in BindingOperations.GetSourceUpdatingBindings(this))
      {
        be.UpdateTarget();
      }
    }
  }
}
