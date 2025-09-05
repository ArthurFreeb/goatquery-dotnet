using System.Text.Json.Serialization;

public record User
{
    public int Age { get; set; }
    public Guid UserId { get; set; }
    public string Firstname { get; set; } = string.Empty;
    public decimal? BalanceDecimal { get; set; }
    public double? BalanceDouble { get; set; }
    public float? BalanceFloat { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool IsEmailVerified { get; set; }
    public User? Manager { get; set; }
    public IEnumerable<Address> Addresses { get; set; } = Array.Empty<Address>();
}

public sealed record CustomJsonPropertyUser : User
{
    [JsonPropertyName("last_name")]
    public string Lastname { get; set; } = string.Empty;
}

public record Address
{
    public City City { get; set; } = new City();
    public string AddressLine1 { get; set; } = string.Empty;
}

public record City
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}