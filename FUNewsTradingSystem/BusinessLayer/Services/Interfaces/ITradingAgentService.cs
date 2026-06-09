namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public interface ITradingAgentService
    {
        Task<TradingAgentResult> RunAnalysisAsync(int tagId, int categoryId, int createdByAccountId);
    }
}
