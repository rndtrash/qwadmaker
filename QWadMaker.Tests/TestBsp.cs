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
    public void ExtractTexturesDefaultPalette()
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
        TextureExtracting.ExtractTextures(GetDataPath("quake_minimal.bsp"), null, outputPath, extractionSettings, Logger);

        // Check that both the reference folder and the output folder have the exact same set of files
        var targetTexturePaths = Directory.GetFiles(GetDataPath("bsp_textures"));
        var outputTexturePaths = Directory.GetFiles(outputPath);
        var textureNames = targetTexturePaths.Select(file => Path.GetFileName(file)).ToArray();
        var outputTextureNames = outputTexturePaths.Select(file => Path.GetFileName(file)).ToArray();
        CollectionAssert.AreEquivalent(textureNames, outputTextureNames);

        foreach (var textureName in textureNames)
        {
            var targetTexture = File.ReadAllBytes(GetDataPath(Path.Combine("bsp_textures", textureName)));
            var outputTexture = File.ReadAllBytes(GetOutputPath(Path.Combine("bsp_extract", textureName)));
            CollectionAssert.AreEqual(targetTexture, outputTexture);
        }
    }
}
