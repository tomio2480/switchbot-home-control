using SwitchBotHomeControl.Monitoring;

namespace SwitchBotHomeControl.Tests;

public class DiscomfortIndexTests
{
    // Expected values are taken from the quick-reference table in the reference article
    [Theory]
    [InlineData(25, 50, 71.8)]
    [InlineData(20, 50, 65.3)]
    [InlineData(28, 75, 79.0)]
    [InlineData(-5, 30, 36.5)]
    [InlineData(35, 100, 95.0)]
    public void Calculate_MatchesReferenceTable(double temperature, double humidity, double expected)
    {
        Assert.Equal(expected, DiscomfortIndex.Calculate(temperature, humidity));
    }

    [Theory]
    [InlineData(54.9, DiscomfortLevel.Cold)]
    [InlineData(55.0, DiscomfortLevel.Chilly)]
    [InlineData(59.9, DiscomfortLevel.Chilly)]
    [InlineData(60.0, DiscomfortLevel.Neutral)]
    [InlineData(64.9, DiscomfortLevel.Neutral)]
    [InlineData(65.0, DiscomfortLevel.Pleasant)]
    [InlineData(69.9, DiscomfortLevel.Pleasant)]
    [InlineData(70.0, DiscomfortLevel.NotHot)]
    [InlineData(74.9, DiscomfortLevel.NotHot)]
    [InlineData(75.0, DiscomfortLevel.SlightlyHot)]
    [InlineData(79.9, DiscomfortLevel.SlightlyHot)]
    [InlineData(80.0, DiscomfortLevel.HotAndSweaty)]
    [InlineData(84.9, DiscomfortLevel.HotAndSweaty)]
    [InlineData(85.0, DiscomfortLevel.UnbearablyHot)]
    public void Classify_UsesLowerBoundInclusiveRanges(double index, DiscomfortLevel expected)
    {
        Assert.Equal(expected, DiscomfortIndex.Classify(index));
    }

    [Theory]
    [InlineData(DiscomfortLevel.Cold, "寒い")]
    [InlineData(DiscomfortLevel.Chilly, "肌寒い")]
    [InlineData(DiscomfortLevel.Neutral, "何も感じない")]
    [InlineData(DiscomfortLevel.Pleasant, "快い")]
    [InlineData(DiscomfortLevel.NotHot, "暑くない")]
    [InlineData(DiscomfortLevel.SlightlyHot, "やや暑い")]
    [InlineData(DiscomfortLevel.HotAndSweaty, "暑くて汗が出る")]
    [InlineData(DiscomfortLevel.UnbearablyHot, "暑くてたまらない")]
    public void ToLabel_ReturnsJapaneseSensation(DiscomfortLevel level, string expected)
    {
        Assert.Equal(expected, level.ToLabel());
    }

    // Colors follow the sensation labels in the reference article
    [Theory]
    [InlineData(DiscomfortLevel.Cold, "#0673B6")]
    [InlineData(DiscomfortLevel.Chilly, "#298B97")]
    [InlineData(DiscomfortLevel.Neutral, "#5CA869")]
    [InlineData(DiscomfortLevel.Pleasant, "#8BC43F")]
    [InlineData(DiscomfortLevel.NotHot, "#9EA53B")]
    [InlineData(DiscomfortLevel.SlightlyHot, "#B37F34")]
    [InlineData(DiscomfortLevel.HotAndSweaty, "#CA562D")]
    [InlineData(DiscomfortLevel.UnbearablyHot, "#EA1F25")]
    public void ToColorHex_FollowsReferenceArticle(DiscomfortLevel level, string expected)
    {
        Assert.Equal(expected, level.ToColorHex());
    }

    [Fact]
    public void LowerBound_IsNullOnlyForTheLowestLevel()
    {
        Assert.Null(DiscomfortLevel.Cold.LowerBound());
    }

    [Fact]
    public void LowerBound_AgreesWithClassify()
    {
        foreach (var level in Enum.GetValues<DiscomfortLevel>().Where(l => l != DiscomfortLevel.Cold))
        {
            var lowerBound = level.LowerBound()!.Value;
            Assert.Equal(level, DiscomfortIndex.Classify(lowerBound));
            Assert.Equal(level - 1, DiscomfortIndex.Classify(lowerBound - 0.1));
        }
    }
}
