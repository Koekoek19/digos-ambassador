﻿//
//  UserServiceTestBase.cs
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
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Tests.Extensions;
using DIGOS.Ambassador.Tests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable SA1648

namespace DIGOS.Ambassador.Tests.Plugins.Core
{
    /// <summary>
    /// Serves as a test base for user service tests.
    /// </summary>
    public abstract class UserServiceTestBase : DatabaseProvidingTestBase, IAsyncLifetime
    {
        /// <summary>
        /// Gets the user service object.
        /// </summary>
        protected UserService Users { get; private set; }

        /// <summary>
        /// Gets the database.
        /// </summary>
        protected CoreDatabaseContext Database { get; private set; }

        /// <inheritdoc />
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<CoreDatabaseContext>(ConfigureOptions<CoreDatabaseContext>);
            serviceCollection.AddScoped<UserService>();
        }

        /// <inheritdoc />
        protected override void ConfigureServices(IServiceProvider serviceProvider)
        {
            this.Database = serviceProvider.GetRequiredService<CoreDatabaseContext>();
            this.Database.Database.Create();

            this.Users = serviceProvider.GetRequiredService<UserService>();
        }
    }
}
