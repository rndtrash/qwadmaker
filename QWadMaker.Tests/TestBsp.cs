namespace QWadMaker.Tests;

[TestClass]
public sealed class TestBsp
{
    static string GetDataPath(string path)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", path);
    }

    static string GetOutputPath(string path)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", path);
    }

    static readonly Shared.Logger Logger = new(Console.WriteLine);

    [TestMethod]
    public void ExtractTextures()
    {
        var outputPath = GetOutputPath("bsp_extract");
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
        }

        var extractionSettings = new ExtractionSettings
        {
            ExtractMipmaps = true,
            NoFullbrightMasks = true,
            OverwriteExistingFiles = true,
            OutputFormat = Shared.FileFormats.ImageFormat.Png,
            SaveAsIndexed = true,
        };
        TextureExtracting.ExtractTextures(GetDataPath("quake_minimal.bsp"), GetDataPath("palette.lmp"), outputPath, extractionSettings, Logger);

        var targetTextures = Directory.GetFiles(GetDataPath("bsp_textures"));
        var outputTextures = Directory.GetFiles(outputPath);
        CollectionAssert.AreEquivalent(targetTextures.Select(file => Path.GetFileName(file)).ToArray(), outputTextures.Select(file => Path.GetFileName(file)).ToArray());

        for (var i = 0; i < targetTextures.Length; i++)
        {
            var targetTexture = File.ReadAllBytes(targetTextures[i]);
            var outputTexture = File.ReadAllBytes(outputTextures[i]);
            CollectionAssert.AreEqual(targetTexture, outputTexture);
        }
    }
}
