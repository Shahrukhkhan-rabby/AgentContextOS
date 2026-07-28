# AGENT.md

## Project Overview
**AgentContextOS** is a local-first engineering memory layer built on **.NET 9 Web API** that provides persistent context for AI coding agents. It transforms Chat, Git, and Error signals into an **append-only event graph** stored securely in SQLite to maintain a deterministic project history. By leveraging **semantic and temporal retrieval**, the system injects relevant memory fragments into LLM prompts to eliminate redundant re-explaining. It strictly isolates context by project hash, ensuring the "cognitive layer" remains focused on the specific codebase. This architecture bridges the gap between transient chat messages and a cohesive, searchable stream of engineering consciousness.

This is a **.NET 9 Web API** project following clean architecture principles.
The goal is to build a scalable, maintainable, and testable RESTful API.

---

## Tech Stack

* .NET 9
* ASP.NET Core Web API
* Entity Framework Core
* SQL Server (or configurable DB)
* Minimal APIs / Controllers (depending on module)
* Dependency Injection (built-in)
* Logging: Serilog (if configured)

---

## Coding Guidelines

### General Rules

* Write **clean, readable, and maintainable code**
* Prefer **async/await** for all I/O operations
* Use **dependency injection** (no manual instantiation of services)
* Avoid hardcoding values → use configuration
* Follow **SOLID principles**

---

### Naming Conventions

* PascalCase → Classes, Methods, Properties
* camelCase → Variables, Parameters
* Interfaces → Prefix with `I` (e.g., `IUserService`)
* DTOs → Suffix with `Dto`
* Controllers → Suffix with `Controller`

---

### Project Structure

```
/Controllers
/Services
/Repositories
/Models
/DTOs
/Data
/Middlewares
/Configurations
```

---

## API Design Guidelines

### Controllers

* Keep controllers **thin**
* Only handle:

    * Request validation
    * Calling services
    * Returning responses

### Services

* Contain **business logic**
* Should be testable
* No direct HTTP context usage

### Repositories

* Handle **data access only**
* Use EF Core properly (no business logic here)

---

## 🔹 API Response Format (JSON Standard)

All API responses **must follow a consistent structure** for success and error handling.

### ✅ Success Response

```json
{
  "success": true,
  "statusCode": 200,
  "message": "Request successful",
  "data": {}
}
```

**Rules:**

* `success`: always `true`
* `statusCode`: HTTP status code (200, 201, etc.)
* `message`: short human-readable message
* `data`: actual response payload (object, array, or null)

---

### ❌ Error Response

```json
{
  "success": false,
  "statusCode": 400,
  "message": "Validation failed",
  "errors": [
    {
      "field": "email",
      "message": "Email is required"
    }
  ]
}
```

**Rules:**

* `success`: always `false`
* `statusCode`: HTTP error code (400, 401, 404, 500, etc.)
* `message`: general error summary
* `errors`: optional, detailed validation or business errors

---

### 🔸 Pagination Response (if applicable)

```json
{
  "success": true,
  "statusCode": 200,
  "message": "Data fetched successfully",
  "data": {
    "items": [],
    "page": 1,
    "pageSize": 10,
    "totalCount": 100,
    "totalPages": 10
  }
}
```

---

### 🔸 Example (.NET 9 Minimal API / Controller)

```csharp
return Results.Ok(new {
    success = true,
    statusCode = 200,
    message = "User retrieved successfully",
    data = user
});
```

```csharp
return Results.BadRequest(new {
    success = false,
    statusCode = 400,
    message = "Validation failed",
    errors = new [] {
        new { field = "email", message = "Email is required" }
    }
});
```

---

### 🔸 Recommended: Strongly Typed Wrapper

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public object Errors { get; set; }
}
```

---

### 🔸 Best Practices (API Response)

* Never return raw objects directly from controllers
* Always wrap responses using the standard format
* Keep `message` short and client-friendly
* Use `errors` only when needed (validation, business rules)
* Log internal errors separately (don’t expose stack traces)

---


## Logging

* Log:

    * Errors
    * Warnings
    * Important business events
* Do NOT log sensitive data

---

## Validation

* Use **FluentValidation** or built-in validation
* Validate all incoming requests
* Never trust client input

---

## Security Practices

* Use HTTPS only
* Implement authentication (JWT if applicable)
* Validate all inputs to prevent:

    * SQL Injection
    * XSS
* Never expose internal exceptions to clients

---

## Performance Guidelines

* Use `AsNoTracking()` for read-only queries
* Avoid unnecessary database calls
* Use pagination for large datasets
* Prefer `IQueryable` over `IEnumerable` when querying DB

---

## Testing

* Write unit tests for:

    * Services
    * Business logic
* Mock dependencies
* Avoid testing controllers directly unless needed

---

## Copilot Instructions

When generating code, ALWAYS:

* Follow project structure
* Use async methods
* Apply dependency injection
* Keep controllers thin
* Use DTOs instead of exposing entities
* Add proper null checks
* Handle exceptions properly
* Generate production-ready code (not demo code)

---

## Example Pattern

### Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        return Ok(result);
    }
}
```

---

### Service

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto> GetByIdAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user == null ? null : new UserDto(user);
    }
}
```

---

## What to Avoid

* Fat controllers
* Business logic inside repositories
* Blocking calls (`.Result`, `.Wait()`)
* God classes
* Copy-paste code

---

---

## 🤖 MCP Server — Agent Connection Guide

AgentContextOS exposes a **Model Context Protocol (MCP)** server at `http://localhost:5199/mcp` using HTTP Streamable transport. AI agents connect to it to read and write engineering memory natively.

### Available MCP Tools

| Tool | Description |
|------|-------------|
| `get_memory` | Retrieves past decisions, commits, and errors relevant to the current query |
| `record_session` | Archives session summary and key decisions into permanent memory |

### Start the server

```bash
cd AgentContextOS
dotnet run --no-launch-profile --urls "http://localhost:5199"
```

### Connect Claude Code

Create or edit `~/.claude/mcp.json`:

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

### Connect GitHub Copilot CLI

```bash
gh copilot mcp add http://localhost:5199/mcp
```

### Recommended System Prompt for Agents

> "You are equipped with the **AgentContextOS** toolset.
> 1. **Before** starting any technical task, call `get_memory` with the current prompt to retrieve relevant past decisions.
> 2. **After** finishing a task, call `record_session` to archive what was done and why. This is your long-term memory."

### Notes

- Tools auto-discover the project via `projectPath` (absolute path to repo root).
- The MCP session lifecycle: first call must be `initialize` → subsequent calls require the returned `Mcp-Session-Id` header.
- All data stays local at `~/.acos/acos.db`. Nothing leaves the machine.

