using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;

namespace NOBlackBox
{
    internal static class ZIPFileCreator
    {
        public static void CreateZip(string content, string entryName, string destinationZipPath)
        {
            // Create a memory stream to hold the zip archive in memory
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Create a new zip archive in the memory stream

                using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // Create a new entry in the zip archive
                    ZipArchiveEntry entry = archive.CreateEntry(entryName);

                    // Write the string content to the entry
                    using (StreamWriter writer = new StreamWriter(entry.Open(), Encoding.UTF8))
                    {
                        writer.Write(content);
                    }
                }

                // Save the zip archive to disk
                using (FileStream fileStream = new FileStream(destinationZipPath, FileMode.Create, FileAccess.Write))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
            }
        }
    }
}
