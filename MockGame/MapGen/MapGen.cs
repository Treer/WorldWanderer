// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapGen.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MapGen
{
    /// <summary>
    /// Access via singleton MapGen.Context (MapGen.Init() must have been be called)
    /// </summary>
    public interface IMapGenContext {
        /// <exception cref="NullReferenceException">if MapGen.Init() hasn't been called</exception>
        public IServiceProvider ServiceProvider { get; }

        /// <exception cref="NullReferenceException">if MapGen.Init() hasn't been called</exception>
        public IConfiguration Configuration { get; }

        /// <summary>Gets the Dependency Injected log</summary>
        /// <exception cref="NullReferenceException">if MapGen.Init() hasn't been called</exception>
        public ILogger Log { get; }

        /// <exception cref="NullReferenceException">if MapGen.Init() hasn't been called</exception>
        public Variant GetAutoloadConstant(string name);

        // DatabaseContext would also go here
    }


    /// <summary>
    /// Global singleton which provides the ServiceProvider, logger etc.
    /// </summary>
    public class MapGen: IMapGenContext {
        private static MapGen instance = new MapGen();
        private Node? autoloadConstants;

        public static IMapGenContext Context => instance;

        /// <inheritdoc/>
        public IConfiguration Configuration { get; private set; }
        /// <inheritdoc/>
        public IServiceProvider ServiceProvider { get; private set; }
        /// <inheritdoc/>
        public ILogger Log { get; private set; }

        /// <inheritdoc/>
        public Variant GetAutoloadConstant(string name) {
            return autoloadConstants.Get(name); // null ref if Init() hasn't been called
        }

        /// <summary>
        /// This gets called by the TileManager.cs in the WorldWanderer Godot project (or by your game),
        /// but if you don't want to use it you don't have to.
        /// </summary>
        public static void Init(string[] commandLineArgs, Node nodeTreeRoot, IServiceCollection? services = null) {
            instance._init(commandLineArgs, nodeTreeRoot, services);
        }

        private void _init(string[] commandLineArgs, Node nodeTreeRoot, IServiceCollection? services) {

            autoloadConstants = nodeTreeRoot.GetNode<Node>("Constants");
            if (autoloadConstants == null) {
                throw new NullReferenceException("Node missing from Godot host: Autoload Constants node not found");
            }


            // todo: decide what to do with ServiceCollection_extensions.cs
            // prehaps refactor MapGenContext to take an existing IServiceCollection,
            // so the C# game and mapgen can use the same collection

            IServiceCollection serviceCollection = services ?? new ServiceCollection();
            AddServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider(); // Services get disposed with the service provider, so keeping it instead of using() {} it.

			// Add configuration
            var configBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(commandLineArgs);

            string configJsonPath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(configJsonPath)) { configBuilder.AddJsonFile(configJsonPath); }

            Configuration = configBuilder.Build();

            // If an ILogger service wasn't already provided, then AddServices() will have added one.
            Log = ServiceProvider.GetRequiredService<ILogger>();
        }

        public IServiceCollection AddServices(IServiceCollection services) {
            var collection = services
                /*.AddTransient<IOceanFloor, OceanFloor>()*/; // Can add services here if DI is your jam
				
            // Add a logger if none was already included
            collection.TryAddSingleton(Logger.GlobalLog);

            return collection;
        }
    }
}
