using WIMS.Shared.Mapping;

namespace WIMS.Application.Tests.Shared;

public class SimpleMapperTests
{
    private readonly ISimpleMapper _mapper = new SimpleMapper();

    private sealed class Source
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Secret { get; set; } = string.Empty; // لا يوجد مقابل في الوجهة
    }

    private sealed class Destination
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Extra { get; set; } // لا يوجد مقابل في المصدر
    }

    [Fact]
    public void Map_CopiesMatchingPropertiesByName()
    {
        var source = new Source { Name = "أحمد", Age = 30, Secret = "x" };

        var result = _mapper.Map<Source, Destination>(source);

        Assert.Equal("أحمد", result.Name);
        Assert.Equal(30, result.Age);
        Assert.Null(result.Extra); // لم يُلمس لعدم وجود مقابل
    }

    [Fact]
    public void Map_IsCaseInsensitiveOnPropertyNames()
    {
        var source = new { name = "سارة", age = 25 };

        var result = _mapper.Map<Destination>(source);

        Assert.Equal("سارة", result.Name);
        Assert.Equal(25, result.Age);
    }

    [Fact]
    public void Map_IntoExistingInstance_OnlyOverwritesMatching()
    {
        var source = new Source { Name = "خالد", Age = 40 };
        var destination = new Destination { Extra = "يبقى كما هو" };

        _mapper.Map(source, destination);

        Assert.Equal("خالد", destination.Name);
        Assert.Equal("يبقى كما هو", destination.Extra);
    }

    [Fact]
    public void MapList_MapsAllElements()
    {
        var sources = new object[]
        {
            new Source { Name = "أ", Age = 1 },
            new Source { Name = "ب", Age = 2 },
        };

        var result = _mapper.MapList<Destination>(sources);

        Assert.Equal(2, result.Count);
        Assert.Equal("أ", result[0].Name);
        Assert.Equal(2, result[1].Age);
    }

    [Fact]
    public void Map_NullSource_Throws()
        => Assert.Throws<ArgumentNullException>(() => _mapper.Map<Destination>(null!));
}
