using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Whisper.net;
using Whisper.net.Ggml;

namespace VForge.Core;

public static class Transcribe
{
    private const string WhisperDirectory = "whisper";
    private const string ModelFileName = "model.bin";
    private static string ModelPath { get; } = Path.Combine(WhisperDirectory, ModelFileName);

    public static async IAsyncEnumerable<string> TranscribeAsync(FileInfo wavFile, [EnumeratorCancellation] CancellationToken token)
    {
        await EnsureModelDownloaded(token);

        using var wavStream = wavFile.OpenRead();
        WhisperFactory factory = WhisperFactory.FromPath(ModelPath);
        WhisperProcessor processor = factory.CreateBuilder().WithLanguage("auto").Build();
        await foreach (SegmentData segmentData in processor.ProcessAsync(wavStream, token))
        {
            yield return segmentData.Text;
        }
    }

    private static async Task EnsureModelDownloaded(CancellationToken token)
    {
        if (!File.Exists(ModelPath))
        {
            Directory.CreateDirectory(WhisperDirectory);
            using Stream modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.SmallEn, cancellationToken: token);
            using FileStream fileStream = File.Create(ModelPath);
            await modelStream.CopyToAsync(fileStream, token);
        }
    }
}
