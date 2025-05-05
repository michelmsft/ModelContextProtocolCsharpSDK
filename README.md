# 🧠 Example of implementation of Model Context Protocol (MCP) Server and Client using .NET C# SDK

This repo demonstrates how to use the **Model Context Protocol (MCP)** in .NET to expose a set of AI tools via an MCP server interface that can be consumed by LLMs or AI-powered applications.

## ✨ What is MCP?

The **Model Context Protocol (MCP)** is an open standard designed to allow Large Language Models (LLMs) to invoke tools (functions, APIs, or plugins) in a standardized way across multiple platforms.

## 🚀 MCP Server Overview

The `MCPserverDemo` project in this repo shows how to:

- Spin up an **MCP server** using `.NET`
- Register tools from your own C# assembly
- Expose them over `stdio` for easy local development
- Interact with the server from a client using the `MCP` client library

You will install in both your server and client the latest preview version of the MCP SDK from NuGet:
```powershell
Install-Package ModelContextProtocol -Version 0.1.0-preview.11
```

## 🔧 Tools Provided

The MCP server defines the following example tools in `McpServerToolBox Class`:

| Tool Name              | Description                                     |
|------------------------|-------------------------------------------------|
| `Echo`                 | Echoes a message back to the client             |
| `SummarizeContentFromUrl` | Uses Azure AI to summarize web content          |
| `TranslateToEnglish`   | Translates input text into English              |

These tools demonstrate how to wrap external services (like Azure AI Language or Translator) and expose them for use by LLMs.

## 🛠 MCP Server Code Snippet

The MCP server is created as follows in the `MCPserverDemo.csproj` project :

```csharp
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(); // Automatically loads all tools marked with [McpServerTool]
try
{
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[MCP Server Fatal Error] {ex}");
}
```
### ✅ Sample Output
![image](https://github.com/user-attachments/assets/ee243c97-3d73-49fc-a353-f836bd5433bf)

## 📡 Using the MCP Client

The `MCPClientDemo` project shows how to connect to the MCP server using a `StdioClientTransport` and interact with the available tools.

### 🔍 List Available Tools

You can retrieve and display the available tools exposed by the MCP server:

```csharp
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
```
### ⚙️ Invoke a Tool
To call a tool, simply use its name and provide the required parameters:
```csharp
var result = await client.CallToolAsync(
    "Echo",
    new Dictionary<string, object?>() { ["message"] = "\nYou are now connect to the MCP Servers!\nHere is the list of Tools available for you to call.\n" },
    cancellationToken: CancellationToken.None);
    Console.WriteLine(result.Content.First(c => c.Type == "text").Text);
```
### ✅ Sample Output
![image](https://github.com/user-attachments/assets/fdcdbbaa-d505-4120-854a-6e662e70a1ab)

## 🏁 Running the Demo Locally
1. Clone the repository:

```bash
git clone https://github.com/michelmsft/ModelContextProtocolCsharpSDK.git
cd ModelContextProtocolCsharpSDK
```
please make sure to replace in your MCP Server Code the LanguageKey and LanguageEnpoint.

2. Start the MCP Server:
```bash
dotnet run --project ./MCPserverDemo/MCPserverDemo.csproj
```
3. Run the Client to interact with the server:

```bash
dotnet run --project ./MCPdemoClient/MCPClientDemo.csproj
```


