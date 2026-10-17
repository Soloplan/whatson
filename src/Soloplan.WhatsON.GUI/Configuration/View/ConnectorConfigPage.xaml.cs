// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConnectorConfigPage.xaml.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Configuration.View
{
  using System.Linq;
  using System.Windows.Controls;
  using MaterialDesignThemes.Wpf;
  using Soloplan.WhatsON.GUI.Common.Localization;
  using Soloplan.WhatsON.GUI.Configuration.ViewModel;

  /// <summary>
  /// Interaction logic for ConnectorConfigPage.xaml.
  /// </summary>
  public partial class ConnectorConfigPage : Page
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorConfigPage" /> class.
    /// </summary>
    /// <param name="connector">The connector.</param>
    public ConnectorConfigPage(ConnectorViewModel? connector)
    {
      this.DataContext = connector;
      this.InitializeComponent();
      this.CreateConfigurationControls(connector);
    }

    /// <summary>
    /// Dynamically creates the configuration controls.
    /// </summary>
    /// <param name="connector">The connector view model.</param>
    private void CreateConfigurationControls(ConnectorViewModel? connector)
    {
      if (connector == null)
      {
        this.StackPanel.Children.Clear();
        return;
      }

      var connectorConfigAttributes = connector.GetConnectorConfigAttributes().OrderBy(s => s.Priority).ToList();

      // move 0 priority connectors to the end of the list
      var zeroPriorityConnectors = connectorConfigAttributes.Where(s => s.Priority == 0).ToList();
      connectorConfigAttributes.RemoveAll(s => s.Priority == 0);
      connectorConfigAttributes.AddRange(zeroPriorityConnectors);

      // create controls
      foreach (var configAttribute in connectorConfigAttributes)
      {
        var configItem = connector.GetConfigurationByKey(configAttribute.Key);
        var builder = ConfigControlBuilderFactory.Instance.GetControlBuilder(configAttribute.Type, configAttribute.Key);
        if (builder == null)
        {
          // TODO log error - no defined control builder for type configAttribute.Type
          continue;
        }

        var editor = builder.GetControl(configItem, configAttribute);
        var captionBinding = TextExtension.CreateBinding(() =>
        {
          ConfigResourcesHelper.ApplyConfigResources(configAttribute, (connector.SourceConnectorPlugin ?? throw new System.InvalidOperationException("No connector plugin is selected.")).ConnectorType);
          return configAttribute.Caption;
        });
        if (editor is TextBox or ComboBox)
        {
          editor.SetBinding(HintAssist.HintProperty, captionBinding);
        }
        else if (editor is Panel panel)
        {
          foreach (var label in panel.Children.OfType<Label>())
          {
            label.SetBinding(ContentControl.ContentProperty, captionBinding);
          }
        }

        this.StackPanel.Children.Add(editor);
      }
    }
  }
}
