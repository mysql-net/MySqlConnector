Performance Tests
=================

The `MySqlConnector.Performance` project runs a .NET Core MVC API application that is intended to be used to load test asynchronous and synchronous MySqlConnector methods.

You first must configure your MySql Database.  Open the `config.json.example` file, configure the connection string, and save it as `config.json`.  Now you can run the application with `dotnet run`.

The application runs on http://localhost:5000 by default.  It drops and creates a table called `BlogPosts` in the test database when it is started.

`GET  /api/async` and `GET /api/sync` return the most recent 10 posts.

`POST /api/async` and `POST /api/sync` create a new post.  The request body should be `Content-Type: application/json` in the form:

    {
        "Title": "Post Title",
        "Content": "Post Content"
    }

`GET  /api/async/bulkinsert/<num>` and `GET /api/sync/bulkinsert/<num>` Insert <num> blog posts serially in a transaction

`GET  /api/async/bulkselect/<num>` and `GET /api/sync/bulkselect/<num>` Selects <num> blog posts and exhausts the datareader (make sure you have inserted <num> posts first with the bulkinsert endpoint)

The `scripts` directory contains load testing scripts.  These scripts require that the  [Vegeta](https://github.com/tsenart/vegeta/releases) binary is installed and accessible in your PATH.  Here are examples of how to call the load testing scripts:

    # by default, runs 50 async queries per second for 5 seconds
    ./stress.sh     # bash for linux
    ./stress.ps1    # powershell for windows

    # runs 100 async queries per second for 10 seconds on linux
    ./stress.sh 100 10s async

    # run 50 sync queries per second for 1 minute on windows
    ./stress.ps1 50 1m sync
