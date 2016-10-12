# Tests

## Side-by-side Tests

The `SideBySide.Baseline` and `SideBySide.New` projects are intended to verify that the new
MySql.Data implementation is equivalent to the official ADO.NET connector in places where that
is deemed important.

The source code is all in the `SideBySide.New` folder, and added (as link) to the `SideBySide.Baseline`
project.

The tests require a MySQL server.  Copy the file `SideBySide.New/config.json.example` to `SideBySide.New/config.json`.
Then edit the `config.json` file in order to connect to your server:

    Data.ConnectionString: The full MySql Connection String to your server.  You should specify a database name.
        If the database does not exist, the test will attempt to create it.

    Data.PasswordlessUser: Leave blank to disable passwordless user tests.  Otherwise, this should be a user
        on your database with no Password and no Roles.

    Data.SupportsJson: True if your MySql server supports JSON (5.7 and up), false otherwise.

There are two ways to run the tests: command line and Visual Studio.

### Visual Studio 2015

After building the solution, you should see a list of tests in the Test Explorer.  Click "Run All" to run them.

### Command Line

To run the Baseline tests:

```
packages\xunit.runner.console.2.1.0\tools\xunit.console.exe tests\SideBySide.Baseline\bin\Debug\SideBySide.Baseline.dll
```

To run the New tests:

```
dotnet test tests\SideBySide.New
```
