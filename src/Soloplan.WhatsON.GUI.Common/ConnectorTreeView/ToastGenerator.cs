using NLog.Internal;
using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Soloplan.WhatsON.Model;
using System.IO;
using System.Windows.Controls;

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  public class ToastGenerator
  {
    public ToastGenerator()
    {

    }

    private ToastContent BuildEmptyToast()
    {
      var content = new Microsoft.Toolkit.Uwp.Notifications.ToastContent()
      {
        Visual = new Microsoft.Toolkit.Uwp.Notifications.ToastVisual()
        {
          BindingGeneric = new ToastBindingGeneric()
          {
            Children =
            {
              new AdaptiveText()
              {
                Text = "Empty Notification"
              },
            }
          }
        }
      };

      return content;
    }

    private string GetIconPath(StatusViewModel statusViewModel)
    {
      string result = string.Empty;
      string imagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
      if (statusViewModel.State == ObservationState.Success)
      {
        result = imagesPath + "\\check-circle.png";
      }
      else if (statusViewModel.State == ObservationState.Failure)
      {
        result = imagesPath + "\\close-circle.png";
      }
      else if (statusViewModel.State == ObservationState.Unstable)
      {
        result = imagesPath + "\\alert-circle.png";
      }
      else if (statusViewModel.State == ObservationState.Running)
      {
        result = imagesPath + "\\circle.png";
      }
      else if (statusViewModel.State == ObservationState.Unknown)
      {
        result = imagesPath + "\\minus-circle.png";
      }

      return result;
    }

    private ToastContent BuildToast(ConnectorViewModel connectorViewModel)
    {
      var content = new Microsoft.Toolkit.Uwp.Notifications.ToastContent()
      {
        Visual = new Microsoft.Toolkit.Uwp.Notifications.ToastVisual()
        {
          BindingGeneric = new ToastBindingGeneric()
          {
            Children =
            {
              new AdaptiveText()
              {
                Text = connectorViewModel.Name,
                HintStyle = AdaptiveTextStyle.Header,
                HintAlign = AdaptiveTextAlign.Left,
              },
              new AdaptiveText()
              {
                Text = "Status: " + connectorViewModel.CurrentStatus.State.ToString(),
                HintStyle = AdaptiveTextStyle.Body,
              },
              new AdaptiveGroup()
              {
                Children =
                {
                  new AdaptiveSubgroup()
                  {
                    Children =
                    {
                      new AdaptiveImage()
                      {
                        Source = this.GetIconPath(connectorViewModel.CurrentStatus),
                        HintAlign = AdaptiveImageAlign.Center,
                      },
                    },
                  },
                },
              },
            },
          },
        },
      };


      return content;
    }


    public ToastContent GenerateToastContent(ConnectorViewModel connectorViewModel = null)
    {
      ToastContent content;
      if (connectorViewModel == null)
      {
        content = this.BuildEmptyToast();
      }
      else
      {
        content = this.BuildToast(connectorViewModel);
      }
      return content;
    }
  }
}
