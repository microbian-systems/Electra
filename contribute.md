Contributing to this project

Thank you for your interest in contributing. This document explains how to report issues, propose changes, and submit code so your contribution can be reviewed and merged smoothly.

Reporting bugs and feature requests

- Use GitHub Issues to report bugs or request features. Provide a clear title and reproducible steps, include expected and actual behavior, environment (OS, .NET version), and minimal repro code or a small project if possible.
- Attach logs and stack traces when relevant and indicate which version of the project you used.

Asking questions and getting help

- For short questions or discussion, join the project's Discord server (look for an invite link in the README or project pages) or open an issue tagged as "discussion" if appropriate.
- For more in-depth support, use GitHub Issues and provide the same diagnostic details requested for bug reports.

Working on changes (fork & pull request workflow)

1. Fork the repository and clone your fork locally.
2. Create a topic branch with a short, descriptive name (e.g., fix/nameofissue or feature/nameoffeature).
3. Commit logical, focused changes with clear messages describing the intent and reasoning.
4. Rebase or merge the latest changes from the upstream default branch into your branch before opening a pull request.
5. Push your branch to your fork and open a pull request against the upstream default branch; include a description of the change, rationale, and any compatibility considerations.

Coding standards and tests

- Follow the existing coding style used in the repository. Match naming, formatting, and design patterns already present.
- Add or update unit tests that cover the behavior your changes introduce or modify.
- Ensure all tests pass locally before submitting a pull request.

Building and testing locally

- Use the standard build tools for the project (for .NET projects, use `dotnet build` and `dotnet test`).
- Run the test suite and any linters or formatters configured in the repo.

Review process and feedback

- Maintainers will review pull requests and may request changes. Address review comments by updating your branch and pushing additional commits.
- Keep changes focused and resolve conversations when their issues have been addressed.

Licensing and contribution expectations

- By contributing code, you agree your contributions will be licensed under this project's license (see the LICENSE file).
- Keep contributions respectful and professional; follow the project's code of conduct if one exists.

Tips for a smooth contribution

- Open an issue to discuss large or architectural changes before implementing them.
- Keep PRs small and focused to make review easier.
- Provide tests and documentation updates alongside code changes.

Thank you for helping improve the project. We appreciate your time and effort!
