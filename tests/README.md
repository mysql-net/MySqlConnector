# Tests

## Side-by-side Tests

The `SideBySide` project is intended to verify that the new MySql.Data implementation
is equivalent to the official ADO.NET connector in places where that is deemed important.

The tests require a MySQL server.  Copy the file `SideBySide/config.json.example` to `SideBySide/config.json`.
Then edit the `config.json` file in order to connect to your server:

    Data.ConnectionString: The full MySql Connection String to your server.  You should specify a database name.
        If the database does not exist, the test will attempt to create it.

    Data.PasswordlessUser: Leave blank to disable passwordless user tests.  Otherwise, this should be a user
        on your database with no Password and no Roles.

    Data.SupportsJson: True if your MySql server supports JSON (5.7 and up), false otherwise.

There are two ways to run the tests: command line and Visual Studio.

### Visual Studio 2017

After building the solution, you should see a list of tests in the Test Explorer.  Click "Run All" to run them.

### Command Line

To run the New tests against MySqlConnector:

```
dotnet restore; dotnet test
```

To run the Baseline tests against MySql.Data:

```
dotnet restore /p:Configuration=Baseline; dotnet test -c Baseline
```
