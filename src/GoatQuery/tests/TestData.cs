public static class TestData
{
    private static readonly User Manager01 = new User
    {
        Id = Guid.Parse("671e6bac-b6de-4cc7-b3e9-1a6ac4546b43"),
        Age = 16,
        Firstname = "Manager 01",
        DateOfBirth = DateTime.Parse("2000-01-01 00:00:00").ToUniversalTime(),
        BalanceDecimal = 2.00m,
        IsEmailVerified = false,
    };

    public static readonly Dictionary<string, User> Users = new Dictionary<string, User>
    {
        ["John"] = new User
        {
            Age = 2,
            Firstname = "John",
            DateOfBirth = DateTime.Parse("2004-01-31 23:59:59").ToUniversalTime(),
            BalanceDecimal = 1.50m,
            IsEmailVerified = true,
            Addresses = new[]
            {
                new Address
                {
                    AddressLine1 = "123 Main St",
                    City = new City { Name = "New York", Country = "USA" }
                },
                new Address
                {
                    AddressLine1 = "456 Oak Ave",
                    City = new City { Name = "Boston", Country = "USA" }
                }
            },
            Manager = Manager01
        },
        ["Jane"] = new User
        {
            Id = Guid.Parse("01998fda-e310-793c-bd8d-f6a92f87b31b"),
            Age = 9,
            Firstname = "Jane",
            DateOfBirth = DateTime.Parse("2020-05-09 15:30:00").ToUniversalTime(),
            BalanceDecimal = 0,
IsEmailVerified = false,
            Addresses = new[]
            {
                new Address
                {
                    AddressLine1 = "789 Pine Rd",
                    City = new City { Name = "Seattle", Country = "USA" }
                }
            },
            Company = new Company
            {
                Name = "Acme Corp",
                Department = "Sales"
            }
        },
        ["Apple"] = new User
        {
            Age = 1,
            Firstname = "Apple",
            DateOfBirth = DateTime.Parse("1980-12-31 00:00:01").ToUniversalTime(),
            BalanceFloat = 1204050.98f,
IsEmailVerified = true,
            Addresses = new[]
            {
                new Address
                {
                    AddressLine1 = "321 Elm St",
                    City = new City { Name = "Chicago", Country = "USA" }
                },
                new Address
                {
                    AddressLine1 = "654 Maple Dr",
                    City = new City { Name = "New York", Country = "USA" }
                }
            },
            Manager = Manager01,
            Tags = ["vip", "premium"]
        },
        ["Harry"] = new User
        {
            Id = Guid.Parse("e4c7772b-8947-4e46-98ed-644b417d2a08"),
            Age = 1,
            Firstname = "Harry",
            DateOfBirth = DateTime.Parse("2002-08-01").ToUniversalTime(),
            BalanceDecimal = 0.5372958205929493m,
IsEmailVerified = false,
            Addresses = Array.Empty<Address>()
        },
        ["Doe"] = new User
        {
            Age = 1,
            Firstname = "Doe",
            DateOfBirth = DateTime.Parse("2023-07-26 12:00:30").ToUniversalTime(),
            BalanceDecimal = null,
IsEmailVerified = true,
            Addresses = new[]
            {
                new Address
                {
                    AddressLine1 = "999 Broadway",
                    City = new City { Name = "Los Angeles", Country = "USA" }
                }
            }
        },
        ["Egg"] = new User
        {
            Age = 33,
            Firstname = "Egg",
            DateOfBirth = DateTime.Parse("2000-01-01 00:00:00").ToUniversalTime(),
            BalanceDouble = 1334534453453433.33435443343231235652d,
IsEmailVerified = false,
            Addresses = new[]
            {
                new Address
                {
                    AddressLine1 = "777 First Ave",
                    City = new City { Name = "Miami", Country = "USA" }
                },
                new Address
                {
                    AddressLine1 = "888 Second St",
                    City = new City { Name = "Orlando", Country = "USA" }
                }
            },
            Manager = new User
            {
                Age = 18,
                Firstname = "Manager 02",
                DateOfBirth = DateTime.Parse("1999-04-21 00:00:00").ToUniversalTime(),
                BalanceDecimal = 19.00m,
IsEmailVerified = true,
                Manager = new User
                {
                    Age = 30,
                    Firstname = "Manager 03",
                    DateOfBirth = DateTime.Parse("1993-04-21 00:00:00").ToUniversalTime(),
                    BalanceDecimal = 29.00m,
                    IsEmailVerified = true,
                    Manager = new User
                    {
                        Age = 40,
                        Firstname = "Manager 04",
                        DateOfBirth = DateTime.Parse("1983-04-21 00:00:00").ToUniversalTime(),
                        BalanceDecimal = 39.00m,
IsEmailVerified = true,
                    },
                    Company = new Company
                    {
                        Name = "My Test Company",
                        Department = "Development"
                    }
                }
            },
            Tags = ["premium"]
        },
        ["NullUser"] = new User
        {
            Age = 4,
            Firstname = "NullUser",
            DateOfBirth = null,
            BalanceDecimal = null,
            BalanceDouble = null,
            BalanceFloat = null,
IsEmailVerified = true,
            Addresses = Array.Empty<Address>()
        },
    };
}
