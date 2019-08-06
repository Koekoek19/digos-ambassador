//
//  CharactersPlugin.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Database.Abstractions.Extensions;
using DIGOS.Ambassador.Plugins.Abstractions;
using DIGOS.Ambassador.Plugins.Abstractions.Attributes;
using DIGOS.Ambassador.Plugins.Characters;
using DIGOS.Ambassador.Plugins.Characters.CommandModules;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.TypeReaders;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

[assembly: AmbassadorPlugin(typeof(CharactersPlugin))]

namespace DIGOS.Ambassador.Plugins.Characters
{
    /// <summary>
    /// Describes the character plugin.
    /// </summary>
    public class CharactersPlugin : PluginDescriptor
    {
        /// <inheritdoc />
        public override string Name => "Characters";

        /// <inheritdoc />
        public override string Description => "Provides user-managed character libraries.";

        /// <inheritdoc />
        public override Task<bool> RegisterServicesAsync(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddScoped<CharacterService>()
                .AddSchemaAwareDbContextPool<CharactersDatabaseContext>();

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public override async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            var commands = serviceProvider.GetRequiredService<CommandService>();

            commands.AddTypeReader<Character>(new CharacterTypeReader());

            await commands.AddModuleAsync<CharacterCommands>(serviceProvider);

            var characterService = serviceProvider.GetRequiredService<CharacterService>();
            characterService.DiscoverPronounProviders();

            return true;
        }
    }
}
