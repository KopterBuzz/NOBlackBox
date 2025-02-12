using System.IO;

namespace NOBlackBox
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
