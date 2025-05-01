using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System.Diagnostics;

var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "DotnetMcpServer",
    Command = "dotnet",
    Arguments = ["run", "--project", "../MCPserverDemo/MCPserverDemo.csproj"], // adjust path if needed
});

var client = await McpClientFactory.CreateAsync(clientTransport);



#region Execute a tool (this would normally be driven by LLM tool invocations) : Execute the MCP Server Echo Tool that always returns one and only one text content object

var result = await client.CallToolAsync(
    "Echo",
    new Dictionary<string, object?>() { ["message"] = "\nYou are now connect to the MCP Servers!\nHere is the list of Tools available for you to call.\n" },
    cancellationToken: CancellationToken.None);
    Console.WriteLine(result.Content.First(c => c.Type == "text").Text);

#endregion

#region Print the list of tools available from the server.

var tools = (await client.ListToolsAsync()).ToList();

if (!tools.Any())
{
    Console.WriteLine("No tools available from the server.");
}
else
{
    // Calculate column widths
    int nameWidth = Math.Max("Tool Name".Length, tools.Max(t => t.Name.Length));
    int descWidth = Math.Max("Description".Length, tools.Max(t => t.Description.Length));

    // Build the format string
    string rowFormat = $"| {{0,-{nameWidth}}} | {{1,-{descWidth}}} |";

    // Print header
    Console.WriteLine(string.Format(rowFormat, "Tool Name", "Description"));
    Console.WriteLine($"|{new string('-', nameWidth + 2)}|{new string('-', descWidth + 2)}|");

    // Print each row
    foreach (var tool in tools)
    {
        Console.WriteLine(string.Format(rowFormat, tool.Name, tool.Description));
    }
}

#endregion

#region Execute MCP Server Tool TranslateToEnglish

Console.WriteLine($"\nNow calling the MCP server TranslateToEnglish Tool...\n");

// Define input text
string hindiText = "يمكنك توفير موارد OpenAI باستخدام مدخل Azure أو CLI ونشر نموذجها واستخدامه في Azure AI Studio أو في Azure AI Foundry المتاح الآن بشكل شائع";

// Call the translation tool
var MCPresult = await client.CallToolAsync(
    "TranslateToEnglish",
    new Dictionary<string, object?>() { ["inputText"] = hindiText },
    cancellationToken: CancellationToken.None);

// Get translated text
string translatedText = MCPresult.Content.FirstOrDefault(c => c.Type == "text")?.Text ?? "[No translation]";

// Calculate column widths
string header1 = "Language";
string header2 = "Text";
int col1Width = header1.Length;
int col2Width = Math.Max(header2.Length, Math.Max(hindiText.Length, translatedText.Length));

// Format string
string rowFormat2 = $"| {{0,-{col1Width}}} | {{1,-{col2Width}}} |";

// Print the Markdown-style table
Console.WriteLine(string.Format(rowFormat2, header1, header2));
Console.WriteLine($"|{new string('-', col1Width + 2)}|{new string('-', col2Width + 2)}|");
Console.WriteLine(string.Format(rowFormat2, "Arabic", hindiText));
Console.WriteLine(string.Format(rowFormat2, "English", translatedText));

Console.WriteLine("\n\n");

#endregion




