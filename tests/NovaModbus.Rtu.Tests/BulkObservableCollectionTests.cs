using System.Collections.Specialized;
using System.ComponentModel;
using NovaModbus.Rtu.Collections;

namespace NovaModbus.Rtu.Tests;

[TestClass]
public sealed class BulkObservableCollectionTests
{
    private sealed class TestItem : ISelectable, INotifyPropertyChanged
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    if (!SuppressSelectionNotification)
                    {
                        SelectionChangedCount++;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                    }
                }
            }
        }

        public bool SuppressSelectionNotification { get; set; }

        public int SelectionChangedCount { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private static BulkObservableCollection<TestItem> Create(int count)
    {
        BulkObservableCollection<TestItem> collection = [];
        for (int i = 0; i < count; i++)
        {
            collection.Add(new TestItem());
        }

        return collection;
    }

    [TestMethod]
    public void SetAllSelectedSingleResetAndNoItemNotifications()
    {
        BulkObservableCollection<TestItem> collection = Create(1000);
        List<NotifyCollectionChangedAction> actions = [];
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        collection.SetAllSelected(true);

        Assert.IsTrue(collection.All(i => i.IsSelected));
        Assert.AreEqual(1, actions.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Reset, actions[0]);
        Assert.AreEqual(0, collection.Sum(i => i.SelectionChangedCount));
    }

    [TestMethod]
    public void SetAllSelectedDirectSetStillNotifies()
    {
        BulkObservableCollection<TestItem> collection = Create(4);

        collection.SetAllSelected(true);
        collection[0].IsSelected = false;

        Assert.IsFalse(collection[0].IsSelected);
        Assert.AreEqual(1, collection[0].SelectionChangedCount);
    }

    [TestMethod]
    public void BulkOpsSingleReset()
    {
        BulkObservableCollection<TestItem> collection = Create(10);
        List<NotifyCollectionChangedAction> actions = [];
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        collection.AddRange([new TestItem(), new TestItem()]);
        Assert.AreEqual(12, collection.Count);

        collection.RemoveRange(collection.Take(2));
        Assert.AreEqual(10, collection.Count);

        collection.ReplaceAll([new TestItem()]);
        Assert.AreEqual(1, collection.Count);

        Assert.AreEqual(3, actions.Count);
        Assert.IsTrue(actions.All(a => a == NotifyCollectionChangedAction.Reset));
    }

    [TestMethod]
    public void RefreshSingleResetWithoutChangingItems()
    {
        BulkObservableCollection<TestItem> collection = Create(5);
        List<NotifyCollectionChangedAction> actions = [];
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        collection.Refresh();

        Assert.AreEqual(5, collection.Count);
        Assert.AreEqual(1, actions.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Reset, actions[0]);
    }
}
