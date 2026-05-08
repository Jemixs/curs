namespace SportClub.Domain;

public class PromoCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int DiscountPercentage { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? MaxUses { get; set; }
    public int CurrentUses { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    
    public Guid Version { get; set; } = Guid.NewGuid();
}
