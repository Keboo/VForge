using System.Runtime.CompilerServices;

using HuggingfaceHub;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace VForge.Core;

public static class Summarizer
{
    public static async IAsyncEnumerable<string> SummarizeAsync(string text, [EnumeratorCancellation] CancellationToken token)
    {
        await EnsureDownloadedAsync(token);

        string modelPath = Path.GetFullPath(Path.Combine("phi3", "cpu_and_mobile", "cpu-int4-rtn-block-32-acc-level-4"));
        var builder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        builder.AddOnnxRuntimeGenAIChatCompletion("phi3", modelPath);
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var kernel = builder.Build();

        var response = kernel.InvokePromptStreamingAsync(
            promptTemplate: """
            You are an expert summarizer.
            Take the following text and summarize it.
            {{$input}}
            """,
            arguments: new KernelArguments()
            {
                { "input", text }
            },
            cancellationToken: token);

        await foreach (var message in response)
        {
            yield return message.ToString();
        }
    }

    private static async Task EnsureDownloadedAsync(CancellationToken token)
    {
        //TODO: Handle model updates
        var modelInfo = await HFDownloader.GetModelInfoAsync("microsoft/Phi-3-mini-4k-instruct-onnx");
        if (modelInfo is null)
        {
            throw new InvalidOperationException("Model not found");
        }
        if (Directory.Exists("phi3")) return;
        Directory.CreateDirectory("phi3");
        await HFDownloader.DownloadSnapshotAsync("microsoft/Phi-3-mini-4k-instruct-onnx", localDir: "phi3", allowPatterns: ["*/cpu-int4-rtn-block-32-acc-level-4/*"]);

    }
}
