// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateEditSubjectDialog.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Config.View
{
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using Soloplan.WhatsON.GUI.Config.ViewModel;

  /// <summary>
  /// Interaction logic for UserControl1.xaml
  /// </summary>
  public partial class CreateEditSubjectDialog : UserControl
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEditSubjectDialog" /> class.
    /// </summary>
    /// <param name="selectedSubject">The selected subject.</param>
    /// <param name="isNew">if set to <c>true</c> the subject is new.</param>
    public CreateEditSubjectDialog(SubjectViewModel selectedSubject, bool isNew)
    {
      this.InitializeComponent();
      if (isNew && this.uxPluginType.Items.Count > 0)
      {
        this.uxPluginType.SelectedIndex = 0;
      }
      else
      {
        this.uxPluginType.SelectedItem = PluginsManager.Instance.GetPlugin(selectedSubject.SourceSubject);
      }

      this.uxPluginType.IsEnabled = isNew;
    }

    /// <summary>
    /// OK button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void OkButtonClick(object sender, RoutedEventArgs e)
    {
      foreach (var be in BindingOperations.GetSourceUpdatingBindings(this))
      {
        be.UpdateSource();
      }
    }

    /// <summary>
    /// Cancel button click.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void CancelButtonClick(object sender, RoutedEventArgs e)
    {
      foreach (var be in BindingOperations.GetSourceUpdatingBindings(this))
      {
        be.UpdateTarget();
      }
    }
  }
}
