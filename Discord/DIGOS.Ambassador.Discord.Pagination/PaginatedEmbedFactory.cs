//
//  PaginatedEmbedFactory.cs
//
//  Author:
//        Jarl Gullberg <jarl.gullberg@gmail.com>
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
using System.Linq;
using DIGOS.Ambassador.Discord.Feedback;
using Discord;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Discord.Pagination
{
    /// <summary>
    /// Factory class for creating paginated embeds from various sources.
    /// </summary>
    public static class PaginatedEmbedFactory
    {
        /// <summary>
        /// Creates a simple paginated list from a collection of items.
        /// </summary>
        /// <param name="feedbackService">The user feedback service.</param>
        /// <param name="sourceUser">The user who caused the interactive message to be created.</param>
        /// <param name="items">The items.</param>
        /// <param name="titleSelector">A function that selects the title for each field.</param>
        /// <param name="valueSelector">A function that selects the value for each field.</param>
        /// <param name="emptyCollectionDescription">The description to use when the collection is empty.</param>
        /// <param name="appearance">The appearance settings to use for the pager.</param>
        /// <typeparam name="TItem">The type of the items in the collection.</typeparam>
        /// <returns>The paginated embed.</returns>
        public static PaginatedEmbed SimpleFieldsFromCollection<TItem>
        (
            UserFeedbackService feedbackService,
            IUser sourceUser,
            IEnumerable<TItem> items,
            Func<TItem, string> titleSelector,
            Func<TItem, string> valueSelector,
            [NotNull] string emptyCollectionDescription = "There's nothing here.",
            [CanBeNull] PaginatedAppearanceOptions appearance = null
        )
        {
            appearance = appearance ?? PaginatedAppearanceOptions.Default;

            var enumeratedItems = items.ToList();
            var paginatedEmbed = new PaginatedEmbed(feedbackService, sourceUser) { Appearance = appearance };

            IEnumerable<EmbedBuilder> pages;
            if (!enumeratedItems.Any())
            {
                var eb = paginatedEmbed.Appearance.CreateEmbedBase().WithDescription(emptyCollectionDescription);
                pages = new[] { eb };
            }
            else
            {
                var fields = enumeratedItems.Select
                (
                    i =>
                        new EmbedFieldBuilder().WithName(titleSelector(i)).WithValue(valueSelector(i))
                );

                pages = PageFactory.FromFields(fields);
            }

            paginatedEmbed.WithPages(pages);

            return paginatedEmbed;
        }
    }
}
