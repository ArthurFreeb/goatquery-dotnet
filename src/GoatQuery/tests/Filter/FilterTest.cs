using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class FilterTest : IClassFixture<DatabaseTestFixture>
{
    private readonly DatabaseTestFixture _fixture;

    public FilterTest(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
    }

    public static IEnumerable<object[]> Parameters()
    {
        yield return new object[] {
            "firstname eq 'John'",
            new[] { TestData.Users["John"] }
        };

        yield return new object[] {
            "firstname eq 'Random'",
            Array.Empty<User>()
        };

        yield return new object[] {
            "Age eq 1",
            new[] { TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Doe"] }
        };

        yield return new object[] {
            "Age eq 0",
            Array.Empty<User>()
        };

        yield return new object[] {
            "firstname eq 'John' and Age eq 2",
            new[] { TestData.Users["John"] }
        };

        yield return new object[] {
            "firstname eq 'John' or Age eq 33",
            new[] { TestData.Users["John"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "Age eq 1 and firstName eq 'Harry' or Age eq 2",
            new[] { TestData.Users["John"], TestData.Users["Harry"] }
        };

        yield return new object[] {
            "Age eq 1 or Age eq 2 or firstName eq 'Egg'",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "Age ne 33",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Doe"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "firstName contains 'a'",
            new[] { TestData.Users["Jane"], TestData.Users["Apple"], TestData.Users["Harry"] }
        };

        yield return new object[] {
            "Age ne 1 and firstName contains 'a'",
            new[] { TestData.Users["Jane"] }
        };

        yield return new object[] {
            "Age ne 1 and firstName contains 'a' or firstName eq 'Apple'",
            new[] { TestData.Users["Jane"], TestData.Users["Apple"] }
        };

        yield return new object[] {
            "Firstname eq 'John' and Age eq 2 or Age eq 33",
            new[] { TestData.Users["John"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "(Firstname eq 'John' and Age eq 2) or Age eq 33",
            new[] { TestData.Users["John"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "Firstname eq 'John' and (Age eq 2 or Age eq 33)",
            new[] { TestData.Users["John"] }
        };

        yield return new object[] {
            "(Firstname eq 'John' and Age eq 2 or Age eq 33)",
            new[] { TestData.Users["John"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "(Firstname eq 'John') or (Age eq 33 and Firstname eq 'Egg') or Age eq 1 and (Age eq 2)",
            new[] { TestData.Users["John"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "UserId eq e4c7772b-8947-4e46-98ed-644b417d2a08",
            new[] { TestData.Users["Harry"] }
        };

        yield return new object[] {
            "age lt 3",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Doe"] }
        };

        yield return new object[] {
            "age lt 1",
            Array.Empty<User>()
        };

        yield return new object[] {
            "age lte 2",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Doe"] }
        };

        yield return new object[] {
            "age gt 1",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Egg"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "age gte 3",
            new[] { TestData.Users["Jane"], TestData.Users["Egg"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "age lt 3 and age gt 1",
            new[] { TestData.Users["John"] }
        };

        yield return new object[] {
            "balanceDecimal eq 1.50m",
            new[] { TestData.Users["John"] }
        };

        yield return new object[] {
            "balanceDecimal gt 1m",
            new[] { TestData.Users["John"] }
        };

        yield return new object[] {
            "balanceDecimal gt 0.50m",
            new[] { TestData.Users["John"], TestData.Users["Harry"] }
        };

        yield return new object[] {
            "balanceDecimal eq 0.5372958205929493m",
            new[] { TestData.Users["Harry"] }
        };

        yield return new object[] {
            "balanceDouble eq 1334534453453433.33435443343231235652d",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "balanceFloat eq 1204050.98f",
            new[] { TestData.Users["Apple"] }
        };

        yield return new object[] {
            "balanceFloat gt 2204050f",
            Array.Empty<User>()
        };

        yield return new object[] {
            "dateOfBirth eq 2000-01-01",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "dateOfBirth eq 2020-05-09",
            new[] { TestData.Users["Jane"] }
        };

        yield return new object[] {
            "dateOfBirth lt 2010-01-01",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "dateOfBirth lte 2002-08-01",
            new[] { TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "dateOfBirth gt 2000-08-01 and dateOfBirth lt 2023-01-01",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Harry"] }
        };

        yield return new object[] {
            "dateOfBirth eq 2023-07-26T12:00:30Z",
            new[] { TestData.Users["Doe"] }
        };

        yield return new object[] {
            "dateOfBirth gte 2000-01-01",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Harry"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "dateOfBirth gte 2000-01-01 and dateOfBirth lte 2020-05-09T15:29:59",
            new[] { TestData.Users["John"], TestData.Users["Harry"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "balanceDecimal eq null",
            new[] { TestData.Users["Apple"], TestData.Users["Doe"], TestData.Users["Egg"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "balanceDecimal ne null",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Harry"] }
        };

        yield return new object[] {
            "balanceDouble eq null",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Doe"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "balanceDouble ne null",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "balanceFloat eq null",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Harry"], TestData.Users["Doe"], TestData.Users["Egg"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "balanceFloat ne null",
            new[] { TestData.Users["Apple"] }
        };

        yield return new object[] {
            "dateOfBirth eq null",
            new[] { TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "dateOfBirth ne null",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "balanceDecimal eq null and age gt 3",
            new[] { TestData.Users["Egg"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "balanceDecimal ne null or age eq 4",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Harry"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "firstname eq 'Doe' and balanceDecimal eq null",
            new[] { TestData.Users["Doe"] }
        };

        yield return new object[] {
            "isEmailVerified eq true",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Doe"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "isEmailVerified eq false",
            new[] { TestData.Users["Jane"], TestData.Users["Harry"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "isEmailVerified ne true",
            new[] { TestData.Users["Jane"], TestData.Users["Harry"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "isEmailVerified ne false",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Doe"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "age gt 2 and isEmailVerified eq true",
            new[] { TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "isEmailVerified eq false or age eq 2",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Harry"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager/firstName eq 'Manager 01'",
            new[] { TestData.Users["John"], TestData.Users["Apple"] }
        };

        yield return new object[] {
            "manager/firstName ne 'Manager 01'",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager/firstName contains 'Manager'",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager/firstName eq 'Manager 02'",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager/isEmailVerified eq true",
            new[] { TestData.Users["Apple"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager/age gt 16",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager/manager/firstName eq 'Manager 03'",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager/manager/firstName ne 'Manager 03'",
            new List<User>()
        };

        yield return new object[] {
            "manager eq null",
            new[] { TestData.Users["Jane"], TestData.Users["Harry"], TestData.Users["Doe"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "manager/firstName eq 'Manager 01' and manager/age eq 16",
            new[] { TestData.Users["John"], TestData.Users["Apple"] }
        };

        yield return new object[] {
            "manager/dateOfBirth lt 2000-01-01",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager/balanceDecimal gte 2.00m and manager/balanceDecimal lt 20m",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager/userId eq 671e6bac-b6de-4cc7-b3e9-1a6ac4546b43",
            new[] { TestData.Users["John"], TestData.Users["Apple"] }
        };

        yield return new object[] {
            "(age eq 2 and manager/isEmailVerified eq true) or (age eq 33 and manager/manager ne null)",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager ne null and manager/manager eq null",
            new[] { TestData.Users["John"], TestData.Users["Apple"] }
        };

        yield return new object[] {
            "company ne null ",
            new[] { TestData.Users["Jane"] }
        };

        yield return new object[] {
            "company/name eq 'Acme Corp'",
            new[] { TestData.Users["Jane"] }
        };

        yield return new object[] {
            "manager/manager/company/name eq 'My Test Company'",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager/balanceDecimal gt 100m",
            Array.Empty<User>()
        };

        yield return new object[] {
            "manager/manager/manager/firstName eq 'Manager 04'",
            new[] { TestData.Users["Egg"] }
        };

        yield return new object[] {
            "addresses/any(addr: addr/city/name eq 'New York')",
            new[] { TestData.Users["John"], TestData.Users["Apple"] }
        };

        yield return new object[] {
            "addresses/any(address: address/city/name eq 'Chicago')",
            new[] { TestData.Users["Apple"] }
        };

        yield return new object[] {
            "addresses/any(a: a/city/name eq 'Seattle')",
            new[] { TestData.Users["Jane"] }
        };

        yield return new object[] {
            "addresses/any(addr: addr/city/name eq 'NonExistentCity')",
            Array.Empty<User>()
        };

        // Lambda expression tests with addresses/all
        yield return new object[] {
            "addresses/all(addr: addr/city/country eq 'USA')",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Apple"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "addresses/all(a: a/city/name ne 'Chicago')",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        // Lambda expression tests with addressLine1
        yield return new object[] {
            "addresses/any(addr: addr/addressLine1 contains 'Main')",
            new[] { TestData.Users["John"] }
        };

        yield return new object[] {
            "addresses/any(a: a/addressLine1 contains 'St')",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Egg"] }
        };

        // Lambda expressions combined with regular filters
        yield return new object[] {
            "firstname eq 'John' and addresses/any(addr: addr/city/name eq 'New York')",
            new[] { TestData.Users["John"] }
        };

        yield return new object[] {
            "age eq 1 or addresses/any(addr: addr/city/name eq 'Miami')",
            new[] { TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "addresses/any(addr: addr/city/name eq 'Seattle') and isEmailVerified eq false",
            new[] { TestData.Users["Jane"] }
        };

        // Empty addresses should work with any() returning false and all() returning true
        yield return new object[] {
            "addresses/any(addr: addr/city/name eq 'AnyCity')",
            Array.Empty<User>()
        };

        yield return new object[] {
            "addresses/all(addr: addr/city/name ne 'SomeCity')",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Apple"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        // Complex lambda expressions with logical operators
        yield return new object[] {
            "addresses/any(addr: addr/city/name eq 'New York' or addr/city/name eq 'Chicago')",
            new[] { TestData.Users["John"], TestData.Users["Apple"] }
        };

        yield return new object[] {
            "addresses/any(addr: addr/city/name eq 'Miami' and addr/city/country eq 'USA')",
            new[] { TestData.Users["Egg"] }
        };

        // Testing with users that have no addresses (empty collections)
        yield return new object[] {
            "firstname eq 'Harry' and addresses/any(addr: addr/city/name eq 'NonExistent')",
            Array.Empty<User>()
        };

        yield return new object[] {
            "firstname eq 'NullUser' and addresses/all(addr: addr/city/country eq 'USA')",
            Array.Empty<User>()
        };

        yield return new object[] {
            "tags/any(x: x eq 'vip')",
            new[] { TestData.Users["Apple"] }
        };

        yield return new object[] {
            "tags/any(x: x eq 'premium')",
            new[] { TestData.Users["Apple"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "tags/all(x: x eq 'premium')",
            new[] { TestData.Users["Egg"] }
        };

        // Status enum tests
        yield return new object[] {
            "status eq 'Active'",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "status eq 'Inactive'",
            new[] { TestData.Users["Jane"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "status eq 'Suspended'",
            new[] { TestData.Users["Harry"] }
        };

        yield return new object[] {
            "status ne 'Active'",
            new[] { TestData.Users["Jane"], TestData.Users["Harry"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "status ne 'Inactive'",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "status ne 'Suspended'",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Apple"], TestData.Users["Doe"], TestData.Users["Egg"], TestData.Users["NullUser"] }
        };

        // Status combined with other properties
        yield return new object[] {
            "status eq 'Active' and age eq 1",
            new[] { TestData.Users["Apple"], TestData.Users["Doe"] }
        };

        yield return new object[] {
            "status eq 'Inactive' or age eq 33",
            new[] { TestData.Users["Jane"], TestData.Users["Egg"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "status eq 'Active' and isEmailVerified eq true",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Doe"] }
        };

        yield return new object[] {
            "status ne 'Active' and age lt 10",
            new[] { TestData.Users["Jane"], TestData.Users["Harry"], TestData.Users["NullUser"] }
        };

        // Manager status tests
        yield return new object[] {
            "manager/status eq 'Active'",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager ne null and manager/status eq 'Active'",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "status eq Active",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "status eq Inactive",
            new[] { TestData.Users["Jane"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "status eq Suspended",
            new[] { TestData.Users["Harry"] }
        };

        yield return new object[] {
            "status ne Active",
            new[] { TestData.Users["Jane"], TestData.Users["Harry"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "status ne Inactive",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Harry"], TestData.Users["Doe"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "status ne Suspended",
            new[] { TestData.Users["John"], TestData.Users["Jane"], TestData.Users["Apple"], TestData.Users["Doe"], TestData.Users["Egg"], TestData.Users["NullUser"] }
        };

        yield return new object[] {
            "status eq Active and age eq 1",
            new[] { TestData.Users["Apple"], TestData.Users["Doe"] }
        };

        yield return new object[] {
            "status eq Active and isEmailVerified eq true",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Doe"] }
        };

        yield return new object[] {
            "manager/status eq Active",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Egg"] }
        };

        yield return new object[] {
            "manager ne null and manager/status eq Active",
            new[] { TestData.Users["John"], TestData.Users["Apple"], TestData.Users["Egg"] }
        };
    }

    [Theory]
    [MemberData(nameof(Parameters))]
    public void Test_Filter(string filter, IEnumerable<User> expected)
    {
        var query = new Query
        {
            Filter = filter
        };

        var result = _fixture.DbContext.Users.Apply(query);

        Console.WriteLine("------------------------------------------ QUERY ------------------------------------------");
        Console.WriteLine(result.Value.Query.ToQueryString());

        Assert.Equal(expected, result.Value.Query);
    }

    [Theory]
    [InlineData("NonExistentProperty eq 'John'")]
    [InlineData("manager//firstName eq 'John'")]
    [InlineData("manager/ eq 'John'")]
    [InlineData("/manager eq 'John'")]
    [InlineData("addresses/any(addr: addr/nonExistentProperty eq 'test')")]
    [InlineData("addresses/invalid(addr: addr/city/name eq 'test')")]
    [InlineData("nonExistentCollection/any(item: item eq 'test')")]
    [InlineData("firstname eq John")] // Unquoted RHS on non-enum should error
    public void Test_InvalidFilterReturnsError(string filter)
    {
        var query = new Query
        {
            Filter = filter
        };

        var result = _fixture.DbContext.Users.Apply(query);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void Test_Filter_WithCustomJsonPropertyName()
    {
        var users = new List<CustomJsonPropertyUser>{
            new CustomJsonPropertyUser { Lastname = "John" },
            new CustomJsonPropertyUser { Lastname = "Jane" },
            new CustomJsonPropertyUser { Lastname = "Apple" },
            new CustomJsonPropertyUser { Lastname = "Harry" },
            new CustomJsonPropertyUser { Lastname = "Doe" },
            new CustomJsonPropertyUser { Lastname = "Egg" }
        }.AsQueryable();

        var query = new Query
        {
            Filter = "last_name eq 'John'"
        };

        var result = users.Apply(query);

        Assert.Equal(new List<CustomJsonPropertyUser>{
            new CustomJsonPropertyUser { Lastname = "John" },
        }, result.Value.Query);
    }

    public record IntegerConverts
    {
        public long Long { get; set; }
        public short Short { get; set; }
        public byte Byte { get; set; }
        public uint Uint { get; set; }
        public ulong ULong { get; set; }
        public ushort UShort { get; set; }
        public sbyte SByte { get; set; }
    }

    [Theory]
    [InlineData("long eq 10")]
    [InlineData("short eq 20")]
    [InlineData("byte eq 30")]
    [InlineData("uint eq 40")]
    [InlineData("ulong eq 50")]
    [InlineData("ushort eq 60")]
    [InlineData("sbyte eq 70")]
    public void Test_Filter_CanConvertIntToOtherNumericTypes(string filter)
    {
        var users = new List<IntegerConverts>{
            new IntegerConverts() { Long = 0, Short = 0, Byte = 0, Uint = 0, ULong = 0, UShort = 0, SByte = 0},
            new IntegerConverts() { Long = 10, Short = 20, Byte = 30, Uint = 40, ULong = 50, UShort = 60, SByte = 70},
            new IntegerConverts() { Long = 1, Short = 2, Byte = 3, Uint = 4, ULong = 5, UShort = 6, SByte = 7},
        }.AsQueryable();

        var query = new Query
        {
            Filter = filter
        };

        var result = users.Apply(query);

        Assert.Equal(new List<IntegerConverts>{
            new IntegerConverts() { Long = 10, Short = 20, Byte = 30, Uint = 40, ULong = 50, UShort = 60, SByte = 70},
        }, result.Value.Query);
    }
}