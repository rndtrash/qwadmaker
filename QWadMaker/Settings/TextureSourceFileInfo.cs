using Shared.FileSystem;

namespace QWadMaker.Settings
{
    class TextureSourceFileInfo(string path, int fileSize, FileHash fileHash, DateTimeOffset lastModified, TextureSettings settings) : Shared.FileSystem.FileInfo(path, fileSize, fileHash, lastModified)
    {
        public TextureSettings Settings { get; } = settings;
    }
}
