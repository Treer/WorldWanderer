// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using MapGen.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace MapGen
{
    public class MapGen
    {
        public static void AddServices(IServiceCollection services, IHostEnvironment environment, IConfigurationManager configuration) {
            var collection = services
                /*.AddTransient<IOceanFloor, OceanFloor>()*/; // Can add services here if DI is your jam

            // Add a logger if none was already included
            collection.TryAddSingleton(Logger.GlobalLog);
        }
    }
}
