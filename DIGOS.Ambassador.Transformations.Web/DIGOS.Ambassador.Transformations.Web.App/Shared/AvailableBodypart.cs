//
//  AvailableBodypart.cs
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

using DIGOS.Ambassador.Plugins.Transformations.Transformations;

namespace DIGOS.Ambassador.Transformations.Web.App.Shared
{
    /// <summary>
    /// Represents an available bodypart.
    /// </summary>
    public class AvailableBodypart
    {
        /// <summary>
        /// Gets the bodypart.
        /// </summary>
        public Bodypart Bodypart { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the bodypart has been added to the species.
        /// </summary>
        public bool IsAdded { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvailableBodypart"/> class.
        /// </summary>
        /// <param name="bodypart">The bodypart.</param>
        public AvailableBodypart(Bodypart bodypart)
        {
            this.Bodypart = bodypart;
        }
    }
}
