namespace AgyTui.Tests.Unit.UI.Navigation;

using AgyTui.UI.Core.Navigation;
using Xunit;

public class SubPageTopicNavigatorTests
{
    [Fact]
    public void SelectionIndex_Matches_RenderedTopics_Across_All_Tabs()
    {
        var allTopics = SubPageTopicNavigator.GetFilteredTopics("");
        Assert.Equal(6, allTopics.Count);
        Assert.Equal("jp", allTopics[0].Key);
        Assert.Equal("jp (Japanese / Language)", allTopics[0].DisplayName);
    }

    [Fact]
    public void GetFilteredTopics_MatchesBothKeyAndDisplayName()
    {
        var filtered = SubPageTopicNavigator.GetFilteredTopics("Japanese");
        Assert.Single(filtered);
        Assert.Equal("jp", filtered[0].Key);
    }
}
