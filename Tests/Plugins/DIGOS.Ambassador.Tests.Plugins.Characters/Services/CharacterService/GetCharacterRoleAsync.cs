//
//  GetCharacterRoleAsync.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public partial class CharacterServiceTests
    {
        public class GetCharacterRoleAsync : CharacterServiceTestBase
        {
            private readonly IGuild _discordGuild;
            private readonly IRole _discordRole;
            private readonly IRole _unregisteredDiscordRole;

            private CharacterRole _role;

            public GetCharacterRoleAsync()
            {
                _discordGuild = MockHelper.CreateDiscordGuild(0);
                _discordRole = MockHelper.CreateDiscordRole(1, _discordGuild);
                _unregisteredDiscordRole = MockHelper.CreateDiscordRole(2, _discordGuild);
            }

            public override async Task InitializeAsync()
            {
                var result = await this.Characters.CreateCharacterRoleAsync
                (
                    _discordRole,
                    RoleAccess.Open
                );

                _role = result.Entity;
            }

            [Fact]
            public async Task GetsCorrectRole()
            {
                var result = await this.Characters.GetCharacterRoleAsync(_discordRole);

                Assert.True(result.IsSuccess);
                Assert.Same(_role, result.Entity);
            }

            [Fact]
            public async Task ReturnsErrorIfRoleIsNotRegistered()
            {
                var result = await this.Characters.GetCharacterRoleAsync
                (
                    _unregisteredDiscordRole
                );

                Assert.False(result.IsSuccess);
            }
        }
    }
}
