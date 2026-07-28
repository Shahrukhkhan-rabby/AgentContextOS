using AgentContextOS.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.ML.Tokenizers;

namespace AgentContextOS.Services;

public interface ITokenBudgetService
{
    int Budget { get; }
    int CountTokens(string text);
    string TrimToBudget(string text, int? budgetOverride = null);
}

public sealed class TokenBudgetService : ITokenBudgetService
{
    private readonly Tokenizer _tokenizer;
    private readonly int _budget;

    public TokenBudgetService(IOptions<AcosOptions> options)
    {
        _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
        _budget = options.Value.TokenBudget;
    }

    public int Budget => _budget;

    public int CountTokens(string text) =>
        _tokenizer.CountTokens(text);

    public string TrimToBudget(string text, int? budgetOverride = null)
    {
        var limit = budgetOverride ?? _budget;
        var count = CountTokens(text);

        if (count <= limit)
            return text;

        // Binary search for the longest prefix that fits within budget
        var lines = text.Split('\n');
        var lo = 0;
        var hi = lines.Length;

        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            var candidate = string.Join('\n', lines[..mid]);
            if (CountTokens(candidate) <= limit)
                lo = mid;
            else
                hi = mid - 1;
        }

        if (lo == 0)
            return string.Empty;

        return string.Join('\n', lines[..lo]);
    }
}
