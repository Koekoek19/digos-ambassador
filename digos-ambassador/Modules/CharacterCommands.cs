﻿//
//  CharacterCommands.cs
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

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Pagination;
using DIGOS.Ambassador.Permissions;
using DIGOS.Ambassador.Permissions.Preconditions;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;
using static Discord.Commands.RunMode;
using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
	/// <summary>
	/// Commands for creating, editing, and interacting with user characters.
	/// </summary>
	[UsedImplicitly]
	[Alias("character", "char", "ch")]
	[Group("character")]
	[Summary
	(
		"Commands for creating, editing, and interacting with user characters.\n" +
		"\n" +
		"Parameters which take a character can be specified in two ways - by just the name, which will search your " +
		"characters, and by mention and name, which will search the given user's characters. For example,\n" +
		"\n" +
		"Your character: Amby  \n" +
		"Another user's character: @DIGOS Ambassador:Amby\n" +
		"\n" +
		"You can also substitute any character name for \"current\", and your active character will be used instead."
	)]
	public class CharacterCommands : InteractiveBase<SocketCommandContext>
	{
		private readonly DiscordService Discord;

		[ProvidesContext]
		private readonly GlobalInfoContext Database;
		private readonly ContentService Content;

		private readonly UserFeedbackService Feedback;

		private readonly CharacterService Characters;

		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterCommands"/> class.
		/// </summary>
		/// <param name="database">A database context from the context pool.</param>
		/// <param name="contentService">The content service.</param>
		/// <param name="discordService">The Discord integration service.</param>
		/// <param name="feedbackService">The feedback service.</param>
		/// <param name="characterService">The character service.</param>
		public CharacterCommands
		(
			GlobalInfoContext database,
			ContentService contentService,
			DiscordService discordService,
			UserFeedbackService feedbackService,
			CharacterService characterService
		)
		{
			this.Database = database;
			this.Content = contentService;
			this.Discord = discordService;
			this.Feedback = feedbackService;
			this.Characters = characterService;
		}

		/// <summary>
		/// Shows available pronoun families that can be used with characters.
		/// </summary>
		[UsedImplicitly]
		[Alias("available-pronouns", "pronouns")]
		[Command("available-pronouns")]
		[Summary("Shows available pronoun families that can be used with characters.")]
		public async Task ShowAvailablePronounFamiliesAsync()
		{
			var pronounProviders = this.Characters.GetAvailablePronounProviders();
			var eb = this.Feedback.CreateEmbedBase();
			eb.WithTitle("Available pronouns");

			eb.WithDescription
			(
				string.Join("\n", pronounProviders.Select(p => $"**{p.Family}**"))
			);

			await this.Feedback.SendEmbedAsync(this.Context, eb.Build());
		}

		/// <summary>
		/// Shows quick information about your current character.
		/// </summary>
		[UsedImplicitly]
		[Alias("show", "info")]
		[Command("show", RunMode = Async)]
		[Summary("Shows quick information about your current character.")]
		[RequireContext(Guild)]
		public async Task ShowCharacterAsync()
		{
			var retrieveCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, this.Context.User);
			if (!retrieveCurrentCharacterResult.IsSuccess)
			{
				await this.Feedback.SendErrorAsync(this.Context, retrieveCurrentCharacterResult.ErrorReason);
				return;
			}

			var character = retrieveCurrentCharacterResult.Entity;
			var eb = CreateCharacterInfoEmbed(character);

			await ShowCharacterAsync(character, eb);
		}

		/// <summary>
		/// Shows quick information about a character.
		/// </summary>
		/// <param name="character">The character.</param>
		[UsedImplicitly]
		[Alias("show", "info")]
		[Priority(1)]
		[Command("show", RunMode = Async)]
		[Summary("Shows quick information about a character.")]
		[RequireContext(Guild)]
		public async Task ShowCharacterAsync([NotNull] Character character)
		{
			var eb = CreateCharacterInfoEmbed(character);

			await ShowCharacterAsync(character, eb);
		}

		private async Task ShowCharacterAsync([NotNull] Character character, [NotNull] EmbedBuilder eb)
		{
			if (character.Description.Length + eb.Build().Length > 2000)
			{
				var userDMChannel = await this.Context.Message.Author.GetOrCreateDMChannelAsync();

				try
				{
					using (var ds = new MemoryStream(Encoding.UTF8.GetBytes(character.Description)))
					{
						await userDMChannel.SendMessageAsync(string.Empty, false, eb.Build());
						await userDMChannel.SendFileAsync(ds, $"{character.Name}_description.txt");
					}

					if (!this.Context.IsPrivate)
					{
						await this.Feedback.SendConfirmationAsync(this.Context, "Please check your private messages.");
					}
				}
				catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
				{
					await this.Feedback.SendWarningAsync
					(
						this.Context,
						"Your description is really long, and you don't accept DMs from non-friends on this server, so I'm unable to do that."
					);
				}
				finally
				{
					await userDMChannel.CloseAsync();
				}
			}
			else
			{
				eb.AddField("Description", character.Description);
				await this.Feedback.SendEmbedAsync(this.Context, eb.Build());
			}
		}

		[NotNull]
		private EmbedBuilder CreateCharacterInfoEmbed([NotNull] Character character)
		{
			var eb = this.Feedback.CreateEmbedBase();

			eb.WithAuthor(this.Context.Client.GetUser((ulong)character.Owner.DiscordID));

			var characterInfoTitle = character.Nickname.IsNullOrWhitespace()
				? character.Name
				: $"{character.Name} - \"{character.Nickname}\"";

			eb.WithTitle(characterInfoTitle);
			eb.WithDescription(character.Summary);

			eb.WithThumbnailUrl
			(
				!character.AvatarUrl.IsNullOrWhitespace()
					? character.AvatarUrl
					: this.Content.DefaultAvatarUri.ToString()
			);

			eb.AddField("Preferred pronouns", character.PronounProviderFamily);

			return eb;
		}

		/// <summary>
		/// Creates a new character.
		/// </summary>
		/// <param name="characterName">The name of the character.</param>
		/// <param name="characterNickname">The nickname that the user should assume when the character is active. Optional.</param>
		/// <param name="characterSummary">A summary of the character. Optional. Max 240 characters.</param>
		/// <param name="characterDescription">The full description of the character. Optional.</param>
		/// <param name="characterAvatarUrl">A url pointing to the character's avatar. Optional.</param>
		[UsedImplicitly]
		[Command("create", RunMode = Async)]
		[Summary("Creates a new character.")]
		[RequireContext(Guild)]
		[RequirePermission(Permission.CreateCharacter)]
		public async Task CreateCharacterAsync
		(
			[NotNull] string characterName,
			[CanBeNull] string characterNickname = null,
			[CanBeNull] string characterSummary = null,
			[CanBeNull] string characterDescription = null,
			[CanBeNull] string characterAvatarUrl = null
		)
		{
			characterAvatarUrl = characterAvatarUrl ?? this.Content.DefaultAvatarUri.ToString();

			var createCharacterResult = await this.Characters.CreateCharacterAsync
			(
				this.Database,
				this.Context,
				characterName,
				characterAvatarUrl,
				characterNickname,
				characterSummary,
				characterDescription
			);
			if (!createCharacterResult.IsSuccess)
			{
				await this.Feedback.SendErrorAsync(this.Context, createCharacterResult.ErrorReason);
				return;
			}

			await this.Feedback.SendConfirmationAsync(this.Context, $"Character \"{createCharacterResult.Entity.Name}\" created.");
		}

		/// <summary>
		/// Deletes the named character.
		/// </summary>
		/// <param name="character">The character to delete.</param>
		[UsedImplicitly]
		[Command("delete", RunMode = Async)]
		[Summary("Deletes the named character.")]
		[RequireContext(Guild)]
		[RequirePermission(Permission.DeleteCharacter)]
		public async Task DeleteCharacterAsync
		(
			[NotNull]
			[RequireEntityOwnerOrPermission(Permission.DeleteCharacter, PermissionTarget.Other)]
			Character character
		)
		{
			this.Database.Attach(character);

			this.Database.Characters.Remove(character);
			await this.Database.SaveChangesAsync();

			await this.Feedback.SendConfirmationAsync(this.Context, $"Character \"{character.Name}\" deleted.");
		}

		/// <summary>
		/// Lists the characters owned by a given user.
		/// </summary>
		/// <param name="discordUser">The user whose characters should be listed. Optional.</param>
		[UsedImplicitly]
		[Alias("list-owned", "list", "owned")]
		[Command("list-owned", RunMode = Async)]
		[Summary("Lists the characters owned by a given user.")]
		[RequireContext(Guild)]
		public async Task ListOwnedCharactersAsync([CanBeNull] IUser discordUser = null)
		{
			discordUser = discordUser ?? this.Context.Message.Author;

			var eb = this.Feedback.CreateEmbedBase();
			eb.WithAuthor(discordUser);
			eb.WithTitle("Your characters");

			var characters = this.Characters.GetUserCharacters(this.Database, discordUser, this.Context.Guild);

			foreach (var character in characters)
			{
				eb.AddField(character.Name, character.Summary);
			}

			if (eb.Fields.Count <= 0)
			{
				eb.WithDescription("You don't have any characters.");
			}

			await this.Feedback.SendEmbedAsync(this.Context, eb.Build());
		}

		/// <summary>
		/// Sets the named character as the user's current character.
		/// </summary>
		/// <param name="character">The character to become.</param>
		[UsedImplicitly]
		[Alias("assume", "become", "transform", "active")]
		[Command("assume", RunMode = Async)]
		[Summary("Sets the named character as the user's current character.")]
		[RequireContext(Guild)]
		public async Task AssumeCharacterFormAsync
		(
			[NotNull]
			[RequireEntityOwnerOrPermission(Permission.AssumeCharacter, PermissionTarget.Other)]
			Character character
		)
		{
			this.Database.Attach(character);

			await this.Characters.MakeCharacterCurrentOnServerAsync(this.Database, this.Context, this.Context.Guild, character);

			if (!character.Nickname.IsNullOrWhitespace() && this.Context.Message.Author is IGuildUser guildUser)
			{
				var currentServer = await this.Database.GetOrRegisterServerAsync(this.Context.Guild);
				var modifyNickResult = await this.Discord.SetUserNicknameAsync(this.Context, guildUser, character.Nickname);
				if (!modifyNickResult.IsSuccess && !currentServer.SuppressPermissonWarnings)
				{
					await this.Feedback.SendWarningAsync(this.Context, modifyNickResult.ErrorReason);
				}
			}

			await this.Database.SaveChangesAsync();

			await this.Feedback.SendConfirmationAsync(this.Context, $"{this.Context.Message.Author.Username} shimmers and morphs into {character.Name}.");
		}

		/// <summary>
		/// Sets your default form to your current character.
		/// </summary>
		[UsedImplicitly]
		[Command("set-default", RunMode = Async)]
		[Summary("Sets your default form to your current character.")]
		[RequireContext(Guild)]
		public async Task SetDefaultCharacterAsync()
		{
			var result = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, this.Context.User);
			if (!result.IsSuccess)
			{
				await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
				return;
			}

			await SetDefaultCharacterAsync(result.Entity);
		}

		/// <summary>
		/// Sets your default form to the given character.
		/// </summary>
		/// <param name="character">The character to set as the default character.</param>
		[UsedImplicitly]
		[Command("set-default", RunMode = Async)]
		[Summary("Sets your default form to the given character.")]
		[RequireContext(Guild)]
		public async Task SetDefaultCharacterAsync
		(
			[NotNull]
			[RequireEntityOwner]
			Character character
		)
		{
			this.Database.Attach(character);

			var user = await this.Database.GetOrRegisterUserAsync(this.Context.User);

			var result = await this.Characters.SetDefaultCharacterForUserAsync(this.Database, this.Context, character, user);
			if (!result.IsSuccess)
			{
				await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
				return;
			}

			await this.Feedback.SendConfirmationAsync(this.Context, "Default character set.");
		}

		/// <summary>
		/// Clears any active characters from you, restoring your default form.
		/// </summary>
		[UsedImplicitly]
		[Alias("clear", "drop", "default")]
		[Command("clear", RunMode = Async)]
		[Summary("Clears any active characters from you, restoring your default form.")]
		[RequireContext(Guild)]
		public async Task ClearCharacterFormAsync()
		{
			var user = await this.Database.GetOrRegisterUserAsync(this.Context.Message.Author);

			await this.Characters.ClearCurrentCharacterOnServerAsync(this.Database, this.Context.Message.Author, this.Context.Guild);

			if (this.Context.Message.Author is IGuildUser guildUser)
			{
				var currentServer = await this.Database.GetOrRegisterServerAsync(this.Context.Guild);

				ModifyEntityResult modifyNickResult;
				if (!(user.DefaultCharacter is null) && !user.DefaultCharacter.Nickname.IsNullOrWhitespace())
				{
					modifyNickResult = await this.Discord.SetUserNicknameAsync(this.Context, guildUser, user.DefaultCharacter.Nickname);
				}
				else
				{
					modifyNickResult = await this.Discord.SetUserNicknameAsync(this.Context, guildUser, guildUser.Username);
				}

				if (!modifyNickResult.IsSuccess && !currentServer.SuppressPermissonWarnings)
				{
					await this.Feedback.SendWarningAsync(this.Context, modifyNickResult.ErrorReason);
				}
			}

			await this.Feedback.SendConfirmationAsync(this.Context, "Character cleared.");
		}

		/// <summary>
		/// View the images in a character's gallery.
		/// </summary>
		/// <param name="character">The character to view the gallery of.</param>
		[UsedImplicitly]
		[Alias("view-gallery", "gallery")]
		[Command("view-gallery")]
		[Summary("View the images in a character's gallery.")]
		[RequireContext(Guild)]
		public async Task ViewCharacterGalleryAsync([NotNull] Character character)
		{
			if (character.Images.Count <= 0)
			{
				await this.Feedback.SendErrorAsync(this.Context, "There are no images in that character's gallery.");
				return;
			}

			var gallery = new PaginatedGallery
			{
				Pages = character.Images,
				Color = Color.DarkPurple,
				Title = character.Name,
				Options =
				new PaginatedAppearanceOptions
				{
					FooterFormat = "Image {0}/{1}",
					InformationText = "Use the reactions to navigate the gallery."
				}
			};

			await this.Interactive.SendPrivatePaginatedMessageAsync(this.Context, this.Feedback, gallery);
		}

		/// <summary>
		/// Lists the images in a character's gallery.
		/// </summary>
		/// <param name="character">The character.</param>
		[UsedImplicitly]
		[Command("list-images")]
		[Summary("Lists the images in a character's gallery.")]
		[RequireContext(Guild)]
		public async Task ListImagesAsync([NotNull] Character character)
		{
			var eb = this.Feedback.CreateEmbedBase();
			eb.WithTitle("Images in character gallery");

			foreach (var image in character.Images)
			{
				eb.AddField(image.Name, image.Caption ?? "No caption set.");
			}

			if (eb.Fields.Count <= 0)
			{
				eb.WithDescription("There are no images in the character's gallery.");
			}

			await this.Context.Channel.SendMessageAsync(string.Empty, false, eb.Build());
		}

		/// <summary>
		/// Adds an attached image to a character's gallery.
		/// </summary>
		/// <param name="character">The character to add the image to.</param>
		/// <param name="imageName">The name of the image to add.</param>
		/// <param name="imageCaption">The caption of the image.</param>
		/// <param name="isNSFW">Whether or not the image is NSFW.</param>
		[UsedImplicitly]
		[Command("add-image", RunMode = Async)]
		[Summary("Adds an attached image to a character's gallery.")]
		[RequireContext(Guild)]
		[RequirePermission(Permission.EditCharacter)]
		public async Task AddImageAsync
		(
			[NotNull]
			[RequireEntityOwnerOrPermission(Permission.EditCharacter, PermissionTarget.Other)]
			Character character,
			[CanBeNull] string imageName = null,
			[CanBeNull] string imageCaption = null,
			bool isNSFW = false
		)
		{
			bool hasAtLeastOneAttachment = this.Context.Message.Attachments.Any();
			if (!hasAtLeastOneAttachment)
			{
				await this.Feedback.SendErrorAsync(this.Context, "You need to attach an image.");
				return;
			}

			// Check that it's an image
			var firstAttachment = this.Context.Message.Attachments.First();
			bool firstAttachmentIsImage = firstAttachment.Width.HasValue && firstAttachment.Height.HasValue;

			if (!firstAttachmentIsImage)
			{
				await this.Feedback.SendErrorAsync(this.Context, "You need to attach an image.");
				return;
			}
			string imageUrl = firstAttachment.Url;

			imageName = (imageName ?? Path.GetFileNameWithoutExtension(firstAttachment.Filename))
						?? firstAttachment.Url.GetHashCode().ToString();

			await AddImageAsync(character, imageName, imageUrl, imageCaption, isNSFW);
		}

		/// <summary>
		/// Adds a linked image to a character's gallery.
		/// </summary>
		/// <param name="character">The character to add the image to.</param>
		/// <param name="imageName">The name of the image to add.</param>
		/// <param name="imageUrl">The url to the image.</param>
		/// <param name="imageCaption">The caption of the image.</param>
		/// <param name="isNSFW">Whether or not the image is NSFW.</param>
		[UsedImplicitly]
		[Command("add-image", RunMode = Async)]
		[Summary("Adds a linked image to a character's gallery.")]
		[RequireContext(Guild)]
		[RequirePermission(Permission.EditCharacter)]
		public async Task AddImageAsync
		(
			[NotNull]
			[RequireEntityOwnerOrPermission(Permission.EditCharacter, PermissionTarget.Other)]
			Character character,
			[NotNull] string imageName,
			[NotNull] string imageUrl,
			[CanBeNull] string imageCaption = null,
			bool isNSFW = false
		)
		{
			this.Database.Attach(character);

			var addImageResult = await this.Characters.AddImageToCharacterAsync(this.Database, character, imageName, imageUrl, imageCaption, isNSFW);
			if (!addImageResult.IsSuccess)
			{
				await this.Feedback.SendErrorAsync(this.Context, addImageResult.ErrorReason);
				return;
			}

			await this.Feedback.SendConfirmationAsync(this.Context, $"Added \"{imageName}\" to {character.Name}'s gallery.");
		}

		/// <summary>
		/// Removes the named image from the given character.
		/// </summary>
		/// <param name="character">The character to remove the image from.</param>
		/// <param name="imageName">The name of the image to remove.</param>
		[UsedImplicitly]
		[Alias("remove-image", "delete-image")]
		[Command("remove-image", RunMode = Async)]
		[Summary("Removes an image from a character's gallery.")]
		[RequireContext(Guild)]
		[RequirePermission(Permission.EditCharacter)]
		public async Task RemoveImageAsync
		(
			[NotNull]
			[RequireEntityOwnerOrPermission(Permission.EditCharacter, PermissionTarget.Other)]
			Character character,
			[Remainder]
			[NotNull] string imageName
		)
		{
			this.Database.Attach(character);

			var removeImageResult = await this.Characters.RemoveImageFromCharacterAsync(this.Database, character, imageName);
			if (!removeImageResult.IsSuccess)
			{
				await this.Feedback.SendErrorAsync(this.Context, removeImageResult.ErrorReason);
				return;
			}

			await this.Feedback.SendConfirmationAsync(this.Context, "Image removed.");
		}

		/// <summary>
		/// Transfers ownership of the named character to another user.
		/// </summary>
		/// <param name="newOwner">The new owner of the character.</param>
		/// <param name="character">The character to transfer.</param>
		[UsedImplicitly]
		[Alias("transfer-ownership", "transfer")]
		[Command("transfer-ownership")]
		[Summary("Transfers ownership of the named character to another user.")]
		[RequireContext(Guild)]
		[RequirePermission(Permission.TransferCharacter)]
		public async Task TransferCharacterOwnershipAsync
		(
			[NotNull] IUser newOwner,
			[NotNull]
			[RequireEntityOwnerOrPermission(Permission.TransferCharacter, PermissionTarget.Other)]
			Character character
		)
		{
			this.Database.Attach(character);

			var transferResult = await this.Characters.TransferCharacterOwnershipAsync(this.Database, newOwner, character, this.Context.Guild);
			if (!transferResult.IsSuccess)
			{
				await this.Feedback.SendErrorAsync(this.Context, transferResult.ErrorReason);
				return;
			}

			await this.Feedback.SendConfirmationAsync(this.Context, "Character ownership transferred.");
		}

		/// <summary>
		/// Property setter commands for characters.
		/// </summary>
		[UsedImplicitly]
		[Group("set")]
		public class SetCommands : ModuleBase<SocketCommandContext>
		{
			private readonly GlobalInfoContext Database;
			private readonly DiscordService Discord;

			private readonly UserFeedbackService Feedback;

			private readonly CharacterService Characters;

			/// <summary>
			/// Initializes a new instance of the <see cref="SetCommands"/> class.
			/// </summary>
			/// <param name="database">A database context from the context pool.</param>
			/// <param name="discordService">The Discord integration service.</param>
			/// <param name="feedbackService">The feedback service.</param>
			/// <param name="characterService">The character service.</param>
			public SetCommands
			(
				GlobalInfoContext database,
				DiscordService discordService,
				UserFeedbackService feedbackService,
				CharacterService characterService
			)
			{
				this.Database = database;
				this.Discord = discordService;
				this.Feedback = feedbackService;
				this.Characters = characterService;
			}

			/// <summary>
			/// Sets the name of a character.
			/// </summary>
			/// <param name="character">The character.</param>
			/// <param name="newCharacterName">The new name of the character.</param>
			[UsedImplicitly]
			[Command("name", RunMode = Async)]
			[Summary("Sets the name of a character.")]
			[RequireContext(Guild)]
			[RequirePermission(Permission.EditCharacter)]
			public async Task SetCharacterNameAsync
			(
				[NotNull]
				[RequireEntityOwnerOrPermission(Permission.EditCharacter, PermissionTarget.Other)]
				Character character,
				[Remainder]
				[NotNull]
				string newCharacterName
			)
			{
				this.Database.Attach(character);

				var setNameResult = await this.Characters.SetCharacterNameAsync(this.Database, this.Context, character, newCharacterName);
				if (!setNameResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, setNameResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Character name set.");
			}

			/// <summary>
			/// Sets the avatar of a character. You can attach an image instead of passing a url as a parameter.
			/// </summary>
			/// <param name="character">The character.</param>
			/// <param name="newCharacterAvatarUrl">The url of the new avatar. Optional.</param>
			[UsedImplicitly]
			[Command("avatar", RunMode = Async)]
			[Summary("Sets the avatar of a character. You can attach an image instead of passing a url as a parameter.")]
			[RequireContext(Guild)]
			[RequirePermission(Permission.EditCharacter)]
			public async Task SetCharacterAvatarAsync
			(
				[NotNull]
				[RequireEntityOwnerOrPermission(Permission.EditCharacter, PermissionTarget.Other)]
				Character character,
				[CanBeNull] string newCharacterAvatarUrl = null
			)
			{
				this.Database.Attach(character);

				if (newCharacterAvatarUrl is null)
				{
					if (!this.Context.Message.Attachments.Any())
					{
						await this.Feedback.SendErrorAsync(this.Context, "You need to attach an image or provide a url.");
						return;
					}

					var newAvatar = this.Context.Message.Attachments.First();
					newCharacterAvatarUrl = newAvatar.Url;
				}

				character.AvatarUrl = newCharacterAvatarUrl;

				await this.Database.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context, "Character avatar set.");
			}

			/// <summary>
			/// Sets the nickname that the user should have when a character is active.
			/// </summary>
			/// <param name="character">The character.</param>
			/// <param name="newCharacterNickname">The new nickname of the character. Max 32 characters.</param>
			[UsedImplicitly]
			[Alias("nickname", "nick")]
			[Command("nickname", RunMode = Async)]
			[Summary("Sets the nickname that the user should have when the character is active.")]
			[RequireContext(Guild)]
			[RequirePermission(Permission.EditCharacter)]
			public async Task SetCharacterNicknameAsync
			(
				[NotNull]
				[RequireEntityOwnerOrPermission(Permission.EditCharacter, PermissionTarget.Other)]
				Character character,
				[Remainder]
				[NotNull]
				string newCharacterNickname
			)
			{
				this.Database.Attach(character);

				var setNickResult = await this.Characters.SetCharacterNicknameAsync(this.Database, character, newCharacterNickname);
				if (!setNickResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, setNickResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Character nickname set.");
			}

			/// <summary>
			/// Sets the summary of a character.
			/// </summary>
			/// <param name="character">The character.</param>
			/// <param name="newCharacterSummary">The new summary. Max 240 characters.</param>
			[UsedImplicitly]
			[Command("summary", RunMode = Async)]
			[Summary("Sets the summary of a character.")]
			[RequireContext(Guild)]
			[RequirePermission(Permission.EditCharacter)]
			public async Task SetCharacterSummaryAsync
			(
				[NotNull]
				[RequireEntityOwnerOrPermission(Permission.EditCharacter, PermissionTarget.Other)]
				Character character,
				[Remainder]
				[NotNull]
				string newCharacterSummary
			)
			{
				this.Database.Attach(character);

				var setSummaryResult = await this.Characters.SetCharacterSummaryAsync(this.Database, character, newCharacterSummary);
				if (!setSummaryResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, setSummaryResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Character summary set.");
			}

			/// <summary>
			/// Sets the description of a character. You can attach a plaintext document instead of passing the contents as a parameter.
			/// </summary>
			/// <param name="character">The character.</param>
			/// <param name="newCharacterDescription">The new description of the character. Optional.</param>
			[UsedImplicitly]
			[Alias("description", "desc")]
			[Command("description", RunMode = Async)]
			[Summary("Sets the description of a character. You can attach a plaintext document instead of passing the contents as a parameter.")]
			[RequireContext(Guild)]
			[RequirePermission(Permission.EditCharacter)]
			public async Task SetCharacterDescriptionAsync
			(
				[NotNull]
				[RequireEntityOwnerOrPermission(Permission.EditCharacter, PermissionTarget.Other)]
				Character character,
				[Remainder]
				[CanBeNull]
				string newCharacterDescription = null
			)
			{
				this.Database.Attach(character);

				if (newCharacterDescription is null)
				{
					if (!this.Context.Message.Attachments.Any())
					{
						await this.Feedback.SendErrorAsync(this.Context, "You need to attach a plaintext document or provide an in-message description.");
						return;
					}

					var newDescription = this.Context.Message.Attachments.First();
					var getAttachmentStreamResult = await this.Discord.GetAttachmentStreamAsync(newDescription);
					if (!getAttachmentStreamResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, getAttachmentStreamResult.ErrorReason);
						return;
					}

					using (var s = getAttachmentStreamResult.Entity)
					{
						using (var sr = new StreamReader(s))
						{
							newCharacterDescription = sr.ReadToEnd();
						}
					}
				}

				var setDescriptionResult = await this.Characters.SetCharacterDescriptionAsync(this.Database, character, newCharacterDescription);
				if (!setDescriptionResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, setDescriptionResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Character description set.");
			}

			/// <summary>
			/// Sets whether or not a character is NSFW.
			/// </summary>
			/// <param name="character">The character.</param>
			/// <param name="isNSFW">Whether or not the character is NSFW</param>
			[UsedImplicitly]
			[Command("nsfw", RunMode = Async)]
			[Summary("Sets whether or not a character is NSFW.")]
			[RequireContext(Guild)]
			[RequirePermission(Permission.EditCharacter)]
			public async Task SetCharacterIsNSFWAsync
			(
				[NotNull]
				[RequireEntityOwnerOrPermission(Permission.EditCharacter, PermissionTarget.Other)]
				Character character,
				bool isNSFW
			)
			{
				this.Database.Attach(character);

				await this.Characters.SetCharacterIsNSFWAsync(this.Database, character, isNSFW);

				await this.Feedback.SendConfirmationAsync(this.Context, $"Character set to {(isNSFW ? "NSFW" : "SFW")}.");
			}

			/// <summary>
			/// Sets the preferred pronoun for a character.
			/// </summary>
			/// <param name="character">The character.</param>
			/// <param name="pronounFamily">The pronoun family.</param>
			[UsedImplicitly]
			[Command("pronoun", RunMode = Async)]
			[Summary("Sets the preferred pronoun of a character.")]
			[RequireContext(Guild)]
			[RequirePermission(Permission.EditCharacter)]
			public async Task SetCharacterPronounAsync
			(
				[NotNull]
				[RequireEntityOwnerOrPermission(Permission.EditCharacter, PermissionTarget.Other)]
				Character character,
				[Remainder]
				[NotNull]
				string pronounFamily
			)
			{
				this.Database.Attach(character);

				var result = await this.Characters.SetCharacterPronounAsync(this.Database, character, pronounFamily);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Preferred pronoun set.");
			}
		}
	}
}
