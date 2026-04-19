using SixLabors.ImageSharp.PixelFormats;

namespace Shared
{
    public static class Lmp
    {
        public const int PALETTE_SIZE = 256;

        public static Rgba32[] Read(Stream input)
        {
            return [.. Enumerable.Range(0, PALETTE_SIZE).Select(i => input.ReadColor())];
        }

        public static Rgba32[] Read(string path)
        {
            using var stream = File.OpenRead(path);
            return Read(stream);
        }

        public static void Write(Stream output, Rgba32[] palette)
        {
            if (palette.Length != PALETTE_SIZE)
            {
                throw new Exception($"Attempted to write a non-standard palette (expected {PALETTE_SIZE} colors, got {palette.Length})");
            }

            foreach (var color in palette)
            {
                output.Write(color);
            }
        }
    }
}
