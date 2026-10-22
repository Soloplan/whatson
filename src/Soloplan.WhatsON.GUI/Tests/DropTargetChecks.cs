// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DropTargetChecks.cs" company="Soloplan GmbH">
//   Copyright (c) Soloplan GmbH. All rights reserved.
//   Licensed under the MIT License. See License-file in the project root for license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Soloplan.WhatsON.GUI.Tests
{
  using System;
  using System.Collections;
  using System.IO;
  using System.Reflection;
  using System.Windows;
  using System.Windows.Controls;
  using System.Windows.Data;
  using System.Windows.Media;
  using System.Windows.Media.Imaging;
  using GongSolutions.Wpf.DragDrop;
  using NUnit.Framework;
  using Soloplan.WhatsON.GUI.Common.ConnectorTreeView;

  internal static class DropTargetChecks
  {
    internal static void Verify(MainWindow window, TreeView tree, TreeViewItem group, ConnectorTreeViewModel model, string previewName)
    {
      Assert.That(GongSolutions.Wpf.DragDrop.DragDrop.GetDropTargetAdornerBrush(tree), Is.SameAs(tree.FindResource("SettingsAccent")));
      var job = (TreeViewItem)group.ItemContainerGenerator.ContainerFromIndex(0);
      foreach (var target in new[] { group, job })
      {
        var info = new TestDropInfo(tree, target, group.Items[1]);
        model.DragOver(info);
        Assert.That(info.DropTargetAdorner, Is.EqualTo(typeof(RowDropTargetAdorner)));
        Assert.That(info.Effects, Is.EqualTo(System.Windows.DragDropEffects.Move));
        var adorner = new RowDropTargetAdorner(tree, info);
        try
        {
          var visual = new DrawingVisual();
          using (var context = visual.RenderOpen())
          {
            var render = typeof(RowDropTargetAdorner).GetMethod("OnRender", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new AssertionException("Missing adorner renderer.");
            render.Invoke(adorner, [context]);
          }

          var outline = (GeometryDrawing)visual.Drawing.Children[1];
          var row = (Border)target.Template.FindName("Row", target);
          var expected = row.TransformToAncestor(tree).TransformBounds(new Rect(row.RenderSize));
          expected.Inflate(-0.5, -0.5);
          Assert.That(outline.Geometry.Bounds, Is.EqualTo(expected), "Drop highlights must cover only the header row, excluding expanded jobs.");
          Assert.That(((RectangleGeometry)outline.Geometry).RadiusX, Is.EqualTo(7));
          Assert.That(outline.Pen.Thickness, Is.EqualTo(1));
          Assert.That(outline.Pen.Brush, Is.SameAs(row.FindResource("SettingsAccent")));

          window.UpdateLayout();
          var root = (FrameworkElement)window.Content;
          var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
          bitmap.Render(root);
          var encoder = new PngBitmapEncoder();
          encoder.Frames.Add(BitmapFrame.Create(bitmap));
          var preview = Path.Combine(TestContext.CurrentContext.TestDirectory, $"DropTarget-{previewName}-{(target == group ? "Group" : "Job")}.png");
          using (var stream = File.Create(preview))
          {
            encoder.Save(stream);
          }

          TestContext.AddTestAttachment(preview);
        }
        finally
        {
          adorner.Detach();
        }

        info.InsertPosition = RelativeInsertPosition.BeforeTargetItem;
        model.DragOver(info);
        Assert.That(info.DropTargetAdorner, Is.EqualTo(target == group ? typeof(RowDropTargetAdorner) : DropTargetAdorners.Insert));
      }

      var groupInfo = new TestDropInfo(tree, group, new ConnectorGroupViewModel());
      model.DragOver(groupInfo);
      Assert.That(groupInfo.DropTargetAdorner, Is.EqualTo(typeof(RowDropTargetAdorner)));
      foreach (var position in new[] { RelativeInsertPosition.BeforeTargetItem, RelativeInsertPosition.AfterTargetItem })
      {
        groupInfo.InsertPosition = position;
        model.DragOver(groupInfo);
        Assert.That(groupInfo.DropTargetAdorner, Is.EqualTo(DropTargetAdorners.Insert));
      }
    }

    private sealed class TestDropInfo : IDropInfo
    {
      public TestDropInfo(TreeView tree, TreeViewItem target, object data)
      {
        this.VisualTarget = tree;
        this.VisualTargetItem = target;
        this.TargetItem = target.Header;
        this.Data = data;
      }

      public object Data { get; set; }

      public IDragInfo? DragInfo => null;

      public Point DropPosition => default;

      public Type? DropTargetAdorner { get; set; }

      public Type? DropTargetHintAdorner { get; set; }

      public DropHintState DropTargetHintState { get; set; }

      public string DropHintText { get; set; } = string.Empty;

      public System.Windows.DragDropEffects Effects { get; set; }

      public int InsertIndex => 0;

      public int UnfilteredInsertIndex => 0;

      public IEnumerable? TargetCollection => null;

      public object TargetItem { get; }

      public CollectionViewGroup? TargetGroup => null;

      public ScrollViewer? TargetScrollViewer => null;

      public ScrollingMode TargetScrollingMode => ScrollingMode.Both;

      public UIElement VisualTarget { get; }

      public UIElement VisualTargetItem { get; }

      public Orientation VisualTargetOrientation => Orientation.Vertical;

      public FlowDirection VisualTargetFlowDirection => FlowDirection.LeftToRight;

      public string DestinationText { get; set; } = string.Empty;

      public string EffectText { get; set; } = string.Empty;

      public RelativeInsertPosition InsertPosition { get; set; } = RelativeInsertPosition.TargetItemCenter;

      public DragDropKeyStates KeyStates => default;

      public bool NotHandled { get; set; }

      public bool IsSameDragDropContextAsSource => true;

      public EventType EventType => EventType.Bubbled;

      public bool AcceptChildItem { get; set; }
    }
  }
}
