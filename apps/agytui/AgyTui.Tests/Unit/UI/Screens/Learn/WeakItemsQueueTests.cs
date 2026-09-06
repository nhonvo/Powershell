using AgyTui.Infrastructure.Persistence.DbContext;
using AgyTui.Domain.LearnContext;

namespace AgyTui.Tests.Unit.Infrastructure.Services;

[Collection("Sequential")]
public class WeakItemsQueueTests
{
    [Fact]
    public void AddWeakItem_ThenGetWeakItems_ReturnsTheItem()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "agy_weak_items_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempDir, "learn", "stats"));

        try
        {
            LearnDataPaths.OverrideBaseDirectory = tempDir;

            var topic = "language_test_" + Guid.NewGuid().ToString("N")[..6];
            var itemKey = "test_weak_item_" + Guid.NewGuid().ToString("N")[..6];

            StudySession.Record(topic, "language", "quiz", new StudyScore(0, 1, 0), [itemKey], 0, 1, "unit test", DateTime.Now);
            WeakItemsQueue.AddWeakItem(topic, itemKey);

            var items = WeakItemsQueue.GetWeakItems(topic);
            Assert.NotNull(items);
            Assert.Contains(items, i => i.ItemId == itemKey);
        }
        finally
        {
            LearnDataPaths.OverrideBaseDirectory = null;
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}


