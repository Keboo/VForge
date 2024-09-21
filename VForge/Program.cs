using System.CommandLine;
using System.Text;

using VForge.Core;

namespace VForge;

public sealed class Program
{
    private static Task<int> Main(string[] args)
    {
        CliConfiguration configuration = GetConfiguration();
        return configuration.InvokeAsync(args);
    }

    public static CliConfiguration GetConfiguration()
    {
        CliArgument<FileInfo> inputFileArgument = new("input-file")
        {
            Description = "The file to transcribe",
            Arity = ArgumentArity.ExactlyOne
        };
        CliCommand transcribeCommand = new("transcribe", "Add two numbers together")
        {
            inputFileArgument
        };
        transcribeCommand.SetAction(async (ParseResult parseResult, CancellationToken token) =>
        {
            try
            {
                FileInfo? inputFile = parseResult.CommandResult.GetValue(inputFileArgument);
                if (inputFile is null)
                {
                    throw new InvalidOperationException("No input file provided");
                }
                FileInfo audioFile = await Ffmpeg.ExtractAudioAsync(inputFile, token);
                StringBuilder sb = new();
                await foreach (var line in Transcribe.TranscribeAsync(audioFile, token))
                {
                    await parseResult.Configuration.Output.WriteLineAsync(line);
                    sb.Append(line).Append(' ');
                }

                await parseResult.Configuration.Output.WriteLineAsync("");
                await parseResult.Configuration.Output.WriteLineAsync("---");
                await parseResult.Configuration.Output.WriteLineAsync("SUMMARY");
                await parseResult.Configuration.Output.WriteLineAsync("---");
                await parseResult.Configuration.Output.WriteLineAsync("");

                await foreach (var summary in Summarizer.SummarizeAsync(sb.ToString(), token))
                {
                    await parseResult.Configuration.Output.WriteAsync(summary);
                }
            }
            catch (Exception ex)
            {
                parseResult.Configuration.Output.WriteLine(ex.Message);
            }
            //parseResult.Configuration.Output.WriteLine($"The result is {result}");
        });

        CliRootCommand rootCommand = new("A starter console app by Keboo")
        {
            transcribeCommand
        };
        return new CliConfiguration(rootCommand);
    }
}