﻿//
//  AmbassadorClient.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Abstractions.Services;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Discord;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Plugins.Abstractions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using DIGOS.Ambassador.Plugins.Permissions.Permissions;
using DIGOS.Ambassador.Plugins.Permissions.Services.Permissions;
using DIGOS.Ambassador.Plugins.Services;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Services.Lua;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Behaviours;
using DIGOS.Ambassador.TypeReaders;
using DIGOS.Ambassador.Utility;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using log4net;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Permissions.PermissionTarget;

#pragma warning disable SA1118 // Parameter spans multiple lines, big strings

namespace DIGOS.Ambassador
{
    /// <summary>
    /// Main class for the bot itself. Handles high-level functionality.
    /// </summary>
    public class AmbassadorClient
    {
        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(AmbassadorClient));

        private readonly DiscordSocketClient _client;

        private readonly ContentService _content;

        private readonly CommandService _commands;

        private readonly BehaviourService _behaviours;

        private IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorClient"/> class.
        /// </summary>
        /// <param name="content">The content service.</param>
        public AmbassadorClient([NotNull] ContentService content)
        {
            _client = Type.GetType("Mono.Runtime") is null
                ? new DiscordSocketClient()
                : new DiscordSocketClient(new DiscordSocketConfig { WebSocketProvider = () => new WebSocketSharpProvider() });

            _client.Log += OnDiscordLogEvent;

            _commands = new CommandService();
            _commands.Log += OnDiscordLogEvent;

            _content = content;
            _commands = new CommandService();

            _behaviours = new BehaviourService();
        }

        /// <summary>
        /// Initializes the bot and its services.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            var pluginService = new PluginService();

            var serviceCollection = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(_client)
                .AddSingleton<BaseSocketClient>(_client)
                .AddSingleton(_behaviours)
                .AddSingleton(_content)
                .AddSingleton(_commands)
                .AddSingleton<RoleplayService>()
                .AddSingleton<DiscordService>()
                .AddSingleton<UserFeedbackService>()
                .AddSingleton<InteractivityService>()
                .AddSingleton<HelpService>()
                .AddSingleton<Random>()
                .AddSingleton(pluginService)
                .AddSingleton<SchemaAwareDbContextService>()
                .AddDbContext<AmbyDatabaseContext>(builder => AmbyDatabaseContext.ConfigureOptions(builder));

            var successfullyRegisteredPlugins = new List<IPluginDescriptor>();

            var availablePlugins = pluginService.LoadAvailablePlugins();
            foreach (var availablePlugin in availablePlugins)
            {
                if (!await availablePlugin.RegisterServicesAsync(serviceCollection))
                {
                    Log.Warn
                    (
                        $"The plugin \"{availablePlugin.Name}\" (v{availablePlugin.Version}) failed to " +
                        $"register its services. It will not be loaded."
                    );

                    continue;
                }

                successfullyRegisteredPlugins.Add(availablePlugin);
            }

            _services = serviceCollection.BuildServiceProvider();

            var transformationService = _services.GetRequiredService<TransformationService>();
            transformationService.WithDescriptionBuilder
            (
                ActivatorUtilities.CreateInstance<TransformationDescriptionBuilder>(_services)
            );

            var characterService = _services.GetRequiredService<CharacterService>();
            characterService.DiscoverPronounProviders();

            foreach (var successfullyRegisteredPlugin in successfullyRegisteredPlugins)
            {
                if (!await successfullyRegisteredPlugin.InitializeAsync(_services))
                {
                    Log.Warn
                    (
                        $"The plugin \"{successfullyRegisteredPlugin.Name}\"" +
                        $" (v{successfullyRegisteredPlugin.Version}) failed to initialize. It may not be functional."
                    );
                }
            }
        }

        /// <summary>
        /// Logs the ambassador into Discord.
        /// </summary>
        /// <returns>A task representing the login action.</returns>
        public async Task LoginAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _content.BotToken.Trim());
        }

        /// <summary>
        /// Starts the ambassador, allowing it to react to messages.
        /// </summary>
        /// <returns>A task representing the start action.</returns>
        public async Task StartAsync()
        {
            var db = _services.GetRequiredService<AmbyDatabaseContext>();
            if (!((RelationalDatabaseCreator)db.Database.GetService<IDatabaseCreator>()).Exists())
            {
                Log.Error("The database doesn't exist.");
                return;
            }

            _commands.AddTypeReader<IMessage>(new UncachedMessageTypeReader<IMessage>());
            _commands.AddTypeReader<Roleplay>(new RoleplayTypeReader());
            _commands.AddTypeReader<Colour>(new ColourTypeReader());

            _commands.AddEnumReader<UserClass>();
            _commands.AddEnumReader<KinkPreference>();
            _commands.AddEnumReader<Bodypart>();
            _commands.AddEnumReader<Pattern>();
            _commands.AddEnumReader<Permission>();
            _commands.AddEnumReader<PermissionTarget>();

            // Load modules and behaviours from the assembly this type was declared in
            var localAssembly = GetType().Assembly;
            await _commands.AddModulesAsync(localAssembly, _services);
            await _behaviours.AddBehavioursAsync(localAssembly, _services);

            await _client.StartAsync();
            await _behaviours.StartBehavioursAsync();
        }

        /// <summary>
        /// Logs the ambassador out of Discord.
        /// </summary>
        /// <returns>A task representing the login action.</returns>
        public async Task LogoutAsync()
        {
            await _client.LogoutAsync();
        }

        /// <summary>
        /// Stops the ambassador, releasing its Discord resources.
        /// </summary>
        /// <returns>A task representing the stop action.</returns>
        public async Task StopAsync()
        {
            await _behaviours.StopBehavioursAsync();

            await LogoutAsync();
            await _client.StopAsync();
        }

        /// <summary>
        /// Saves log events from Discord using the configured method in log4net.
        /// </summary>
        /// <param name="arg">The log message from Discord.</param>
        /// <returns>A completed task.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the log severity is not recognized.</exception>
        [NotNull]
        private static Task OnDiscordLogEvent(LogMessage arg)
        {
            var content = $"Discord log event: {arg.Message}";
            switch (arg.Severity)
            {
                case LogSeverity.Critical:
                {
                    Log.Fatal(content, arg.Exception);
                    break;
                }
                case LogSeverity.Error:
                {
                    Log.Error(content, arg.Exception);
                    break;
                }
                case LogSeverity.Warning:
                {
                    Log.Warn(content, arg.Exception);
                    break;
                }
                case LogSeverity.Verbose:
                case LogSeverity.Info:
                {
                    Log.Info(content, arg.Exception);
                    break;
                }
                case LogSeverity.Debug:
                {
                    Log.Debug(content, arg.Exception);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            return Task.CompletedTask;
        }
    }
}
