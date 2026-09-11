using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MovieLogger.Service.Mapping;

namespace MovieLogger.Service.Tests.TestSupport
{
    public static class TestMapperFactory
    {
        public static IMapper Create()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MovieProfile>();
                cfg.AddProfile<GenreProfile>();
            }, NullLoggerFactory.Instance);

            return configuration.CreateMapper();
        }
    }
}
