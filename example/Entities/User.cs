using System.ComponentModel.DataAnnotations.Schema;

public record User
{
    public Guid Id { get; set; }
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsEmailVerified { get; set; }
    public double Test { get; set; }
    public int? NullableInt { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime DateOfBirthUtc { get; set; }

    [Column(TypeName = "timestamp without time zone")]
    public DateTime DateOfBirthTz { get; set; }
    public User? Manager { get; set; }
    public IEnumerable<Address> Addresses { get; set; } = Array.Empty<Address>();
    public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();
    public Company? Company { get; set; }
}

public record Address
{
    public Guid Id { get; set; }
    public City City { get; set; } = new City();
    public string AddressLine1 { get; set; } = string.Empty;
}

public record City
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

public record Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}