namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    /// <summary>
    /// Contract for the AI trading analysis pipeline.
    /// Implementations orchestrate news fetching, multi-step LLM analysis,
    /// response validation, and report persistence in a single atomic operation.
    /// </summary>
    public interface ITradingAgentService
    {
        /// <summary>Runs the full pipeline for the given ticker and sector.</summary>
        /// <inheritdoc cref="FUNewsTradingSystem_BusinessLayer.Services.Implements.TradingAgentService.RunAnalysisAsync"/>
        Task<TradingAgentResult> RunAnalysisAsync(int tagId, int categoryId, int createdByAccountId, System.Func<string, int, System.Threading.Tasks.Task>? onProgress = null);
    }
}
