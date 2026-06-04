namespace FUNewsTradingSystem_BusinessLayer.Services
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int? EntityId { get; set; }
    }
}
