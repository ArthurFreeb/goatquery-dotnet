public static class TestData
{
    public static readonly Dictionary<string, User> Users = new Dictionary<string, User>
    {
        ["John"] = new User
        {
            Age = 2,
            Firstname = "John",
            UserId = Guid.Parse("58cdeca3-645b-457c-87aa-7d5f87734255"),
            DateOfBirth = DateTime.Parse("2004-01-31 23:59:59"),
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
            Manager = new User
            {
                Age = 16,
                Firstname = "Manager 01",
                UserId = Guid.Parse("671e6bac-b6de-4cc7-b3e9-1a6ac4546b43"),
                DateOfBirth = DateTime.Parse("2000-01-01 00:00:00"),
                BalanceDecimal = 2.00m,
                IsEmailVerified = false
            }
        },
        ["Jane"] = new User
        {
            Age = 9,
            Firstname = "Jane",
            UserId = Guid.Parse("58cdeca3-645b-457c-87aa-7d5f87734255"),
            DateOfBirth = DateTime.Parse("2020-05-09 15:30:00"),
            BalanceDecimal = 0,
            IsEmailVerified = false,
            Addresses = new[]
            {
                new Address
                {
                    AddressLine1 = "789 Pine Rd",
                    City = new City { Name = "Seattle", Country = "USA" }
                }
            }
        },
        ["Apple"] = new User
        {
            Age = 1,
            Firstname = "Apple",
            UserId = Guid.Parse("58cdeca3-645b-457c-87aa-7d5f87734255"),
            DateOfBirth = DateTime.Parse("1980-12-31 00:00:01"),
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
            Manager = new User
            {
                Age = 16,
                Firstname = "Manager 01",
                UserId = Guid.Parse("671e6bac-b6de-4cc7-b3e9-1a6ac4546b43"),
                DateOfBirth = DateTime.Parse("2000-01-01 00:00:00"),
                BalanceDecimal = 2.00m,
                IsEmailVerified = true
            }
        },
        ["Harry"] = new User
        {
            Age = 1,
            Firstname = "Harry",
            UserId = Guid.Parse("e4c7772b-8947-4e46-98ed-644b417d2a08"),
            DateOfBirth = DateTime.Parse("2002-08-01"),
            BalanceDecimal = 0.5372958205929493m,
            IsEmailVerified = false,
            Addresses = Array.Empty<Address>()
        },
        ["Doe"] = new User
        {
            Age = 1,
            Firstname = "Doe",
            UserId = Guid.Parse("58cdeca3-645b-457c-87aa-7d5f87734255"),
            DateOfBirth = DateTime.Parse("2023-07-26 12:00:30"),
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
            UserId = Guid.Parse("58cdeca3-645b-457c-87aa-7d5f87734255"),
            DateOfBirth = DateTime.Parse("2000-01-01 00:00:00"),
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
                UserId = Guid.Parse("2bde56ac-4829-41fb-abbc-2b8454962e2a"),
                DateOfBirth = DateTime.Parse("1999-04-21 00:00:00"),
                BalanceDecimal = 19.00m,
                IsEmailVerified = true,
                Manager = new User
                {
                    Age = 30,
                    Firstname = "Manager 03",
                    UserId = Guid.Parse("8ef23728-c429-42f9-98ee-425419092664"),
                    DateOfBirth = DateTime.Parse("1993-04-21 00:00:00"),
                    BalanceDecimal = 29.00m,
                    IsEmailVerified = true,
                    Manager = new User
                    {
                        Age = 40,
                        Firstname = "Manager 04",
                        UserId = Guid.Parse("4cde56ac-4829-41fb-abbc-2b8454962e2a"),
                        DateOfBirth = DateTime.Parse("1983-04-21 00:00:00"),
                        BalanceDecimal = 39.00m,
                        IsEmailVerified = true
                    }
                }
            }
        },
        ["NullUser"] = new User
        {
            Age = 4,
            Firstname = "NullUser",
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DateOfBirth = null,
            BalanceDecimal = null,
            BalanceDouble = null,
            BalanceFloat = null,
            IsEmailVerified = true,
            Addresses = Array.Empty<Address>()
        },
    };
}