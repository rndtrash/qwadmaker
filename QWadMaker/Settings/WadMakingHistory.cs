using Shared.JSON;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.Json;
using FileInfo = Shared.FileSystem.FileInfo;

namespace QWadMaker.Settings
{
    class WadMakingHistory(FileInfo outputFile, Rgba32[] palette, IDictionary<string, TextureSourceFileInfo[]> textureInputs)
    {
        private static JsonSerializerOptions SerializerOptions { get; }

        static WadMakingHistory()
        {
            SerializerOptions = new JsonSerializerOptions();
            SerializerOptions.Converters.Add(new WadMakingHistoryJsonSerializer());
            SerializerOptions.TypeInfoResolver = new NoReflectionJsonTypeInfoResolver();
        }


        const string HistoryFilename = "qwadmaker.dat";


        public FileInfo OutputFile { get; } = outputFile;
        public Dictionary<string, TextureSourceFileInfo[]> TextureInputs { get; } = textureInputs.ToDictionary(kv => kv.Key, kv => kv.Value);
        public Rgba32[] Palette { get; } = palette;

        public static WadMakingHistory? Load(string folder)
        {
            try
            {
                var historyFilePath = Path.Combine(folder, HistoryFilename);
                if (!File.Exists(historyFilePath))
                    return null;

                var json = File.ReadAllText(historyFilePath);
                return JsonSerializer.Deserialize<WadMakingHistory>(json, SerializerOptions);
            }
            catch
            {
                // Error reading file? Just ignore - history only matters when doing incremental updates, and we can always fall back to doing a full rebuild:
                return null;
            }
        }

        public static bool IsHistoryFile(string path) => Path.GetFileName(path) == HistoryFilename;


        public void Save(string folder)
        {
            var historyFilePath = Path.Combine(folder, HistoryFilename);
            File.WriteAllText(historyFilePath, JsonSerializer.Serialize(this, SerializerOptions));
        }
    }
}
