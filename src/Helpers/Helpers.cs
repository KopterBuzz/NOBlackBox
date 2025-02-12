using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NOBlackBox.src.Helpers
{
    public static class Helpers
    {
        public static (bool isFolder, bool success) IsFileOrFolder(string path)
        {
            try
            {
                var attr = File.GetAttributes(path);
                return attr.HasFlag(FileAttributes.Directory) ? (true, true)! : (false, true)!;
            }
            catch (FileNotFoundException)
            {
                return (false, false);
            }
        }
    }
}
