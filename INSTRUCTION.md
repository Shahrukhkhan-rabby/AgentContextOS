# 🧠 Project Brief: AgentContextOS (Service Edition)
**AgentContextOS** is a local-first, persistent engineering memory layer designed to bridge the gap between transient developer intent and LLM execution. Built as a high-performance **.NET 9 Web API**, it serves as a central "cognitive layer" that connects chat history, Git activity, and system errors into a unified, searchable event graph.
**Vision:** A local-first, API-driven context layer that serves as a persistent "Engineering Memory" service for AI-native workflows.
**Architecture:** A lightweight ASP.NET Core service running on `localhost` that captures, indexes, and retrieves engineering context via REST endpoints.
  
---

### 🎯 Core Objectives
* **Persistent Engineering Memory:** Provide AI agents (like Claude Code or Aider) with a long-term memory of architectural decisions, past bugs, and developer intent.
* **Zero-Friction Ingestion:** Automatically capture context from Git logs and CLI interactions without manual documentation.
* **Local Sovereignty:** Ensure all data and embeddings remain on the developer's machine, prioritizing privacy and ultra-low latency.

---

### 🏗️ Architectural Pillars
* **The Event-Driven Model:** Unlike standard RAG that indexes static documents, AgentContextOS indexes **Context Events**—discrete fragments of time-stamped activity (Chat, Commit, or Error).
* **Append-Only Persistence:** Uses a "truth layer" powered by **SQLite**. Data is never mutated, preserving the integrity of the original engineering reasoning history.
* **Project Isolation:** Uses deterministic hashing of repository paths to ensure memory remains contextually isolated, preventing "cognitive leakage" between different codebases.

---

### 🔍 Intelligence & Retrieval
The system moves beyond simple vector search by using **Context Graph Expansion**:
1.  **Semantic Search:** Locates fragments related to the current query.
2.  **Temporal Expansion:** Automatically retrieves events that occurred immediately before or after a relevant match.
3.  **File-Path Linking:** Connects chat discussions to the specific commits and code changes mentioned within the same time window.

---

### 🚀 MVP Scope
The initial version focuses on a background service that watches the current repository, ingests Git history, and provides a `/compose` endpoint. This endpoint takes a raw user prompt and returns an **"Enriched Prompt"**—a Markdown-formatted brief containing the most relevant "memory fragments" for the LLM to process.

> **The Vision:** AgentContextOS isn't just a tool; it's the missing infrastructure for AI-native software engineering.
---

# 🛠️ Technical Specifications
| Component | Technology |
| :--- | :--- |
| **Runtime** | .NET 9 ASP.NET Core (Minimal APIs) |
| **Persistence** | SQLite with EF Core 9 |
| **Vector Search** | `Microsoft.Extensions.VectorData` + `sqlite-vec` |
| **AI Integration** | `Microsoft.Extensions.AI` (Ollama/Local Embeddings) |
| **Background Tasks**| `IHostedService` / `BackgroundService` (for Git watching) |
| **Serialization** | System.Text.Json (Source Generated for performance) |
| **Agent Protocol** | `ModelContextProtocol.AspNetCore` (HTTP Streamable MCP) |

---

# 🏗️ Phase-by-Phase Implementation Backlog

### Phase 1: The Service Foundation
* **Goal:** Boot up the Web API and the storage engine.
* **Tasks:**
    * Create an ASP.NET Core Minimal API project.
    * **Project Tracking:** Implement a service to detect the current working directory's Git hash to scope memory.
    * **Storage:** Setup `AcosDbContext` with SQLite. Define the `ContextEvent` entity (Id, Type, Content, Timestamp, Metadata, ProjectHash).
    * **Endpoint:** `POST /events` – Basic ingestion endpoint.

### Phase 2: Semantic Intelligence (Embeddings)
* **Goal:** Enable the API to "understand" the meaning of events.
* **Tasks:**
    * Integrate `Microsoft.Extensions.AI` to point to a local Ollama instance.
    * **Vector Storage:** Configure the SQLite provider to handle vector columns using `sqlite-vec`.
    * **Middleware:** Create an `EmbeddingTransformationService` that automatically generates vectors for incoming `ContextEvent` requests before saving.

### Phase 3: The Git Pulse (Background Ingestion)
* **Goal:** Automatically sync repository history without user intervention.
* **Tasks:**
    * **Worker Service:** Implement a `BackgroundService` that polls the local Git log or uses file system watchers.
    * **Mapping:** Convert commits into `CommitEvents` and batch-insert them into the API.
    * **Endpoint:** `POST /sync/git` – Manual trigger to force a repo re-index.

### Phase 4: The Graph Retrieval Engine
* **Goal:** The core intelligence endpoint.
* **Tasks:**
    * **Endpoint:** `GET /context?query=...` 
    * **Search Logic:** 1.  Perform a vector search for the top 5 matches.
        2.  **Temporal Expansion:** Query for events ±10 minutes from those matches.
        3.  **Project Isolation:** Ensure results only come from the current `ProjectHash`.
    * **Ranking:** Sort by a weighted combination of similarity and recency.

### Phase 5: The Context Composer
* **Goal:** Turn data into the "Golden Prompt."
* **Tasks:**
    * **Composition Logic:** A service that takes the retrieved graph and assembles it into a Markdown-formatted engineering brief.
    * **Endpoint:** `POST /compose` – Takes a raw user prompt and returns the "Enriched" version ready for an LLM.
    * **Token Guard:** Integrate `Microsoft.ML.Tokenizers` to ensure the composed output stays within a defined context budget (e.g., 8k tokens).

### Phase 6: The MCP Server (Agent Native Interface)
* **Goal:** Expose AgentContextOS as a first-class **Model Context Protocol (MCP)** server so AI agents (Claude Code, GitHub Copilot CLI) can natively read and write engineering memory without any REST boilerplate.
* **Transport:** HTTP Streamable MCP via `ModelContextProtocol.AspNetCore` — single endpoint at `/mcp`, auto-discovered by agents.
* **Tasks:**
    * **NuGet:** Add `ModelContextProtocol.AspNetCore`.
    * **MCP Tools:** Create `Mcp/McpContextTools.cs` with two tools using constructor DI:
        * `RecordSession(summary, decisions[], projectPath)` — ingests a `Chat` event via `IEventService`
        * `GetMemory(userQuery, projectPath)` — returns the enriched Markdown brief via `IContextComposerService`
    * **Registration:** `AddMcpServer().WithHttpTransport().WithTools<McpContextTools>()` in service extensions; `app.MapMcp("/mcp")` in app extensions.
    * **Agent Config:** Document `~/.claude/mcp.json` (Claude Code) and Copilot CLI config pointing to `http://localhost:5199/mcp`.

---

# 📝 Instructions for the Coding Agent

> **Role:** Lead .NET 9 Backend Engineer.
> **Project:** AgentContextOS (Local Context Service).
>
> **Standard Operating Procedures:**
> 1.  Use **Minimal APIs** for all REST endpoints.
> 2.  Use **Primary Constructors** and **C# 13 features** (e.g., params collections).
> 3.  Implement a **Local Storage Directory** at `~/.acos/` for the SQLite DB.
> 4.  Use **Dependency Injection** for the `IEmbeddingGenerator` and `IVectorStore`.
> 5.  For MCP tools, use constructor DI and `[Description]` on every method and parameter.

---

# 🤖 Phase 6: MCP Server — Implementation Reference

## NuGet

```bash
dotnet add package ModelContextProtocol.AspNetCore
```

## MCP Tools (`Mcp/McpContextTools.cs`)

```csharp
using System.ComponentModel;
using AgentContextOS.DTOs;
using AgentContextOS.Models;
using AgentContextOS.Services;
using ModelContextProtocol.Server;

namespace AgentContextOS.Mcp;

[McpServerToolType]
public class McpContextTools(
    IEventService eventService,
    IContextComposerService composerService,
    IProjectHashService projectHashService)
{
    [McpServerTool, Description("Saves the AI agent's session summary, decisions, and outcomes into permanent engineering memory.")]
    public async Task<string> RecordSession(
        [Description("High-level summary of what was accomplished in this session")] string summary,
        [Description("List of key decisions made (e.g. 'Use SQLite', 'Chose append-only model')")] string[] decisions,
        [Description("Absolute path to the project repository")] string projectPath,
        CancellationToken cancellationToken = default)
    {
        var content = $"Summary: {summary}\nDecisions: {string.Join(", ", decisions)}";
        var projectHash = projectHashService.ComputeHash(projectPath);
        await eventService.IngestAsync(new CreateEventRequestDto(ContextEventType.Chat, content), projectHash, cancellationToken);
        return "Context successfully archived to AgentContextOS.";
    }

    [McpServerTool, Description("Retrieves relevant past decisions, commits, and errors to enrich the current user prompt with engineering memory.")]
    public async Task<string> GetMemory(
        [Description("The current user query or task description to find relevant context for")] string userQuery,
        [Description("Absolute path to the project repository")] string projectPath,
        CancellationToken cancellationToken = default)
    {
        var projectHash = projectHashService.ComputeHash(projectPath);
        var result = await composerService.ComposeAsync(userQuery, projectHash, cancellationToken);
        return result.EnrichedPrompt;
    }
}
```

## Registration (`Extensions/ServiceCollectionExtensions.cs`)

```csharp
public static IServiceCollection AddAcosMcp(this IServiceCollection services)
{
    services.AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "AgentContextOS", Version = "1.0.0" };
        options.ServerInstructions = "Engineering memory layer. Use GetMemory before tasks, RecordSession after.";
    })
    .WithHttpTransport()
    .WithTools<McpContextTools>();
    return services;
}
```

## Endpoint mapping (`Extensions/WebApplicationExtensions.cs`)

```csharp
app.MapMcp("/mcp");  // Streamable HTTP at http://localhost:5199/mcp
```

## Connecting Agents

### Claude Code — `~/.claude/mcp.json`
```json
{
  "mcpServers": {
    "agent-context": {
      "type": "http",
      "url": "http://localhost:5199/mcp"
    }
  }
}
```

### GitHub Copilot CLI
```bash
gh copilot mcp add http://localhost:5199/mcp
```

## System Prompt for Agents
> "You are equipped with the **AgentContextOS** toolset.
> 1. **Before** starting any technical task, call `GetMemory` with the current prompt to retrieve relevant past decisions.
> 2. **After** finishing a task, call `RecordSession` to archive what was done and why. This is your long-term memory."

