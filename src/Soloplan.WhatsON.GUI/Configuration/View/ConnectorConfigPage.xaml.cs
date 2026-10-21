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
  using Soloplan.WhatsON.Composition;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
  using Soloplan.WhatsON.GUI.Common.Localization;
  using ConnectorViewModel = Soloplan.WhatsON.GUI.Configuration.ViewModel.ConnectorViewModel;

  /// <summary>
  /// Interaction logic for ConnectorConfigPage.xaml.
  /// </summary>
  public partial class ConnectorConfigPage : Page
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectorConfigPage" /> class.
    /// </summary>
    /// <param name="connector">The connector.</param>
    public ConnectorConfigPage(ConnectorViewModel? connector, bool serverSettings = false)
    {
      this.DataContext = connector;
      this.InitializeComponent();
      this.Heading.SetBinding(TextBlock.TextProperty, TextExtension.CreateBinding(() => serverSettings
        ? Soloplan.WhatsON.Localization.Strings.ServerCredentials : Soloplan.WhatsON.Localization.Strings.ProjectSettings));
      this.CreateConfigurationControls(connector, serverSettings);
    }

    /// <summary>
    /// Dynamically creates the configuration controls.
    /// </summary>
    /// <param name="connector">The connector view model.</param>
    private void CreateConfigurationControls(ConnectorViewModel? connector, bool serverSettings)
    {
      if (connector == null)
      {
        this.StackPanel.Children.Clear();
        return;
      }

      var connectorConfigAttributes = connector.GetConnectorConfigAttributes()
        .Where(attribute => attribute.IsServerSetting == serverSettings).OrderBy(s => s.Priority).ToList();
      if (!serverSettings && connector.GetConnectorConfigAttributes().Any(attribute => attribute.IsServerSetting))
      {
        var hint = new TextBlock { TextWrapping = System.Windows.TextWrapping.Wrap, Margin = new System.Windows.Thickness(0, 0, 0, 16) };
        hint.SetBinding(TextBlock.TextProperty, TextExtension.CreateBinding(() => Soloplan.WhatsON.Localization.Strings.ServerCredentialsHint));
        this.StackPanel.Children.Add(hint);
      }

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
        var action = PluginManager.Instance.GetPresentationPlugin(connector.SourceConnectorPlugin?.Name ?? string.Empty)
          ?.CreateConfigurationAction(configAttribute.Key, connector);
        if (action != null)
        {
          this.StackPanel.Children.Add(action);
        }
      }
    }
  }
}
