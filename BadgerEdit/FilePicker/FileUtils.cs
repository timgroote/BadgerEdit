using System.IO;

namespace BadgerEdit.FilePicker
{
    public static class FileUtils
    {
        public static bool IsValidPath(string file)
        {
            try
            {
                Path.GetFullPath(file);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetFileInfo(string fileName, out FileInfo realFile)
        {
            try
            {
                realFile = new FileInfo(fileName);
                return true;
            }
            catch
            {
                realFile = null;
                return false;
            }
        }
    }
}
