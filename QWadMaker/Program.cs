using System.Text;
using Shared;
using System.Diagnostics.CodeAnalysis;
using Shared.FileFormats;
using System.CommandLine;

namespace QWadMaker
{
    class ProgramSettings
    {
        // Build settings:
        public bool FullRebuild { get; set; }               // -full            Forces a full rebuild instead of an incremental one
        public bool IncludeSubDirectories { get; set; }     // -subdirs         Also include images in sub-directories

        // Extract settings:
        [MemberNotNullWhen(true, nameof(InputFilePath))]
        [MemberNotNullWhen(true, nameof(OutputDirectory))]
        public bool Extract { get; set; }                   //                  Texture extraction mode is enabled when the first argument (path) is a wad or bsp file.
        public bool ExtractMipmaps { get; set; }            // -mipmaps         Also extract mipmaps
        public bool NoFullbrightMasks { get; set; }         // -nofullbright    Do not extract fullbright masks
        public bool OverwriteExistingFiles { get; set; }    // -overwrite       Extract mode only, enables overwriting of existing image files (off by default)
        public ImageFormat OutputImageFormat { get; set; }  // -format          Extracted images output format (png, jpg, gif, bmp or tga).
        public bool ExtractAsIndexed { get; set; }          // -indexed         Extracted images are indexed and contain the original texture's palette. Only works with png, gif and bmp.

        [MemberNotNullWhen(true, nameof(InputFilePath))]
        [MemberNotNullWhen(true, nameof(InputPalette))]
        [MemberNotNullWhen(true, nameof(OutputFilePath))]
        public bool ExtractToWad { get; set; }              //                  This extraction mode is enabled when the first argument is a bsp file path, and the second a wad file path.

        // Bsp settings:
        [MemberNotNullWhen(true, nameof(InputFilePath))]
        [MemberNotNullWhen(true, nameof(OutputFilePath))]
        public bool RemoveEmbeddedTextures { get; set; }    // -remove          Removes embedded textures from the given bsp file

        [MemberNotNullWhen(true, nameof(InputFilePath))]
        [MemberNotNullWhen(true, nameof(ExtraInputFilePath))]
        [MemberNotNullWhen(true, nameof(OutputFilePath))]
        public bool EmbedTextures { get; set; }

        // Other settings:
        public string? InputDirectory { get; set; }         // Build mode only
        public string? InputFilePath { get; set; }          // Wad or bsp path
        public string? InputPalette { get; set; }           // LMP path
        public string? ExtraInputFilePath { get; set; }     // Bsp path (when embedding textures)
        public string? OutputDirectory { get; set; }        // Extract mode only
        public string? OutputFilePath { get; set; }         // Output bsp path (when adding or removing embedded textures)
        public string? OutputPalettePath { get; set; }      // Output LMP path

        public bool DisableFileLogging { get; set; }        // -nologfile   disables logging to a file (parent-directory\wadmaker.log)
    }

    class Program
    {
        static int Main(string[] args)
        {
            TextWriter? LogFile = null;
            void Log(string? message)
            {
                Console.WriteLine(message);
                LogFile?.WriteLine(message);
            }

            try
            {
                var assemblyName = typeof(Program).Assembly.GetName();
                var launchInfo = $"{Environment.ProcessPath} (v{assemblyName.Version}) {string.Join(" ", args)}";
                Log(launchInfo);

                var logger = new Logger(Log);

                RootCommand rootCommand = new("QWadMaker - a command line tool to create, modify and extract the Quake WADs");

                #region Common parameters
                Option<bool> disableFileLogging = new("--nologfile")
                {
                    Description = "Disables logging to a file (parent-directory\\wadmaker.log)",
                    Recursive = true
                };
                rootCommand.Add(disableFileLogging);

                void SetupLogging(ParseResult parseResult, string inputFile)
                {
                    if (!parseResult.GetValue(disableFileLogging))
                    {
                        var logName = Path.GetFileNameWithoutExtension(inputFile);
                        var logFilePath = Path.Combine(Path.GetDirectoryName(inputFile) ?? "", $"wadmaker - {logName}.log");
                        LogFile = new StreamWriter(logFilePath, false, Encoding.UTF8);
                        LogFile.WriteLine(launchInfo);
                    }
                }
                #endregion

                #region Extract subcommand
                {
                    Command extract = new("extract", "Extract a .wad or .bsp into a folder");

                    Argument<string> inputFilePath = new("input")
                    {
                        Description = "The .wad or .bsp that you want to extract the textures from",
                        Arity = ArgumentArity.ExactlyOne
                    };
                    extract.Add(inputFilePath);

                    Argument<string> outputFilePath = new("output")
                    {
                        Description = "A folder for the exported textures, or the name for a new .wad file with textures from .bsp",
                        Arity = ArgumentArity.ZeroOrOne
                    };
                    extract.Add(outputFilePath);

                    Option<string> inputPalettePath = new("--input-palette")
                    {
                        Description = "Path to the input .lmp palette (required when extracting textures from a .bsp file)"
                    };
                    extract.Add(inputPalettePath);

                    Option<string> outputPalettePath = new("--output-palette")
                    {
                        Description = "Path to the output .lmp palette (used when extracting a .wad file)"
                    };
                    extract.Add(outputPalettePath);

                    Option<bool> extractMipmaps = new("--mipmaps")
                    {
                        Description = "Extract mipmap levels as images"
                    };
                    extract.Add(extractMipmaps);

                    Option<bool> noFullbrightMasks = new("--nofullbright")
                    {
                        Description = "Do not extract fullbright masks"
                    };
                    extract.Add(noFullbrightMasks);

                    Option<bool> overwriteExistingFiles = new("--overwrite")
                    {
                        Description = "Extract mode only, enables overwriting of existing image files (off by default)"
                    };
                    extract.Add(overwriteExistingFiles);

                    Option<ImageFormat> outputImageFormat = new("--format")
                    {
                        Description = "Extracted images output format"
                    };
                    extract.Add(outputImageFormat);

                    Option<bool> extractAsIndexed = new("--indexed")
                    {
                        Description = "Extracted images are indexed and contain the original texture's palette. Only works with png, gif and bmp."
                    };
                    extract.Add(extractAsIndexed);

                    extract.SetAction(result =>
                    {
                        var input = result.GetRequiredValue(inputFilePath);
                        var output = result.GetValue(outputFilePath);

                        SetupLogging(result, input);

                        var inputExtension = Path.GetExtension(input);
                        if (inputExtension != null)
                        {
                            var inputIsBsp = inputExtension.Equals(".bsp", StringComparison.InvariantCultureIgnoreCase);
                            // If the output file is set to a .wad file, then we call a special extraction method
                            if (inputIsBsp && output != null && Path.GetExtension(output).Equals(".wad", StringComparison.InvariantCultureIgnoreCase))
                            {
                                TextureExtracting.ExtractEmbeddedTexturesToWad(input, result.GetRequiredValue(inputPalettePath), output, logger);
                            }
                            else
                            {
                                var outputFolder = output ?? Path.Combine(Path.GetDirectoryName(input) ?? "", $"{Path.GetFileNameWithoutExtension(input)}_extracted");
                                string? palette = null;
                                if (inputIsBsp)
                                {
                                    palette = result.GetRequiredValue(inputPalettePath);
                                }
                                else
                                {
                                    palette = result.GetValue(outputPalettePath) ?? Path.Combine(outputFolder, $"{Path.GetFileNameWithoutExtension(input)}.lmp");
                                }

                                var extractionSettings = new ExtractionSettings
                                {
                                    ExtractMipmaps = result.GetValue(extractMipmaps),
                                    NoFullbrightMasks = result.GetValue(noFullbrightMasks),
                                    OverwriteExistingFiles = result.GetValue(overwriteExistingFiles),
                                    OutputFormat = result.GetValue(outputImageFormat),
                                    SaveAsIndexed = result.GetValue(extractAsIndexed),
                                };
                                TextureExtracting.ExtractTextures(input, palette, outputFolder, extractionSettings, logger);
                            }
                        }
                    });

                    rootCommand.Subcommands.Add(extract);
                }
                #endregion

                #region Embed subcommand
                {
                    Command embed = new("embed", "Embed textures into a .bsp");

                    Argument<string> inputWadPath = new("input-wad")
                    {
                        Description = "The .wad with textures to be embedded",
                        Arity = ArgumentArity.ExactlyOne
                    };
                    embed.Add(inputWadPath);

                    Argument<string> inputBspPath = new("input-bsp")
                    {
                        Description = "The .bsp to embed the textures to",
                        Arity = ArgumentArity.ExactlyOne
                    };
                    embed.Add(inputBspPath);

                    Argument<string> outputBspPath = new("output")
                    {
                        Description = "The name of a new .bsp with embedded textures",
                        Arity = ArgumentArity.ZeroOrOne
                    };
                    embed.Add(outputBspPath);

                    embed.SetAction(result =>
                    {
                        var inputBsp = result.GetRequiredValue(inputBspPath);
                        SetupLogging(result, inputBsp);
                        TextureEmbedding.EmbedTextures(result.GetRequiredValue(inputWadPath), inputBsp, result.GetValue(outputBspPath) ?? inputBsp, logger);
                    });

                    rootCommand.Subcommands.Add(embed);
                }
                #endregion

                #region Strip subcommand
                {
                    Command strip = new("strip", "Removes all the textures embedded into .bsp");

                    Argument<string> inputBspPath = new("input")
                    {
                        Description = "The source .bsp",
                        Arity = ArgumentArity.ExactlyOne
                    };
                    strip.Add(inputBspPath);

                    Argument<string> outputBspPath = new("output")
                    {
                        Description = "Name of a new .bsp file with textures removed, the source file will be kept intact",
                        Arity = ArgumentArity.ZeroOrOne
                    };
                    strip.Add(outputBspPath);

                    strip.SetAction(result =>
                    {
                        var inputBsp = result.GetRequiredValue(inputBspPath);
                        SetupLogging(result, inputBsp);
                        TextureEmbedding.RemoveEmbeddedTextures(inputBsp, result.GetValue(outputBspPath) ?? inputBsp, logger);
                    });

                    rootCommand.Subcommands.Add(strip);
                }
                #endregion

                Option<bool> fullRebuild = new("--full")
                {
                    Description = "Forces a full rebuild instead of an incremental one"
                };
                rootCommand.Add(fullRebuild);

                Option<bool> includeSubdirs = new("--subdirs")
                {
                    Description = "Recursively include images in sub-directories"
                };
                rootCommand.Add(includeSubdirs);

                var parseResult = rootCommand.Parse(args);
                if (parseResult.Errors.Count > 0)
                {
                    foreach (var error in parseResult.Errors)
                    {
                        Console.Error.WriteLine(error);
                    }
                    return 1;
                }

                return parseResult.Invoke();

                var settings = new ProgramSettings()
                {
                    FullRebuild = parseResult.GetValue(fullRebuild),
                    IncludeSubDirectories = parseResult.GetValue(includeSubdirs),
                    //ExtractMipmaps = parseResult.GetValue(extractMipmaps),
                    //NoFullbrightMasks = parseResult.GetValue(noFullbrightMasks),
                    //OverwriteExistingFiles = parseResult.GetValue(overwriteExistingFiles),
                    //OutputImageFormat = parseResult.GetValue(outputImageFormat),
                    //ExtractAsIndexed = parseResult.GetValue(extractAsIndexed),
                    //RemoveEmbeddedTextures = parseResult.GetValue(removeEmbeddedTextures),
                    DisableFileLogging = parseResult.GetValue(disableFileLogging)
                };

                /*
                {
                    // Wad making requires a directory path, and optionally an output wad file path:
                    settings.InputDirectory = args[index++];

                    if (index < args.Length)
                        settings.OutputFilePath = args[index++];
                    else
                        settings.OutputFilePath = $"{Path.GetFileName(settings.InputDirectory)}.wad";

                    if (!Path.IsPathRooted(settings.OutputFilePath))
                        settings.OutputFilePath = Path.Combine(Path.GetDirectoryName(settings.InputDirectory) ?? "", settings.OutputFilePath);
                }
                */
                // TODO: Making WADs
                {
                    WadMaking.MakeWad(settings.InputDirectory!, settings.OutputFilePath!, settings.FullRebuild, settings.IncludeSubDirectories, logger);
                }
            }
            catch (InvalidUsageException ex)
            {
                Log($"ERROR: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.GetType().Name}: '{ex.Message}'.");
                Log(ex.StackTrace);
            }
            finally
            {
                LogFile?.Dispose();
            }

            // We would get here only after receiving an exception, so let's return a non-zero exit code
            return 1;
        }
    }
}
