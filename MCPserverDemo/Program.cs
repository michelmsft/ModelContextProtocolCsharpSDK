using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol.Transport;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

// Configure all logs to go to stderr

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
try
{
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[MCP Server Fatal Error] {ex}");
}


[McpServerToolType]
public static class McpServerToolBox
{
    static string languageKey = "AI_LANGUAGE_SERVICE_KEY";
    static string languageEndpoint = "AI_LANGUAGE_SERVICE_ENDPOINT";
    private static readonly AzureKeyCredential credentials = new AzureKeyCredential(languageKey);
    private static readonly Uri endpoint = new Uri(languageEndpoint);

    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";

    [McpServerTool(Name = "SummarizeContentFromUrl"), Description("Summarizes content from a given URL.")]
    public static async Task<string> SummarizeContentFromUrl(
        IMcpServer thisServer,
        [Description("The URL of the content to summarize.")] string url,
        CancellationToken cancellationToken)
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("SummarizeContentFromUrl");

        try
        {
            logger.LogInformation("Starting to download content from: {Url}", url);

            // Download content from the URL
            using var httpClient = new HttpClient();
            string content = await httpClient.GetStringAsync(url, cancellationToken);

            // Log the content length
            logger.LogInformation("Downloaded content of length: {Length}", content.Length);

            // Prepare analyze operation input
            var batchInput = new List<string> { content };
        
            TextAnalyticsActions actions = new TextAnalyticsActions()
            {
                ExtractiveSummarizeActions = new List<ExtractiveSummarizeAction> { new ExtractiveSummarizeAction() }
            };

            // Create client and start the analysis process
            var client = new TextAnalyticsClient(endpoint, credentials);

            // Start the operation and wait for completion
            AnalyzeActionsOperation operation = await client.StartAnalyzeActionsAsync(batchInput, actions);
            await operation.WaitForCompletionAsync(cancellationToken);  // Pass CancellationToken here for wait operation

            string docSummary = string.Empty;

            // View operation results
            await foreach (AnalyzeActionsResult documentsInPage in operation.Value)
            {
                IReadOnlyCollection<ExtractiveSummarizeActionResult> summaryResults = documentsInPage.ExtractiveSummarizeResults;

                foreach (ExtractiveSummarizeActionResult summaryActionResults in summaryResults)
                {
                    if (summaryActionResults.HasError)
                    {
                        logger.LogError($"  Error! Action error code: {summaryActionResults.Error.ErrorCode}.");
                        continue;
                    }

                    foreach (ExtractiveSummarizeResult documentResults in summaryActionResults.DocumentsResults)
                    {
                        if (documentResults.HasError)
                        {
                            logger.LogError($"  Error! Document error code: {documentResults.Error.ErrorCode}.");
                            continue;
                        }

                        // Append the extracted sentences to the summary
                        foreach (ExtractiveSummarySentence sentence in documentResults.Sentences)
                        {
                            docSummary += "\n" + sentence.Text;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(docSummary))
            {
                docSummary = "No summary could be extracted.";
            }

            return docSummary;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error during content fetch: {Url}", url);
            return $"Error: Unable to download content from '{url}'.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during content summarization");
            return "Error: An unexpected error occurred.";
        }
    }

    [McpServerTool(Name = "TranslateToEnglish"), Description("Translates input text into English.")]
    public static async Task<string> TranslateToEnglish(
        IMcpServer thisServer,
        [Description("The text to translate.")] string inputText,
        CancellationToken cancellationToken)
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("TranslateToEnglish");
        try
        {
            logger.LogInformation("Starting translation to English...");

            // Prepare HttpClient
            using var httpClient = new HttpClient();

            // Add required headers
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", languageKey);
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", "eastus");

            var endpoint = "https://api.cognitive.microsofttranslator.com";
            var route = "/translate?api-version=3.0&to=en";

            // Construct request body
            var requestBody = new object[] { new { Text = inputText } };
            var requestContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            // Send POST request
            var response = await httpClient.PostAsync($"{endpoint}{route}", requestContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Parse response
            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            var translatedText = doc.RootElement[0].GetProperty("translations")[0].GetProperty("text").GetString();

            logger.LogInformation("Translation successful.");

            return translatedText ?? "Translation failed.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Translation failed.");
            return "Error: Translation failed.";
        }
    }

}


