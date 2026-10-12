using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace NovaModbus.Rtu.Collections;

/// <summary>Items that support selection state with bulk suppression.</summary>
public interface ISelectable
{
    bool IsSelected { get; set; }
    /// <summary>When true, the item should not raise PropertyChanged for IsSelected.</summary>
    bool SuppressSelectionNotification { get; set; }
}

/// <summary>
/// ObservableCollection that supports bulk operations with a single change notification.
/// </summary>
public sealed class BulkObservableCollection<T> : ObservableCollection<T> where T : class
{
    private bool _suppressNotification;

    /// <summary>Initializes an empty collection.</summary>
    public BulkObservableCollection() : base() { }

    /// <summary>Initializes a collection with the specified items.</summary>
    public BulkObservableCollection(IEnumerable<T> items) : base(items ?? []) { }

    /// <summary>Temporarily suppresses CollectionChanged and PropertyChanged events.</summary>
    public void SuppressNotifications() => _suppressNotification = true;

    /// <summary>Resumes notifications and fires a single Reset event.</summary>
    public void ResumeNotifications()
    {
        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
    }

    /// <summary>Adds multiple items with a single notification.</summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items is null) return;
        SuppressNotifications();
        try { foreach (var item in items) Items.Add(item); }
        finally { ResumeNotifications(); }
    }

    /// <summary>Removes multiple items with a single notification.</summary>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items is null) return;
        var doomed = new HashSet<T>(items);
        SuppressNotifications();
        try
        {
            for (int i = Items.Count - 1; i >= 0; i--)
                if (doomed.Contains(Items[i])) Items.RemoveAt(i);
        }
        finally { ResumeNotifications(); }
    }

    /// <summary>Replaces the entire collection with new items in one notification.</summary>
    public void ReplaceAll(IEnumerable<T> newItems)
    {
        SuppressNotifications();
        try
        {
            Items.Clear();
            if (newItems is not null) foreach (var item in newItems) Items.Add(item);
        }
        finally { ResumeNotifications(); }
    }

    /// <summary>Fires a single Reset without changing items (rebinds realized rows).</summary>
    public void Refresh() => ResumeNotifications();

    /// <summary>Sets IsSelected on all items that implement ISelectable (single Reset notification).</summary>
    public void SetAllSelected(bool selected)
    {
        SuppressNotifications();
        try
        {
            foreach (var item in Items)
            {
                if (item is ISelectable s)
                {
                    s.SuppressSelectionNotification = true;
                    try
                    {
                        s.IsSelected = selected;
                    }
                    finally
                    {
                        s.SuppressSelectionNotification = false;
                    }
                }
            }
        }
        finally { ResumeNotifications(); }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification) base.OnCollectionChanged(e);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppressNotification) base.OnPropertyChanged(e);
    }
}