# Contributing to MySqlConnector

Firstly, thank you for wanting to contribute to the project!

### General Guidelines

If there's not already an issue describing the problem you want to fix, please create one. Otherwise,
add a comment to the existing issue, indicating that you want to work on it.

Please read [how to run the tests](../tests/README.md) and run them locally before opening your pull request.
All pull requests will be tested automatically; PRs with failing tests will not be accepted.

All PRs that add new functionality must also include tests that verify the new functionality. This is important
to ensure that compatibility with `MySql.Data` is maintained.

This project uses the [Developer Certificate of Origin](https://developercertificate.org/). In short, you
must add a `Signed-off-by: Your Name <email@example.com>` line to the bottom of your commit
message to indicate that you accept the DCO and have the right to submit your contributions. You
can use `git commit -s` to automatically add this line to your commit message. [Learn more](https://probot.github.io/apps/dco/)

### Code Guidelines

Please install an [Editorconfig plugin](http://editorconfig.org/#download) in your favourite editor so that your
source code follows the repo style.

Commit messages should generally follow [these guidelines](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html).

Each commit should contain one logical change. If some refactoring is necessary before your
patch can be written, commit that as a separate change first. This allows the independent parts
of the PR to be reviewed separately. This [post on commit messages](http://who-t.blogspot.com/2009/12/on-commit-messages.html)
has helpful recommendations.
