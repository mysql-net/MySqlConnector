# AI Instructions

## Project Context

MySqlConnector is a high-performance ADO.NET Data Provider for MySQL.
It implements the MySQL protocol and handles authentication, connection pooling, and data access.

## Key Conventions

### Code Style
- follow Microsoft's C# coding conventions with the exception that field names are prefixed with `m_` and are specified at the end of the class definition
- use C# nullable reference types and implicit usings
- XML documentation required for public APIs
- use tabs for indentation in *.cs files; use two spaces for indentation in XML and JSON files, including *.csproj files
- follow the rules from `.editorconfig`
- implement async methods using the IOBehavior pattern: sync and async methods delegate to a common implementation that takes an IOBehavior parameter and handles both I/O types

### Performance
- minimize allocations in hot paths
- use Span-based APIs wherever possible

### Testing
- Test files should be located in corresponding test directories. Place unit tests in tests/MySqlConnector.Tests. Place integration tests in tests/IntegrationTests.
- Test names should be descriptive and follow the pattern `CamelCaseShortDescription`.
- Use xUnit for tests.
