using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace NOBlackBox
{
    public static class ZipHelper
    {
        public static async Task<string> ZipFileAsync(
            string filePath,
            int maxRetries = 5,
            int initialDelayMs = 16)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            // 🔁 Wait until file is free (with exponential backoff)
            await WaitForFileAccessAsync(filePath, maxRetries, initialDelayMs)
                .ConfigureAwait(false);

            string zipPath = Path.ChangeExtension(filePath, ".zip.acmi");

            using (FileStream zipStream = new FileStream(
                zipPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry(
                    Path.GetFileName(filePath),
                    CompressionLevel.Optimal);

                // Open once and hold the handle during the copy
                using (FileStream inputStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true))
                using (Stream entryStream = entry.Open())
                {
                    await inputStream.CopyToAsync(entryStream).ConfigureAwait(false);
                }
            }

            return zipPath;
        }

        private static async Task WaitForFileAccessAsync(
            string filePath,
            int maxRetries,
            int initialDelayMs)
        {
            int delay = initialDelayMs;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.None))
                    {
                        // Successfully acquired exclusive access
                        return;
                    }
                }
                catch (IOException) when (attempt < maxRetries)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                    delay *= 2; // 📈 exponential backoff
                }
            }

            throw new IOException(
                $"The file '{filePath}' is still in use after {maxRetries} retries.");
        }

        /// <summary>
        /// Deletes a file with exponential backoff to avoid sharing violations.
        /// </summary>
        public static async Task DeleteFileAsync(
            string filePath,
            int maxRetries = 5,
            int initialDelayMs = 16)
        {
            await WaitForFileAccessAsync(filePath, maxRetries, initialDelayMs).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                return; // already gone — nothing to do

            int delay = initialDelayMs;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    File.Delete(filePath);
                    return; // success
                }
                catch (IOException) when (attempt < maxRetries)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                    delay *= 2; // exponential backoff
                }
                catch (UnauthorizedAccessException) when (attempt < maxRetries)
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                    delay *= 2;
                }
            }

            throw new IOException(
                $"Failed to delete file '{filePath}' after {maxRetries} retries.");
        }
    }
}
