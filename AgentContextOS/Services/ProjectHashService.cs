using System.Security.Cryptography;
using System.Text;

namespace AgentContextOS.Services;

public interface IProjectHashService
{
    string ComputeHash(string? projectPath = null);
}

public sealed class ProjectHashService(IConfiguration configuration) : IProjectHashService
{
    private readonly string _defaultPath = configuration.GetValue<string>("Acos:DefaultProjectPath")
        ?? Directory.GetCurrentDirectory();

    public string ComputeHash(string? projectPath = null)
    {
        var path = NormalizePath(projectPath ?? _defaultPath);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(path));
        return Convert.ToHexStringLower(bytes);
    }

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
