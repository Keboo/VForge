using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Octokit;

namespace VForge.Core;

public class Ffmpeg
{
    private static HttpClient HttpClient { get; } = new();

    public static async Task<FileInfo> ExtractAudioAsync(FileInfo videoFile, CancellationToken token)
    {
        await EnsureDownloadedAsync(token);

        FileInfo outputFile = new("output.wav");
        Process ffmpeg = new()
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = Path.GetFullPath("ffmpeg"),
                FileName = "ffmpeg.exe",
                Arguments = $"-i \"{videoFile.FullName}\" -y -vn -ac 1 -ar 16000 -f wav -acodec pcm_s16le \"{outputFile.FullName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
        ffmpeg.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
        ffmpeg.ErrorDataReceived += (sender, args) => Console.Error.WriteLine(args.Data);

        ffmpeg.Start();

        ffmpeg.BeginOutputReadLine();
        ffmpeg.BeginErrorReadLine();

        await ffmpeg.WaitForExitAsync(token);

        outputFile.Refresh();
        if (!outputFile.Exists)
        {
            throw new InvalidOperationException("FFmpeg failed to extract audio");
        }
        return outputFile;
    }

    private static async Task EnsureDownloadedAsync(CancellationToken token)
    {
        if (File.Exists("ffmpeg/ffmpeg.exe")) return;

        var client = new GitHubClient(new ProductHeaderValue("VForge"));
        var latestRelease = await client.Repository.Release.GetLatest("BtbN", "FFmpeg-Builds");

        if (latestRelease != null)
        {
            //TODO: Handle non-windows
            var assets = latestRelease.Assets
                .Where(x => x.Name.EndsWith("ffmpeg-master-latest-win64-lgpl.zip", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (assets.FirstOrDefault() is { } asset)
            {
                using var stream = await HttpClient.GetStreamAsync(asset.BrowserDownloadUrl);
                using var fileStream = File.Create("ffmpeg.zip");
                await stream.CopyToAsync(fileStream, token);
                fileStream.Position = 0;
                Directory.CreateDirectory("ffmpeg");
                ZipArchive archive = new(fileStream);
                foreach (var entry in archive.Entries.Where(x => x.FullName.Contains("bin/") && x.Length > 0))
                {
                    entry.ExtractToFile(Path.Combine("ffmpeg", entry.Name), true);
                }
            }
        }
    }


}
