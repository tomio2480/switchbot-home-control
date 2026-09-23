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

    [Theory]
    [InlineData(DiscomfortLevel.Neutral, true)]
    [InlineData(DiscomfortLevel.Pleasant, true)]
    [InlineData(DiscomfortLevel.Cold, false)]
    [InlineData(DiscomfortLevel.Chilly, false)]
    [InlineData(DiscomfortLevel.NotHot, false)]
    [InlineData(DiscomfortLevel.SlightlyHot, false)]
    [InlineData(DiscomfortLevel.HotAndSweaty, false)]
    [InlineData(DiscomfortLevel.UnbearablyHot, false)]
    public void IsComfortable_OnlyForNeutralAndPleasant(DiscomfortLevel level, bool expected)
    {
        Assert.Equal(expected, level.IsComfortable());
    }
}
