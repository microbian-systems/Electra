# Electra .NET Utilities Framework

## Project Overview

Electra is a .NET utilities framework designed to be flexible and powerful. It provides a wide range of features and integrations with various data stores and services. The project is organized into a main solution with many individual projects, each focusing on a specific feature or integration. It also includes the Solnet library as a git submodule, which provides functionality for interacting with the Solana blockchain.

**Main Technologies:**

*   .NET
*   C#
*   Docker
*   PostgreSQL
*   Redis
*   Microsoft Garnet
*   Vault
*   RabbitMQ
*   Elasticsearch
*   Kibana
*   Grafana
*   Logstash

**Architecture:**

The project follows a modular architecture, with each feature or integration implemented as a separate project. The main solution, `Electra.sln`, brings all these projects together. The `docker-compose.yml` file defines the services that the project can connect to, allowing for a flexible development and deployment environment.

## Building and Running

**Building the project:**

To build the project, you can use the `dotnet build` command in the `src` directory:

```bash
cd src
dotnet build
```

**Running the project:**

The project is designed to be run with Docker. The `docker-compose.yml` file in the `src` directory defines the services that the project can connect to. To run the project, you can use the `docker-compose up` command:

```bash
cd src
docker-compose up
```

This will start all the services defined in the `docker-compose.yml` file. You can then run the main application, which will connect to these services.

**Testing the project:**

The project includes a number of test projects, which can be run using the `dotnet test` command:

```bash
cd src
dotnet test
```

## Development Conventions

*   **Git Flow:** The project uses the Git Flow branching strategy. All work should be done on feature branches, and then merged into the `develop` branch via a pull request.
*   **Thin Controllers:** MVC controllers should be small and thin.
*   **Submodules:** The project uses a git submodule for the Solnet library. Remember to push changes from both the main git repository and the sub-module if modifications are made there.
*   **SOLID Principles:** The project follows the SOLID principles of object-oriented design.
*   **DRY (Don't Repeat Yourself):** The project follows the DRY principle.
