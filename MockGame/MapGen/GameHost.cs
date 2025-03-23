// Copyright 2023 Treer (https://github.com/Treer)
// License: MIT, see LICENSE.txt for rights granted

using Godot;
using MapGen.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;


// I switched my project to using Microsoft's generic host pattern
// https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=appbuilder
//
// so this file is just a stand-in for my game's generic host, and doesn't properly implement
// some things like IHostEnvironment environment, IConfigurationManager.

namespace GameHosting
{

    /// <summary>
    /// A callback which allows services to be added to the ServiceCollection before it is built/finalized into the ServiceProvider
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="environment"></param>
    /// <param name="configuration"></param>
    public delegate void ConfigureServiceCallback(IServiceCollection serviceCollection, IHostEnvironment environment, IConfigurationManager configuration);


    /// <summary>
    /// Access via singleton MapGen.Context (MapGen.Init() must have been be called)
    /// </summary>
    public interface IMapGenContext {
        /// <exception cref="NullReferenceException">if MapGen.Init() hasn't been called</exception>
        public IServiceProvider Services { get; }

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
    public class GameHost: IMapGenContext {
        private static GameHost instance = new GameHost();
        private Node? autoloadConstants;

        public static IMapGenContext Context => instance;

        /// <inheritdoc/>
        public IConfiguration Configuration { get; private set; }
        /// <inheritdoc/>
        public IServiceProvider Services { get; private set; }
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
        public static void Init(string[] commandLineArgs, Node nodeTreeRoot, IEnumerable<ConfigureServiceCallback>? configureServices = null) {
            instance._init(commandLineArgs, nodeTreeRoot, configureServices);
        }

        private void _init(string[] commandLineArgs, Node nodeTreeRoot, IEnumerable<ConfigureServiceCallback>? configureServices = null) {

            autoloadConstants = nodeTreeRoot.GetNode<Node>("Constants");
            if (autoloadConstants == null) {
                throw new NullReferenceException("Node missing from Godot host: Autoload Constants node not found");
            }


            // todo: decide what to do with ServiceCollection_extensions.cs
            // prehaps refactor MapGenContext to take an existing IServiceCollection,
            // so the C# game and mapgen can use the same collection

            IServiceCollection serviceCollection = new ServiceCollection();
            foreach (var callback in configureServices ?? Enumerable.Empty<ConfigureServiceCallback>()) {
                // this stand-in file for my game's generic host doesn't have IHostEnvironment or IConfigurationManager.
                callback(serviceCollection, null, null);
            }
            Services = serviceCollection.BuildServiceProvider(); // Services get disposed with the service provider, so keeping it instead of using() {} it.

			// Add configuration
            var configBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(commandLineArgs);

            string configJsonPath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(configJsonPath)) { configBuilder.AddJsonFile(configJsonPath); }

            Configuration = configBuilder.Build();

            // If an ILogger service wasn't already provided, then AddServices() will have added one.
            Log = Services.GetRequiredService<ILogger>();
        }

        public static void Stop() {
            // stub. This is where you might call StopAsync() if you're using an IHost
        }
    }
}
