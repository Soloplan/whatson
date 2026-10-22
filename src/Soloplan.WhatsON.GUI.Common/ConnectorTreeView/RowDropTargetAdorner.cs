// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RowDropTargetAdorner.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Common.ConnectorTreeView
{
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Media;
  using GongSolutions.Wpf.DragDrop;

  public class RowDropTargetAdorner : DropTargetAdorner
  {
    public RowDropTargetAdorner(UIElement adornedElement, IDropInfo dropInfo)
      : base(adornedElement, dropInfo)
    {
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
      if (this.DropInfo.VisualTargetItem is not TreeViewItem item ||
          item.Template.FindName("Row", item) is not Border row)
      {
        return;
      }

      var bounds = row.TransformToAncestor(this.AdornedElement).TransformBounds(new Rect(row.RenderSize));
      bounds.Inflate(-0.5, -0.5);
      drawingContext.PushOpacity(0.35);
      drawingContext.DrawRoundedRectangle((Brush)row.FindResource("SettingsSelection"), null, bounds, 7, 7);
      drawingContext.Pop();
      drawingContext.DrawRoundedRectangle(null, new Pen((Brush)row.FindResource("SettingsAccent"), 1), bounds, 7, 7);
    }
  }
}
