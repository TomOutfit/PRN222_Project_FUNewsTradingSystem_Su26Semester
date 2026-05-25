namespace FUNewsTradingSystem_DataAccessLayer.Models;

public class SystemAccount
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountEmail { get; set; } = string.Empty;
    public int AccountRole { get; set; }
    public string AccountPassword { get; set; } = string.Empty;
}
