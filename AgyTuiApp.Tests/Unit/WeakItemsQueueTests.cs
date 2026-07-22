using System;
using Xunit;
using AgyTui;

namespace AgyTui.Tests.Unit;

public class WeakItemsQueueTests
{
    [Fact]
    public void AddWeakItem_ThenGetWeakItems_ReturnsTheItem()
    {
        var topic = "language_test_" + Guid.NewGuid().ToString("N")[..6];
        var itemKey = "test_weak_item_" + Guid.NewGuid().ToString("N")[..6];

        StudySession.Record(topic, "language", "quiz", new StudyScore(0, 1, 0), [itemKey], 0, 1, "unit test", DateTime.Now);
        WeakItemsQueue.AddWeakItem(topic, itemKey);

        var items = WeakItemsQueue.GetWeakItems(topic);
        Assert.NotNull(items);
        Assert.Contains(items, i => i.ItemId == itemKey);
    }
}
