using EasyMapper.Logic;

namespace EasyMapper.Tests;

public class TestMapper
{
    public class Car{
        public int TopSpeed { get; set; }
        public string? Model { get; set; }
        public int Year { get; set; }
    }

    public class SUV: Car{
        public int OffRoadCapability { get; set; }
    }

    public class Pickup: Car{
        public int LoadCapacity { get; set; }
    }

    [Fact]
    public void TestPopulateEntityProperties()
    {
        var jeepCompass = new SUV
        {
            TopSpeed = 180,
            Model = "Compass",
            Year = 2025,
            OffRoadCapability = 5
        };

        Pickup fiatToro = Mapper.PopulateEntityProperties<Pickup, SUV>(jeepCompass);
        fiatToro.LoadCapacity = 1000;
        fiatToro.Model = "Fiat Toro";
        Assert.Equal(jeepCompass.TopSpeed, fiatToro.TopSpeed);
        Assert.Equal(jeepCompass.Year, fiatToro.Year);   
    }
}