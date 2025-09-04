# GoatQuery

A .NET library for parsing query parameters into LINQ expressions. Enables database-level filtering, sorting, and pagination from HTTP query strings.

## Installation

```bash
dotnet add package GoatQuery
dotnet add package GoatQuery.AspNetCore  # For ASP.NET Core integration
```

## Quick Start

```csharp
// Basic usage
var users = dbContext.Users
    .Apply(new Query { Filter = "age gt 18 and isActive eq true" })
    .Value.Results;

// ASP.NET Core
[HttpGet]
[EnableQuery<UserDto>(maxTop: 100)]
public IActionResult GetUsers() => Ok(dbContext.Users);
```

## Supported Syntax

```
GET /api/users?$filter=age gt 18 and isActive eq true
GET /api/users?$orderby=lastName asc, firstName desc
GET /api/users?$top=10&$skip=20&$count=true
GET /api/users?$search=john
```

## Filtering

### Operators

- **Comparison**: `eq`, `ne`, `gt`, `ge`, `lt`, `le`
- **Logical**: `and`, `or`
- **String**: `contains`

### Data Types

- String: `'value'`
- Numbers: `42`, `3.14f`, `2.5m`, `1.0d`
- Boolean: `true`, `false`
- DateTime: `2023-12-25T10:30:00Z`
- GUID: `123e4567-e89b-12d3-a456-426614174000`
- Null: `null`

### Examples

```csharp
"age gt 18"
"firstName eq 'John' and isActive ne false"
"salary ge 50000 or department eq 'Engineering'"
"name contains 'smith'"
```

## Property Mapping

Supports `JsonPropertyName` attributes:

```csharp
public class UserDto
{
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; }

    public int Age { get; set; }  // Maps to "age"
}
```

Query: `$filter=first_name eq 'John' and age gt 18`

## Search

Implement custom search logic:

```csharp
public class UserSearchBinder : ISearchBinder<User>
{
    public Expression<Func<User, bool>> Bind(string searchTerm) =>
        user => user.FirstName.Contains(searchTerm) ||
                user.LastName.Contains(searchTerm);
}

var result = users.Apply(query, new UserSearchBinder());
```

## ASP.NET Core Integration

### Action Filter

```csharp
[HttpGet]
[EnableQuery<UserDto>(maxTop: 100)]
public IActionResult GetUsers() => Ok(dbContext.Users);
```

### Manual Processing

```csharp
[HttpGet]
public IActionResult GetUsers([FromQuery] Query query)
{
    var result = dbContext.Users.Apply(query);
    return result.IsFailed ? BadRequest(result.Errors) : Ok(result.Value);
}
```

## Error Handling

Uses FluentResults pattern:

```csharp
var result = users.Apply(query);
if (result.IsFailed)
    return BadRequest(result.Errors.Select(e => e.Message));

var data = result.Value.Results;
var count = result.Value.Count;  // If Count = true
```

## Development

```bash
dotnet test ./src/GoatQuery/tests
dotnet build --configuration Release
cd example && dotnet run
```

**Targets**: .NET Standard 2.0/2.1, .NET 6.0+
