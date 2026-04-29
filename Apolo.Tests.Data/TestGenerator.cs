using Models;

namespace Apolo.Tests.Data
{
    public static class TestGenerator
    {
        // Service const
        public const string ServiceName1 = "Service / hour";
        public const string ServiceName2 = "Contract service";
        public const decimal ServicePrice1 = 10m;
        public const decimal ServicePrice2 = 20m;

        // Service constructors
        public static Service CreateService1() => new Service
        {
            Id = Guid.NewGuid(),
            Name = ServiceName1,
            Price = ServicePrice1,
            IsPricePerHour = true
        };
        public static Service CreateService2() => new Service
        {
            Id = Guid.NewGuid(),
            Name = ServiceName2,
            Price = ServicePrice2,
            IsPricePerHour = false
        };
        public static Service CreateServiceDuplicate1() => new Service
        {
            Id = Guid.NewGuid(),
            Name = ServiceName1.ToLower(),
            Price = ServicePrice1,
            IsPricePerHour = true
        };
        public static Service CreateTemporaryService(Guid id) => new Service
        {
            Id = id,
            Name = "Temporary",
            Price = 10,
        };
    }
}
