﻿//
//  TransformationDescriptionBuilder.cs
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Attributes;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations
{
    /// <summary>
    /// Service class for building user-visible descriptions of characters based on their appearances.
    /// </summary>
    public sealed class TransformationDescriptionBuilder
    {
        [NotNull]
        private readonly TransformationTextTokenizer _tokenizer;

        [NotNull] private readonly Regex _sentenceSpacingRegex = new Regex
        (
            "(?<=\\w)\\.(?=\\w)",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase
        );

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationDescriptionBuilder"/> class.
        /// </summary>
        /// <param name="services">The available services.</param>
        public TransformationDescriptionBuilder([NotNull] IServiceProvider services)
        {
            _tokenizer = new TransformationTextTokenizer(services);

            _tokenizer.DiscoverAvailableTokens();
        }

        /// <summary>
        /// Replaces tokens in a piece of text with their respective contents.
        /// </summary>
        /// <param name="text">The text to replace in.</param>
        /// <param name="appearance">The character and appearance for which the text should be valid.</param>
        /// <param name="component">The transformation that the text belongs to.</param>
        /// <returns>A string with no tokens in it.</returns>
        [NotNull]
        public string ReplaceTokensWithContent
        (
            [NotNull] string text,
            [NotNull] Appearance appearance,
            [CanBeNull] AppearanceComponent component
        )
        {
            var tokens = _tokenizer.GetTokens(text);
            var tokenContentMap = tokens.ToDictionary(token => token, token => token.GetText(appearance, component));

            var relativeOffset = 0;
            var sb = new StringBuilder(text);

            foreach (var (token, content) in tokenContentMap)
            {
                sb.Remove(token.Start + relativeOffset, token.Length);
                sb.Insert(token.Start + relativeOffset, content);

                relativeOffset += content.Length - token.Length;
            }

            var result = string.Join
            (
                ". ",
                sb.ToString().Split('.').Select(s => s.Trim().Transform(To.SentenceCase))
            ).Trim();

            return result;
        }

        /// <summary>
        /// Builds a complete visual description of the given character.
        /// </summary>
        /// <param name="appearance">The appearance to describe.</param>
        /// <returns>A visual description of the character.</returns>
        [Pure, NotNull]
        public string BuildVisualDescription([NotNull] Appearance appearance)
        {
            var sb = new StringBuilder();
            sb.Append(ReplaceTokensWithContent("{@target} is a {@sex} {@species}.", appearance, null));
            sb.AppendLine();
            sb.AppendLine();

            var partsToSkip = new List<AppearanceComponent>();
            var componentCount = 0;

            var orderedComponents = appearance.Components.OrderByDescending
            (
                c =>
                {
                    var priorityAttribute = c.Bodypart.GetCustomAttribute<DescriptionPriorityAttribute>();

                    return priorityAttribute?.Priority ?? 0;
                }
            );

            foreach (var component in orderedComponents)
            {
                ++componentCount;

                // Break the description into paragraphs every third component
                if (componentCount % 3 == 0)
                {
                    sb.Append("\n\n");
                }

                if (partsToSkip.Contains(component))
                {
                    continue;
                }

                var csb = new StringBuilder();

                var transformation = component.Transformation;
                if (component.Bodypart.IsChiral())
                {
                    var sameSpecies = AreChiralPartsTheSameSpecies(appearance, component);
                    csb.Append
                    (
                        sameSpecies
                            ? transformation.UniformDescription
                            : transformation.SingleDescription
                    );

                    if (sameSpecies)
                    {
                        if
                        (
                            appearance.TryGetAppearanceComponent
                            (
                                component.Bodypart,
                                component.Chirality.Opposite(),
                                out var partToSkip
                            )
                        )
                        {
                            partsToSkip.Add(partToSkip);
                        }
                    }
                }
                else
                {
                    csb.Append(transformation.SingleDescription);
                }

                if (component.Pattern.HasValue)
                {
                    csb.Append("A {@pattern}, {@colour|pattern} pattern covers it.");
                }

                var tokenizedDesc = csb.ToString();
                var componentDesc = ReplaceTokensWithContent(tokenizedDesc, appearance, component);

                sb.Append(componentDesc);
            }

            var description = sb.ToString().Trim();
            var withSentenceSpacing = _sentenceSpacingRegex.Replace(description, ". ");

            return withSentenceSpacing;
        }

        /// <summary>
        /// Determines if the chiral parts on the character are the same species.
        /// </summary>
        /// <param name="appearanceConfiguration">The character and its appearances.</param>
        /// <param name="component">The chiral component.</param>
        /// <returns>true if the parts are the same species; otherwise, false.</returns>
        private bool AreChiralPartsTheSameSpecies
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent component
        )
        {
            if
            (
                !appearanceConfiguration.TryGetAppearanceComponent
                (
                    component.Bodypart,
                    component.Chirality.Opposite(),
                    out var opposingComponent
                )
            )
            {
                return false;
            }

            return string.Equals(component.Transformation.Species.Name, opposingComponent.Transformation.Species.Name);
        }

        /// <summary>
        /// Builds a shift message for the given character if the given transformation were to be applied.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="component">The component to build the message from.</param>
        /// <returns>The shift message.</returns>
        [Pure, NotNull]
        public string BuildShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent component
        )
        {
            var transformation = component.Transformation;

            return ReplaceTokensWithContent(transformation.ShiftMessage, appearanceConfiguration, component);
        }

        /// <summary>
        /// Builds a shift message for the given character if the given transformation were to be applied to both chiral
        /// components at the same time.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="component">The component to build the message from.</param>
        /// <returns>The uniform shift message.</returns>
        [Pure, NotNull]
        public string BuildUniformShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent component
        )
        {
            var transformation = component.Transformation;

            if (transformation.UniformShiftMessage is null)
            {
                throw new InvalidOperationException("Missing uniform shift description.");
            }

            return ReplaceTokensWithContent(transformation.UniformShiftMessage, appearanceConfiguration, component);
        }

        /// <summary>
        /// Builds a grow message for the given character if the given transformation were to be applied.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="component">The component to build the message from.</param>
        /// <returns>The grow message.</returns>
        [Pure, NotNull]
        public string BuildGrowMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent component
        )
        {
            var transformation = component.Transformation;

            return ReplaceTokensWithContent(transformation.GrowMessage, appearanceConfiguration, component);
        }

        /// <summary>
        /// Builds a grow message for the given character if the given transformation were to be applied to both chiral
        /// components at the same time.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="component">The component to build the message from.</param>
        /// <returns>The uniform grow message.</returns>
        [Pure, NotNull]
        public string BuildUniformGrowMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent component
        )
        {
            var transformation = component.Transformation;

            if (transformation.UniformGrowMessage is null)
            {
                throw new InvalidOperationException("Missing uniform grow description.");
            }

            return ReplaceTokensWithContent(transformation.UniformGrowMessage, appearanceConfiguration, component);
        }

        /// <summary>
        /// Builds a removal message for the given character if the given transformation were to be applied.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="bodypart">The bodypart to build the message from.</param>
        /// <returns>The removal message.</returns>
        [Pure, NotNull]
        public string BuildRemoveMessage([NotNull]Appearance appearanceConfiguration, Bodypart bodypart)
        {
            string removalText;
            switch (bodypart)
            {
                case Bodypart.Hair:
                {
                    removalText = "{@target}'s hair becomes dull and colourless. Strand by strand, tuft by tuft, " +
                                  "it falls out, leaving an empty scalp.";
                    break;
                }
                case Bodypart.Face:
                {
                    removalText = "{@target}'s face begins to warp strangely. Slowly, their features smooth and " +
                                  "vanish, leaving a blank surface.";
                    break;
                }
                case Bodypart.Ear:
                {
                    removalText = $"{{@target}}'s {{@side}} {bodypart.Humanize().Transform(To.LowerCase)} shrivels " +
                                  $"and vanishes.";
                    break;
                }
                case Bodypart.Eye:
                {
                    removalText = $"{{@target}}'s {{@side}} {bodypart.Humanize().Transform(To.LowerCase)} deflates " +
                                  $"as their eye socket closes, leaving nothing behind.";
                    break;
                }
                case Bodypart.Teeth:
                {
                    removalText = "With a strange popping sound, {@target}'s teeth retract and disappear.";
                    break;
                }
                case Bodypart.Leg:
                case Bodypart.Arm:
                {
                    removalText = $"{{@target}}'s {{@side}} {bodypart.Humanize().Transform(To.LowerCase)} shrivels " +
                                  $"and retracts, vanishing.";
                    break;
                }
                case Bodypart.Tail:
                {
                    removalText = "{@target}'s tail flicks and thrashes for a moment, before it thins out and " +
                                  "disappears into nothing.";
                    break;
                }
                case Bodypart.Wing:
                {
                    removalText = $"{{@target}}'s {{@side}} {bodypart.Humanize().Transform(To.LowerCase)} stiffens " +
                                  $"and shudders, before losing cohesion and disappearing into their body.";
                    break;
                }
                case Bodypart.Penis:
                {
                    removalText = "{@target}'s shaft twitches and shudders, as it begins to shrink and retract. In " +
                                  "mere moments, it's gone, leaving nothing.";
                    break;
                }
                case Bodypart.Vagina:
                {
                    removalText = "{@target}'s slit contracts and twitches. A strange sensation rushes through " +
                                  "{@f|them} as the opening zips up and fills out, leaving nothing.";
                    break;
                }
                case Bodypart.Head:
                {
                    removalText = "{@target}'s head warps strangely before it deflates like a balloon, disappearing.";
                    break;
                }
                case Bodypart.Legs:
                case Bodypart.Arms:
                {
                    removalText = $"{{@target}}'s {bodypart.Humanize().Transform(To.LowerCase)} shrivel and " +
                                  $"retract, vanishing.";
                    break;
                }
                case Bodypart.Body:
                {
                    removalText = "{@target}'s torso crumples into itself as their main body collapses, shifting " +
                                  "and vanishing.";
                    break;
                }
                case Bodypart.Wings:
                {
                    removalText = $"{{@target}}'s {bodypart.Humanize().Transform(To.LowerCase)} stiffen and shudder, " +
                                  $"before losing cohesion and disappearing into their body.";
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            return ReplaceTokensWithContent(removalText, appearanceConfiguration, null);
        }

        /// <summary>
        /// Builds a pattern colour shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildPatternColourShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage =
                $"{{@target}}'s {currentComponent.Bodypart.Humanize()} morphs, as" +
                $" {{@f|their}} {{@pattern}} hues turn into {currentComponent.PatternColour}.";

            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern colour shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildUniformPatternColourShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var bodypartName = currentComponent.Bodypart.Humanize().Pluralize().ToLower();

            var shiftMessage =
                $"{{@target}}'s {bodypartName} morph, as" +
                $" {{@f|their}} {{@pattern}} hues turn into {currentComponent.PatternColour}.";

            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildUniformPatternShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var bodypartName = currentComponent.Bodypart.Humanize().Pluralize().ToLower();

            var shiftMessage =
                $"The surface of {{@target}}'s {bodypartName} morph, as" +
                " {@colour|pattern} {@pattern} patterns spread across them, replacing their existing ones.";

            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern addition message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildUniformPatternAddMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var bodypartName = currentComponent.Bodypart.Humanize().Pluralize().ToLower();

            var shiftMessage =
                $"The surface of {{@target}}'s {bodypartName} morph, as" +
                " {@colour|pattern} {@pattern} patterns spread across them.";

            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern removal message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildUniformPatternRemoveMessage
        (
            [NotNull]Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var bodypartName = currentComponent.Bodypart.Humanize().Pluralize().ToLower();

            var shiftMessage =
                $"The surface of {{@target}}'s {bodypartName} shimmer, as the patterns on them fade and vanish.";

            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildPatternShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage =
                $"The surface of {{@target}}'s {currentComponent.Bodypart.Humanize().Transform(To.LowerCase)} " +
                "morphs, as {@colour|pattern} {@pattern} patterns spread across it, replacing their existing ones.";

            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern addition message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildPatternAddMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var bodypartName = currentComponent.Bodypart.Humanize().ToLower();

            var shiftMessage =
                $"The surface of {{@target}}'s {bodypartName} morphs, as" +
                " {@colour|pattern} {@pattern} patterns spreads across it.";

            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern removal message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildPatternRemoveMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var bodypartName = currentComponent.Bodypart.Humanize().ToLower();

            var shiftMessage =
                $"The surface of {{@target}}'s {bodypartName} shimmers, as the pattern on it fades and vanishes.";

            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a base colour shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildColourShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage =
                $"{{@target}}'s {currentComponent.Bodypart.Humanize().Transform(To.LowerCase)} morphs, as" +
                $" {{@f|their}} existing hues turn into {currentComponent.BaseColour}.";

            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a base uniform colour shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildUniformColourShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var bodypartName = currentComponent.Bodypart.Humanize().Pluralize().ToLower();

            var shiftMessage =
                $"{{@target}}'s {bodypartName} morph, as" +
                $" {{@f|their}} existing hues turn into {currentComponent.BaseColour}.";

            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }
    }
}
