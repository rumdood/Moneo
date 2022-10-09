using Moneo.Core;
using Newtonsoft.Json;

namespace Moneo.Tests;

public class FixedLengthListTests
{
    [Fact]
    public void CanCreate()
    {
        var fll = new FixedLengthList<DateTime>(4);
        Assert.Equal(4, fll.Capacity);
    }

    [Fact]
    public void CanCreateWithCollection()
    {
        var dates = new[] {DateTime.Now, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(-2)};
        var fll = new FixedLengthList<DateTime>(dates, 4);
        Assert.Equal(4, fll.Capacity);
    }

    [Fact]
    public void AddPushesValueDown()
    {
        var today = DateTime.Now;
        var dates = new[] {today.AddDays(-1), today.AddDays(-2), today.AddDays(-3)};
        var fll = new FixedLengthList<DateTime>(dates, 4);
        fll.Add(today);

        Assert.Collection(fll,
            item => Assert.Equal(today, item),
            item => Assert.Equal(today.AddDays(-1), item),
            item => Assert.Equal(today.AddDays(-2), item),
            item => Assert.Equal(today.AddDays(-3), item));

        var tomorrow = today.AddDays(1);
        fll.Add(tomorrow);
        
        Assert.Collection(fll,
            item => Assert.Equal(today.AddDays(1), item),
            item => Assert.Equal(today, item),
            item => Assert.Equal(today.AddDays(-1), item),
            item => Assert.Equal(today.AddDays(-2), item));
    }
    
    [Fact]
    public void AddRangePushesValueDown()
    {
        var today = DateTime.Now;
        var dates = new[] {today.AddDays(-1), today.AddDays(-2), today.AddDays(-3)};
        var fll = new FixedLengthList<DateTime>(dates, 4);
        fll.Add(today);

        var additionalDays = new[] {today.AddDays(2), today.AddDays(1)};
        fll.AddRange(additionalDays);
        
        Assert.Collection(fll,
            item => Assert.Equal(today.AddDays(2), item),
            item => Assert.Equal(today.AddDays(1), item),
            item => Assert.Equal(today, item),
            item => Assert.Equal(today.AddDays(-1), item));
    }

    [Fact]
    public void CanSerialize()
    {
        const string expectedJson = @"{""_collection"":[""2022-01-01T00:00:00"",""2021-01-01T00:00:00"",""2020-01-01T00:00:00"",""0001-01-01T00:00:00""],""capacity"":4}";
        var fll = new FixedLengthList<DateTime>(4)
        {
            new DateTime(2020, 1, 1),
            new DateTime(2021, 1, 1),
            new DateTime(2022, 1, 1)
        };

        var json = JsonConvert.SerializeObject(fll);
        Assert.Equal(expectedJson, json);
    }
    
    [Fact]
    public void CanDeserialize()
    {
        const string json = @"{""_collection"":[""2022-01-01T00:00:00"",""2021-01-01T00:00:00"",""2020-01-01T00:00:00"",""0001-01-01T00:00:00""],""capacity"":4}";
        var fll = JsonConvert.DeserializeObject<FixedLengthList<DateTime>>(json);
        
        Assert.Collection(fll,
            item => Assert.Equal(new DateTime(2022, 1, 1), item),
            item => Assert.Equal(new DateTime(2021, 1, 1), item),
                item => Assert.Equal(new DateTime(2020, 1, 1), item),
            item => Assert.Equal(default, item));
    }
}