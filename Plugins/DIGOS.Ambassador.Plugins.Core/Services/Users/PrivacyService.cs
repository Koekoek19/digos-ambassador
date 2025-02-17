﻿//
//  PrivacyService.cs
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

using System.Net;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Discord;
using Discord.Net;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Zio;

namespace DIGOS.Ambassador.Plugins.Core.Services.Users
{
    /// <summary>
    /// Handles privacy-related logic.
    /// </summary>
    [PublicAPI]
    public sealed class PrivacyService
    {
        [NotNull]
        private readonly CoreDatabaseContext _database;

        [NotNull]
        private readonly UserFeedbackService _feedback;

        [NotNull]
        private readonly ContentService _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyService"/> class.
        /// </summary>
        /// <param name="database">The core database.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="content">The content service.</param>
        public PrivacyService
        (
            [NotNull] CoreDatabaseContext database,
            [NotNull] UserFeedbackService feedback,
            [NotNull] ContentService content
        )
        {
            _database = database;
            _feedback = feedback;
            _content = content;
        }

        /// <summary>
        /// Sends a consent request to the given DM channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>An execution result.</returns>
        [NotNull, ItemNotNull]
        public async Task<DetermineConditionResult> RequestConsentAsync([NotNull] IDMChannel channel)
        {
            try
            {
                var consentBuilder = _feedback.CreateEmbedBase(Color.Orange);
                consentBuilder.WithDescription
                (
                    "Hello there! This appears to be the first time you're using the bot (or you've not granted your " +
                    "consent for it to store potentially sensitive or identifiable data about you).\n" +
                    "\n" +
                    "In order to use Amby and her commands, you need to give her your consent to store various data " +
                    "about you. We need this consent in order to be compliant with data regulations in the European " +
                    "Union (and it'd be rude not to ask!).\n" +
                    "\n" +
                    "In short, if you use the bot, we're going to be storing " +
                    "stuff like your Discord ID, some messages, server IDs, etc. You can - and should! - read the " +
                    "full privacy policy before you agree to anything. It's not very long (3 pages) and shouldn't take " +
                    "more than five minutes to read through.\n" +
                    "\n" +
                    "Once you've read it, you can grant consent by running the `!privacy grant-consent` command over DM. If you " +
                    "don't want to consent to anything, just don't use the bot :smiley:"
                );

                await channel.SendMessageAsync(string.Empty, embed: consentBuilder.Build());

                await SendPrivacyPolicyAsync(channel);
            }
            catch (HttpException hex) when (hex.HttpCode == HttpStatusCode.Forbidden)
            {
                return DetermineConditionResult.FromError("Could not send the privacy message over DM.");
            }

            return DetermineConditionResult.FromSuccess();
        }

        /// <summary>
        /// Sends the privacy policy to the given channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>A task that must be awaited.</returns>
        [NotNull]
        public async Task SendPrivacyPolicyAsync([NotNull] IMessageChannel channel)
        {
            var result = _content.OpenLocalStream(UPath.Combine(UPath.Root, "Privacy", "PrivacyPolicy.pdf"));
            if (!result.IsSuccess)
            {
                var errorBuilder = _feedback.CreateEmbedBase(Color.Red);
                errorBuilder.WithDescription
                (
                    "Oops. Something went wrong, and I couldn't grab the privacy policy. Please report this to the " +
                    "developer, don't agree to anything, and read it online instead."
                );

                errorBuilder.AddField("Privacy Policy", _content.PrivacyPolicyUri);

                await channel.SendMessageAsync(string.Empty, embed: errorBuilder.Build());
            }

            using (var privacyPolicy = result.Entity)
            {
                await channel.SendFileAsync(privacyPolicy, "PrivacyPolicy.pdf");
            }
        }

        /// <summary>
        /// Determines whether or not the given user has granted consent to store user data.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        /// <returns>true if the user has granted consent; Otherwise, false.</returns>
        [Pure, NotNull]
        public async Task<bool> HasUserConsentedAsync([NotNull] IUser discordUser)
        {
            var userConsent = await _database.UserConsents.FirstOrDefaultAsync(uc => uc.DiscordID == (long)discordUser.Id);

            return !(userConsent is null) && userConsent.HasConsented;
        }

        /// <summary>
        /// Grants consent to store user data for a given user.
        /// </summary>
        /// <param name="discordUser">The user that has granted consent.</param>
        /// <returns>A task that must be awaited.</returns>
        [NotNull]
        public async Task GrantUserConsentAsync([NotNull] IUser discordUser)
        {
            var userConsent = await _database.UserConsents.FirstOrDefaultAsync(uc => uc.DiscordID == (long)discordUser.Id);

            if (userConsent is null)
            {
                userConsent = new UserConsent((long)discordUser.Id)
                {
                    HasConsented = true
                };

                _database.UserConsents.Add(userConsent);
            }
            else
            {
                userConsent.HasConsented = true;
            }

            await _database.SaveChangesAsync();
        }

        /// <summary>
        /// Revokes consent to store user data for a given user.
        /// </summary>
        /// <param name="discordUser">The user that has revoked consent.</param>
        /// <returns>A task that must be awaited.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> RevokeUserConsentAsync([NotNull] IUser discordUser)
        {
            var userConsent = await _database.UserConsents.FirstOrDefaultAsync
            (
                uc => uc.DiscordID == (long)discordUser.Id
            );

            if (userConsent is null)
            {
                return ModifyEntityResult.FromError("The user has not consented.");
            }

            userConsent.HasConsented = false;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
