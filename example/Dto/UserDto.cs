using System.Text.Json.Serialization;

public record UserDto
{
    public Guid Id { get; set; }

    [JsonPropertyName("first_name")]
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsEmailVerified { get; set; }
    public double Test { get; set; }
    public int? NullableInt { get; set; }
    public DateTime DateOfBirthUtc { get; set; }
    public DateTime DateOfBirthTz { get; set; }
    public User? Manager { get; set; }
    public IEnumerable<AddressDto> Addresses { get; set; } = Array.Empty<AddressDto>();
}

public record AddressDto
{
    public string AddressLine1 { get; set; } = string.Empty;
    public CityDto City { get; set; } = new CityDto();
}

public record CityDto
{
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}