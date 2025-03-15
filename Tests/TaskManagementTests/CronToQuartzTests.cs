using Moneo.TaskManagement.Jobs;

namespace TaskManagementTests;

public class CronToQuartzTests
{
    [Theory]
    [InlineData("0 15 10 * * *", "0 15 10 * * ? *")]
    [InlineData("0 0 12 * * *", "0 0 12 * * ? *")]
    [InlineData("0 0 9,21 * * *", "0 0 9,21 * * ? *")]
    [InlineData("0 0 12 * * ?", "0 0 12 * * ?")]
    [InlineData("15 10 * * *", "0 15 10 * * ? *")]
    [InlineData("0 * 14 * * *", "0 * 14 * * ? *")]
    [InlineData("0 0-5 14 * * *", "0 0-5 14 * * ? *")]
    [InlineData("0 11 11 11 11 *", "0 11 11 11 11 * *")]
    [InlineData("0 15 10 ? * MON-FRI", "0 15 10 ? * MON-FRI")]
    [InlineData("0 15 10 ? * 6L 2002-2005", "0 15 10 ? * 6L 2002-2005")]
    public void ValidCronTabProducesCorrectQuartzExpression(string cronTab, string expectedQuartz)
    {
        // Act
        var actualQuartz = cronTab.GetQuartzCronExpression();

        // Assert
        Assert.Equal(expectedQuartz, actualQuartz);
    }
}