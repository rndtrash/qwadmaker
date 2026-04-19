using Shared;
using SixLabors.ImageSharp.PixelFormats;

namespace QWadMaker.Settings
{
    static class Serialization
    {
        public static string ToString(LumpType textureType)
        {
            return textureType switch
            {
                LumpType.Palette => "palette",
                LumpType.SimpleTexture => "qpic",
                LumpType.Font => "font",
                _ => "mipmap",
            };
        }

        public static LumpType? ReadTextureType(string? str)
        {
            if (str is null)
                return null;

            return str.ToLowerInvariant() switch
            {
                "mipmap" => (LumpType?)LumpType.MipmapTexture,
                "qpic" => (LumpType?)LumpType.SimpleTexture,
                "font" => (LumpType?)LumpType.Font,
                _ => throw new InvalidDataException($"Invalid texture type: '{str}'."),
            };
        }


        public static string ToString(MipmapLevel mipmapLevel)
        {
            return mipmapLevel switch
            {
                MipmapLevel.Mipmap1 => "mipmap1",
                MipmapLevel.Mipmap2 => "mipmap2",
                MipmapLevel.Mipmap3 => "mipmap3",
                _ => "",
            };
        }

        public static MipmapLevel? ReadMipmapLevel(string? str)
        {
            if (str is null)
                return null;

            return str.ToLowerInvariant() switch
            {
                "mipmap1" => (MipmapLevel?)MipmapLevel.Mipmap1,
                "mipmap2" => (MipmapLevel?)MipmapLevel.Mipmap2,
                "mipmap3" => (MipmapLevel?)MipmapLevel.Mipmap3,
                _ => (MipmapLevel?)MipmapLevel.Main,
            };
        }


        public static string ToString(DitheringAlgorithm ditheringAlgorithm)
        {
            return ditheringAlgorithm switch
            {
                DitheringAlgorithm.FloydSteinberg => "floyd-steinberg",
                _ => "none",
            };
        }

        public static DitheringAlgorithm? ReadDitheringAlgorithm(string? str)
        {
            if (str is null)
                return null;

            return str.ToLowerInvariant() switch
            {
                "none" => (DitheringAlgorithm?)DitheringAlgorithm.None,
                "floyd-steinberg" => (DitheringAlgorithm?)DitheringAlgorithm.FloydSteinberg,
                _ => throw new InvalidDataException($"Invalid dithering algorithm: '{str}'."),
            };
        }


        public static string ToString(DecalTransparencySource decalTransparencySource)
        {
            return decalTransparencySource switch
            {
                DecalTransparencySource.Grayscale => "grayscale",
                _ => "alpha",
            };
        }

        public static DecalTransparencySource? ReadDecalTransparencySource(string? str)
        {
            if (str is null)
                return null;

            return str.ToLowerInvariant() switch
            {
                "alpha" => (DecalTransparencySource?)DecalTransparencySource.AlphaChannel,
                "grayscale" => (DecalTransparencySource?)DecalTransparencySource.Grayscale,
                _ => throw new InvalidDataException($"Invalid decal transparency: '{str}'."),
            };
        }


        public static string ToString(Rgba32 color) => color.ToHex();

        public static Rgba32? ReadRgba32(string? str) => str is null ? null : Rgba32.ParseHex(str);
    }
}
