using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using NovaModbus.Rtu.Collections;

namespace NovaModbus.Client.Helpers;

/// <summary>
/// Attached mouse + keyboard multi-select for large virtualized DataGrids.
/// Writes directly to <see cref="ISelectable.IsSelected"/> on data items,
/// bypassing the built-in SelectedItems tracking loop and SelectionChanged routing.
/// Supports drag range, plain click (single), Ctrl+click (toggle),
/// Shift+click (range), Shift+arrows/PageUp/PageDown/Home/End (extend) and Escape (clear).
/// Requires SelectionMode="Single" (built-in selection is cleared immediately)
/// and items implementing <see cref="ISelectable"/>.
/// No external behavior package required (pure attached property).
/// </summary>
public static class DataGridDragSelect
{
    /// <summary>Enables rectangle drag selection on the attached DataGrid.</summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridDragSelect),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>Gets IsEnabled.</summary>
    public static bool GetIsEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsEnabledProperty);

    /// <summary>Sets IsEnabled.</summary>
    public static void SetIsEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsEnabledProperty, value);

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(DragState),
            typeof(DataGridDragSelect),
            new PropertyMetadata(null));

    private static readonly DependencyProperty AnchorProperty =
        DependencyProperty.RegisterAttached(
            "Anchor",
            typeof(SelectionAnchor),
            typeof(DataGridDragSelect),
            new PropertyMetadata(null));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            grid.PreviewMouseLeftButtonDown += OnMouseDown;
            grid.PreviewMouseMove += OnMouseMove;
            grid.PreviewMouseLeftButtonUp += OnMouseUp;
            grid.PreviewKeyDown += OnKeyDown;
            grid.SelectionChanged += OnSelectionChanged;
        }
        else
        {
            grid.PreviewMouseLeftButtonDown -= OnMouseDown;
            grid.PreviewMouseMove -= OnMouseMove;
            grid.PreviewMouseLeftButtonUp -= OnMouseUp;
            grid.PreviewKeyDown -= OnKeyDown;
            grid.SelectionChanged -= OnSelectionChanged;
            grid.ClearValue(StateProperty);
            grid.ClearValue(AnchorProperty);
        }
    }

    private sealed class SelectionAnchor
    {
        public int Anchor = -1;
        public int Focus = -1;
    }

    private sealed class DragState
    {
        public Point AnchorPoint;
        public int AnchorIndex = -1;
        public int CurrentIndex = -1;
        public int RangeStart = -1;
        public int RangeEnd = -1;
        public bool Active;
        public SelectionAdorner? Adorner;
    }

    private sealed class SelectionAdorner : Adorner
    {
        private static readonly SolidColorBrush Fill = CreateFrozen(new SolidColorBrush(Color.FromArgb(60, 51, 153, 255)));
        private static readonly SolidColorBrush Border = CreateFrozen(new SolidColorBrush(Color.FromRgb(51, 153, 255)));

        private static SolidColorBrush CreateFrozen(SolidColorBrush brush)
        {
            brush.Freeze();
            return brush;
        }

        private Rect _rect;

        public SelectionAdorner(UIElement adorned)
            : base(adorned)
        {
            IsHitTestVisible = false;
        }

        public void Update(Rect rect)
        {
            _rect = rect;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_rect.IsEmpty)
            {
                return;
            }

            drawingContext.DrawRectangle(Fill, new Pen(Border, 1), _rect);
        }
    }

    private static SelectionAnchor GetAnchor(DataGrid grid)
    {
        if (grid.GetValue(AnchorProperty) is not SelectionAnchor anchor)
        {
            anchor = new SelectionAnchor();
            grid.SetValue(AnchorProperty, anchor);
        }

        return anchor;
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Keep the built-in selection empty so only the IsSelected highlight shows.
        // Single-item clear is O(1); nested event fires once with no items and no-ops.
        if (sender is DataGrid grid && grid.SelectedItems.Count > 0)
        {
            grid.UnselectAll();
        }
    }

    private static void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        // Never mark handled: checkboxes and cell editing keep working.
        // Capture starts only after the drag threshold is exceeded (see OnMouseMove).
        Point pos = e.GetPosition(grid);
        int index = IndexFromPoint(grid, pos);
        grid.SetValue(StateProperty, new DragState
        {
            AnchorPoint = pos,
            AnchorIndex = index,
        });

        if (index < 0)
        {
            return;
        }

        SelectionAnchor sel = GetAnchor(grid);
        if (IsInside<CheckBox>(e.OriginalSource as DependencyObject))
        {
            // The checkbox toggles IsSelected itself; just move the anchor.
            sel.Anchor = index;
            sel.Focus = index;
            return;
        }

        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

        if (shift && IsValidIndex(grid, sel.Anchor))
        {
            if (!ctrl)
            {
                DeselectAllItems(grid);
            }

            SelectIndexRange(grid, sel.Anchor, index);
            sel.Focus = index;
        }
        else if (ctrl)
        {
            ToggleIndex(grid, index);
            sel.Anchor = index;
            sel.Focus = index;
        }
        else
        {
            DeselectAllItems(grid);
            SetSelected(grid, index, true);
            sel.Anchor = index;
            sel.Focus = index;
        }
    }

    private static void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (grid.GetValue(StateProperty) is not DragState state)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            EndDrag(grid, state);
            return;
        }

        Point pos = e.GetPosition(grid);
        if (!state.Active)
        {
            Vector delta = pos - state.AnchorPoint;
            if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (state.AnchorIndex < 0)
            {
                // Drag started outside rows (e.g. header): ignore.
                return;
            }

            state.Active = true;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            {
                DeselectAllItems(grid);
            }

            grid.CaptureMouse();
            AttachAdorner(grid, state);
        }

        AutoScroll(grid, pos);
        int index = IndexFromPoint(grid, pos);
        if (index < 0)
        {
            // Pointer left the row area: clamp to the nearest end.
            index = pos.Y < 0 ? 0 : grid.Items.Count - 1;
        }

        ApplyRange(grid, state, pos, index);
    }

    private static void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        if (grid.GetValue(StateProperty) is DragState state)
        {
            EndDrag(grid, state);
        }
    }

    private static void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid || grid.Items.Count == 0)
        {
            return;
        }

        // Never mark handled: built-in focus navigation and cell edit-cancel keep working.
        if (e.Key == Key.System)
        {
            return;
        }

        if (IsInside<TextBox>(e.OriginalSource as DependencyObject))
        {
            // Cell editing in progress: leave keys to the editor.
            return;
        }

        if (e.Key == Key.Escape)
        {
            DeselectAllItems(grid);
            return;
        }

        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        if (!shift)
        {
            // Plain navigation: resync the focus index after the built-in move.
            if (e.Key is Key.Down or Key.Up or Key.PageDown or Key.PageUp or Key.Home or Key.End)
            {
                ScheduleFocusSync(grid);
            }

            return;
        }

        int? step = e.Key switch
        {
            Key.Down => 1,
            Key.Up => -1,
            Key.PageDown => PageSize(grid),
            Key.PageUp => -PageSize(grid),
            Key.Home => int.MinValue,
            Key.End => int.MaxValue,
            _ => null,
        };

        if (step is null)
        {
            return;
        }

        SelectionAnchor sel = GetAnchor(grid);
        int count = grid.Items.Count;
        int anchor = IsValidIndex(grid, sel.Anchor) ? sel.Anchor : CurrentIndex(grid);
        if (!IsValidIndex(grid, anchor))
        {
            anchor = 0;
        }

        int from = IsValidIndex(grid, sel.Focus) ? sel.Focus : anchor;
        int target = step == int.MinValue ? 0
            : step == int.MaxValue ? count - 1
            : Math.Clamp(from + step.Value, 0, count - 1);

        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            DeselectAllItems(grid);
        }

        SelectIndexRange(grid, anchor, target);
        sel.Anchor = anchor;
        sel.Focus = target;

        // Reconcile with the built-in focus move (page steps may differ).
        ScheduleFocusSync(grid);
    }

    private static void ScheduleFocusSync(DataGrid grid)
    {
        grid.Dispatcher.BeginInvoke(
            () =>
            {
                SelectionAnchor sel = GetAnchor(grid);
                int current = CurrentIndex(grid);
                if (IsValidIndex(grid, current))
                {
                    sel.Focus = current;
                }
            },
            System.Windows.Threading.DispatcherPriority.Background);
    }

    private static int CurrentIndex(DataGrid grid) =>
        grid.CurrentItem is null ? -1 : grid.Items.IndexOf(grid.CurrentItem);

    private static bool IsValidIndex(DataGrid grid, int index) =>
        index >= 0 && index < grid.Items.Count;

    private static int PageSize(DataGrid grid)
    {
        ScrollViewer? scroller = FindScrollViewer(grid);
        if (scroller is not null && scroller.ViewportHeight > 1)
        {
            return Math.Max(1, (int)scroller.ViewportHeight);
        }

        return 20;
    }

    private static void EndDrag(DataGrid grid, DragState state)
    {
        if (state.Active && grid.IsMouseCaptured)
        {
            grid.ReleaseMouseCapture();
        }

        if (state.Active && IsValidIndex(grid, state.AnchorIndex))
        {
            SelectionAnchor sel = GetAnchor(grid);
            sel.Anchor = state.AnchorIndex;
            sel.Focus = state.CurrentIndex >= 0 ? state.CurrentIndex : state.AnchorIndex;
        }

        DetachAdorner(grid, state);
        grid.ClearValue(StateProperty);
    }

    private static void ApplyRange(DataGrid grid, DragState state, Point pos, int index)
    {
        if (index == state.CurrentIndex)
        {
            state.Adorner?.Update(new Rect(state.AnchorPoint, pos));
            return;
        }

        state.CurrentIndex = index;
        int lo = Math.Min(state.AnchorIndex, index);
        int hi = Math.Max(state.AnchorIndex, index);

        // Differential update: only rows entering/leaving the range are touched.
        // Setters skip unchanged values, so already-correct rows cost nothing.
        if (state.RangeStart >= 0)
        {
            for (int i = state.RangeStart; i <= state.RangeEnd; i++)
            {
                if (i < lo || i > hi)
                {
                    SetSelected(grid, i, false);
                }
            }
        }

        for (int i = lo; i <= hi; i++)
        {
            if (state.RangeStart < 0 || i < state.RangeStart || i > state.RangeEnd)
            {
                SetSelected(grid, i, true);
            }
        }

        state.RangeStart = lo;
        state.RangeEnd = hi;
        state.Adorner?.Update(new Rect(state.AnchorPoint, pos));
    }

    private static void SetSelected(DataGrid grid, int index, bool selected)
    {
        if (!IsValidIndex(grid, index))
        {
            return;
        }

        if (grid.Items[index] is ISelectable item)
        {
            item.IsSelected = selected;
        }
    }

    private static void ToggleIndex(DataGrid grid, int index)
    {
        if (!IsValidIndex(grid, index))
        {
            return;
        }

        if (grid.Items[index] is ISelectable item)
        {
            item.IsSelected = !item.IsSelected;
        }
    }

    private static void SelectIndexRange(DataGrid grid, int first, int second)
    {
        int lo = Math.Min(first, second);
        int hi = Math.Max(first, second);
        for (int i = lo; i <= hi; i++)
        {
            SetSelected(grid, i, true);
        }
    }

    private static void DeselectAllItems(DataGrid grid)
    {
        for (int i = 0; i < grid.Items.Count; i++)
        {
            if (grid.Items[i] is ISelectable item)
            {
                item.IsSelected = false;
            }
        }
    }

    private static int IndexFromPoint(DataGrid grid, Point pos)
    {
        if (pos.X < 0 || pos.Y < 0 || pos.X > grid.ActualWidth || pos.Y > grid.ActualHeight)
        {
            return -1;
        }

        DependencyObject? hit = grid.InputHitTest(pos) as DependencyObject;
        DataGridRow? row = FindAncestor<DataGridRow>(hit);
        return row is null ? -1 : grid.ItemContainerGenerator.IndexFromContainer(row);
    }

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        DependencyObject? current = source;
        while (current is not null && current is not T)
        {
            current = VisualTreeHelper.GetParent(current);
        }

        return current as T;
    }

    private static bool IsInside<T>(DependencyObject? source) where T : DependencyObject =>
        FindAncestor<T>(source) is not null;

    private static void AutoScroll(DataGrid grid, Point pos)
    {
        ScrollViewer? scroller = FindScrollViewer(grid);
        if (scroller is null)
        {
            return;
        }

        const double edge = 24;
        if (pos.Y < edge)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset - 3);
        }
        else if (pos.Y > grid.ActualHeight - edge)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset + 3);
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject parent)
    {
        if (parent is ScrollViewer viewer)
        {
            return viewer;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            ScrollViewer? found = FindScrollViewer(VisualTreeHelper.GetChild(parent, i));
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static void AttachAdorner(DataGrid grid, DragState state)
    {
        AdornerLayer? layer = AdornerLayer.GetAdornerLayer(grid);
        if (layer is null)
        {
            return;
        }

        state.Adorner = new SelectionAdorner(grid);
        layer.Add(state.Adorner);
    }

    private static void DetachAdorner(DataGrid grid, DragState state)
    {
        if (state.Adorner is null)
        {
            return;
        }

        AdornerLayer? layer = AdornerLayer.GetAdornerLayer(grid);
        layer?.Remove(state.Adorner);
        state.Adorner = null;
    }
}
