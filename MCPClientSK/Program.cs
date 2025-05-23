﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System.ComponentModel;
using System.Threading;



// === SETUP: Azure OpenAI Chat Completion ===
var azureOpenAiService = new AzureOpenAIChatCompletionService(
    deploymentName: "AZURE_OPENAI_MODEL_DEPLOYMENT_NAME", // Your deployment name
    endpoint: "AZURE_OPENAI_ENDPOINT",
    apiKey: "AZURE_OPENAI_KEY"
);


// Create MCP Client
var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "DotnetMcpServer",
    Command = "dotnet",
    Arguments = ["run", "--project", "../MCPserverDemo/MCPserverDemo.csproj"], // adjust path if needed
});


var mcpClient = await McpClientFactory.CreateAsync(clientTransport);

// Create Semantic Kernel
var builder = Kernel.CreateBuilder();
builder.Services.AddSingleton<IChatCompletionService>(azureOpenAiService);
builder.Services.AddSingleton(mcpClient);
builder.Plugins.AddFromType<McpPlugins>();
var kernel = builder.Build();



var result = await mcpClient.CallToolAsync(
    "Echo",
    new Dictionary<string, object?>() { ["message"] = "You are now connected to the MCP Server!\n\n" },
    cancellationToken: CancellationToken.None);

Console.WriteLine(result.Content.First(c => c.Type == "text").Text);



// === CHAT LOOP ===

#region Enable planning

AzureOpenAIPromptExecutionSettings settings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    Temperature = 0.3
};

#endregion



var chat = kernel.GetRequiredService<IChatCompletionService>();
var history = new ChatHistory("You are a helpful assistant. Call tools when needed.");

Console.WriteLine("Type 'exit' to quit.\n");

while (true)
{
    Console.Write($"You :");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    history.AddUserMessage(input);

    var reply = await chat.GetChatMessageContentAsync(
        history,
        settings,
        kernel
    );

    history.AddMessage(reply.Role, reply.Content ?? string.Empty);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\nAssistant: {reply?.Content}\n");
    Console.ResetColor();
}



public class McpPlugins
{
    private readonly IMcpClient _mcpClient;

    public McpPlugins(IMcpClient mcpClient)
    {
        _mcpClient = mcpClient;
    }


    [KernelFunction("TranslateInEnglish"), Description("Always translate the input to English if it's not already in English.")]
    public async Task<string> DetectAndTranslateIfNeeded(
        [Description("Text that may not be in English. Always try to translate.")] string inputPrompt,
        CancellationToken cancellationToken = default)
    {

        var MCPresult = await _mcpClient.CallToolAsync(
                "TranslateToEnglish",
                new Dictionary<string, object?> { ["inputText"] = inputPrompt },
                cancellationToken: CancellationToken.None);

            return MCPresult.Content.FirstOrDefault(c => c.Type == "text")?.Text ?? "[No text content]";

    }



}

