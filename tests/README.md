# Tests

## Side-by-side Tests

The `SideBySide.Baseline` and `SideBySide.New` projects are intended to verify that the new
MySql.Data implementation is equivalent to the official ADO.NET connector in places where that
is deemed important.

The source code is all in the `SideBySide.New` folder, and added (as link) to the `SideBySide.Baseline`
project.

The tests require a local MySQL server (although this can be changed in `Constants.cs` to use
a remote server). Create a user with the Login Name `mysqltest`, Password `test;key="val` and
Role "DBManager".

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
cd tests\SideBySide.New
dnx test
```
