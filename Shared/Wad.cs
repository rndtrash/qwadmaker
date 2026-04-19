using SixLabors.ImageSharp.PixelFormats;

namespace Shared
{
    public class Wad(Rgba32[] palette, List<Texture> textures)
    {
        const string MAGIC = "WAD2";

        public List<Texture> Textures { get; } = textures;
        public Rgba32[] Palette { get; private set; } = palette;

        public void Save(string path)
        {
            using var file = File.Create(path);
            Save(file);
        }

        public void Save(Stream stream)
        {
            stream.Write(MAGIC);
            stream.Write((uint)Textures.Count);

            var textureOffset = (uint)(stream.Position + 4);
            var lumps = Textures
                .Select(texture =>
                {
                    var offset = textureOffset;
                    var textureFileSize = GetTextureFileSize(texture);
                    textureOffset += textureFileSize;

                    return new Lump
                    {
                        Offset = offset,
                        CompressedLength = textureFileSize,
                        FullLength = textureFileSize,
                        Type = texture.Type,
                        CompressionType = 0,    // Always uncompressed
                        Name = texture.Name,
                    };
                })
                .ToArray();
            var lumpOffset = (uint)(stream.Position + 4 + lumps.Sum(lump => lump.CompressedLength));
            stream.Write(lumpOffset);

            foreach (var texture in Textures)
                WriteTexture(stream, texture);

            foreach (var lump in lumps)
                WriteLump(stream, lump);
        }


        public static Wad Load(string wadPath, string? palettePath, Action<int, string, Exception>? errorCallback = null)
        {
            using var file = File.OpenRead(wadPath);
            var palette = palettePath != null ? Shared.Palette.Read(palettePath) : Shared.Palette.DefaultQuakePalette;
            return Load(file, palette, errorCallback);
        }

        public static Wad Load(Stream stream, Rgba32[] palette, Action<int, string, Exception>? errorCallback = null)
        {

            var fileSignature = stream.ReadString(4);
            if (fileSignature != MAGIC)
                throw new InvalidDataException($"Expected file to start with '{MAGIC}' but found '{fileSignature}'.");

            var textureCount = stream.ReadUint();

            var lumpOffset = stream.ReadUint();
            stream.Seek(lumpOffset, SeekOrigin.Begin);
            var lumps = Enumerable.Range(0, (int)textureCount)
                .Select(i => ReadLump(stream))
                .ToArray();

            List<Texture> textures = [];
            for (int i = 0; i < lumps.Length; i++)
            {
                var lump = lumps[i];
                try
                {
                    textures.Add(ReadTexture(stream, lump));
                }
                catch (Exception ex)
                {
                    if (errorCallback == null)
                        throw;

                    errorCallback(i, lump.Name, ex);
                }
            }

            return new Wad(palette, textures);
        }


        private static void WriteLump(Stream stream, Lump lump)
        {
            stream.Write(lump.Offset);
            stream.Write(lump.CompressedLength);
            stream.Write(lump.FullLength);

            stream.Write((byte)lump.Type);
            stream.Write(lump.CompressionType);
            stream.Write(new byte[2]);  // Padding.

            stream.Write(lump.Name, 16);
        }

        private static void WriteTexture(Stream stream, Texture texture)
        {
            if (texture.Type == LumpType.MipmapTexture)
            {
                stream.Write(texture.Name, 16);
                stream.Write((uint)texture.Width);
                stream.Write((uint)texture.Height);
                stream.Write((uint)40);
                stream.Write((uint)(40 + texture.ImageData.Length));
                stream.Write((uint)(40 + texture.ImageData.Length + texture.Mipmap1Data!.Length));
                stream.Write((uint)(40 + texture.ImageData.Length + texture.Mipmap1Data!.Length + texture.Mipmap2Data!.Length));

                stream.Write(texture.ImageData);
                stream.Write(texture.Mipmap1Data);
                stream.Write(texture.Mipmap2Data);
                stream.Write(texture.Mipmap3Data);
            }
            else if (texture.Type == LumpType.Font)
            {
                stream.Write((uint)texture.Width);
                stream.Write((uint)texture.Height);

                stream.Write((uint)texture.RowCount);
                stream.Write((uint)texture.CharHeight);
                foreach (var charInfo in texture.CharInfos!)
                {
                    stream.Write((ushort)charInfo.StartOffset);
                    stream.Write((ushort)charInfo.CharWidth);
                }
                stream.Write(texture.ImageData);
            }
            else if (texture.Type == LumpType.SimpleTexture)
            {
                stream.Write((uint)texture.Width);
                stream.Write((uint)texture.Height);
                stream.Write(texture.ImageData);
            }
            else if (texture.Type == LumpType.FlatTexture)
            {
                stream.Write(texture.ImageData);
            }
            else
            {
                throw new InvalidDataException($"Unknown texture type: {texture.Type}.");
            }
        }


        private static Lump ReadLump(Stream stream)
        {
            var lump = new Lump();
            lump.Offset = stream.ReadUint();
            lump.CompressedLength = stream.ReadUint();
            lump.FullLength = stream.ReadUint();

            var types = stream.ReadBytes(4);    // 2 type bytes + padding.
            lump.Type = (LumpType)types[0];
            lump.CompressionType = types[1];

            lump.Name = stream.ReadString(16);
            return lump;
        }

        private static Texture ReadTexture(Stream stream, Lump lump)
        {
            stream.Seek(lump.Offset, SeekOrigin.Begin);

            if (lump.Type == LumpType.MipmapTexture)
            {
                var name = stream.ReadString(16);
                var width = (int)stream.ReadUint();
                var height = (int)stream.ReadUint();
                var offset = stream.ReadUint();
                var mipmap1Offset = stream.ReadUint();
                var mipmap2Offset = stream.ReadUint();
                var mipmap3Offset = stream.ReadUint();

                stream.Seek(lump.Offset + offset, SeekOrigin.Begin);
                var imageData = stream.ReadBytes(width * height);

                stream.Seek(lump.Offset + mipmap1Offset, SeekOrigin.Begin);
                var mipmap1Data = stream.ReadBytes(width / 2 * height / 2);

                stream.Seek(lump.Offset + mipmap2Offset, SeekOrigin.Begin);
                var mipmap2Data = stream.ReadBytes(width / 4 * height / 4);

                stream.Seek(lump.Offset + mipmap3Offset, SeekOrigin.Begin);
                var mipmap3Data = stream.ReadBytes(width / 8 * height / 8);

                return Texture.CreateMipmapTexture(name, width, height, imageData, mipmap1Data, mipmap2Data, mipmap3Data);
            }
            else if (lump.Type == LumpType.Font)
            {
                // Supposedly the width of the image, but in gfx.wad this contains the same value as the row-height field. The actual width seems to always be 256.
                stream.ReadUint();
                var width = Constants.FontImageWidth;
                var height = (int)stream.ReadUint();

                var rowCount = (int)stream.ReadUint();
                var charHeight = (int)stream.ReadUint();
                var charInfos = Enumerable.Range(0, Constants.FontCharacterCount)
                    .Select(i => new CharInfo { StartOffset = stream.ReadUshort(), CharWidth = stream.ReadUshort() })
                    .ToArray();
                var imageData = stream.ReadBytes(width * height);

                return Texture.CreateFont(lump.Name, width, height, rowCount, charHeight, charInfos, imageData);
            }
            else if (lump.Type == LumpType.SimpleTexture)
            {
                var width = (int)stream.ReadUint();
                var height = (int)stream.ReadUint();
                var imageData = stream.ReadBytes(width * height);

                return Texture.CreateSimpleTexture(lump.Name, width, height, imageData);
            }
            else if (lump.Type == LumpType.FlatTexture)
            {
                // TODO:
                throw new NotImplementedException();
            }
            else
            {
                throw new InvalidDataException($"Unknown texture type: {lump.Type}.");
            }
        }

        private static uint GetTextureFileSize(Texture texture)
        {
            if (texture.Type == LumpType.MipmapTexture)
            {
                var size = 40;
                size += texture.ImageData.Length;
                size += texture.Mipmap1Data!.Length;
                size += texture.Mipmap2Data!.Length;
                size += texture.Mipmap3Data!.Length;
                return (uint)size;
            }
            else if (texture.Type == LumpType.Font)
            {
                var size = 16;
                size += texture.CharInfos!.Length * 4;
                size += texture.ImageData.Length;
                return (uint)size;
            }
            else if (texture.Type == LumpType.SimpleTexture)
            {
                var size = 8;
                size += texture.ImageData.Length;
                return (uint)size;
            }
            else if (texture.Type == LumpType.FlatTexture)
            {
                // TODO:
                throw new NotImplementedException();
            }
            else
            {
                throw new NotSupportedException($"Texture type {texture.Type} is not supported.");
            }
        }


        class Lump
        {
            public uint Offset { get; set; }
            public uint CompressedLength { get; set; }
            public uint FullLength { get; set; }
            public LumpType Type { get; set; }
            public byte CompressionType { get; set; }   // Always set to 0 (no compression).
            public string Name { get; set; } = "";      // 16 bytes.
        }
    }
}
