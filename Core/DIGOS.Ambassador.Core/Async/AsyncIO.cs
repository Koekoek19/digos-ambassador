﻿//
//  AsyncIO.cs
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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Core.Async
{
    /// <summary>
    /// Asynchronous file operations.
    /// </summary>
    public static class AsyncIO
    {
        /// <summary>
        /// This is the same default buffer size as
        /// <see cref="StreamReader"/> and <see cref="FileStream"/>.
        /// </summary>
        private const int DefaultBufferSize = 4096;

        /// <summary>
        /// Indicates that
        /// 1. The file is to be used for asynchronous reading.
        /// 2. The file is to be accessed sequentially from beginning to end.
        /// </summary>
        private const FileOptions DefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        /// <summary>
        /// Asynchronously reads all lines from the given file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="encoding">The encoding of the file.</param>
        /// <returns>The contents of the file.</returns>
        [ItemNotNull]
        public static async Task<string[]> ReadAllLinesAsync
        (
            [NotNull] string path,
            [CanBeNull] Encoding encoding = null
        )
        {
            encoding = encoding ?? Encoding.UTF8;

            // Open the FileStream with the same FileMode, FileAccess
            // and FileShare as a call to File.OpenText would've done.
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultOptions))
            {
                return await ReadAllLinesAsync(stream, encoding);
            }
        }

        /// <summary>
        /// Asynchronously reads all lines from the given stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="encoding">The encoding of the file.</param>
        /// <param name="leaveOpen">Whether to leave the stream open after the call.</param>
        /// <returns>The contents of the file.</returns>
        public static async Task<string[]> ReadAllLinesAsync
        (
            [NotNull] Stream stream,
            [CanBeNull] Encoding encoding = null,
            bool leaveOpen = true
        )
        {
            encoding = encoding ?? Encoding.UTF8;

            var lines = new List<string>();
            using (var reader = new StreamReader(stream, encoding, false, DefaultBufferSize, leaveOpen))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Asynchronously reads all text from the given file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="encoding">The encoding of the file.</param>
        /// <returns>The contents of the file.</returns>
        [NotNull]
        public static async Task<string> ReadAllTextAsync([NotNull] string path, [CanBeNull] Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            // Open the FileStream with the same FileMode, FileAccess
            // and FileShare as a call to File.OpenText would've done.
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultOptions))
            {
                return await ReadAllTextAsync(stream, encoding);
            }
        }

        /// <summary>
        /// Asynchronously reads all text from the given stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="leaveOpen">Whether to leave the stream open after the read.</param>
        /// <returns>The contents of the stream.</returns>
        public static async Task<string> ReadAllTextAsync
        (
            [NotNull] Stream stream,
            [CanBeNull] Encoding encoding = null,
            bool leaveOpen = true
        )
        {
            encoding = encoding ?? Encoding.UTF8;

            using (var reader = new StreamReader(stream, encoding, false, DefaultBufferSize, leaveOpen))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
