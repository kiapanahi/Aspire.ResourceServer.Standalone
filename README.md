# A standalone resource server for Aspire dashboard

> Based on [this](https://github.com/dotnet/aspire/discussions/4440) thread.

> Related [GitHub discussion](https://github.com/dotnet/aspire/discussions/6772) in main [dotnet/aspire](https://github.com/dotnet/aspire) repo.


## Todo

- [x] Standalone resource server
- [x] Collecting external containers' logs
- [ ] Handling commands to external containers

## Test environment (manual) using Docker

To test the resource server, there's a sample docker compose file in the `compose` directory. Either start the compose file
manually by

```bash
docker compose -f compose/compose.yaml up -d
```

Or use the facilitator scripts (`start-compose.ps1`, `start-compose.sh`).

This compose file includes:
- An Aspire dashboard container
- A RabbitMQ container (as a sample of any message bus)
- A Redis container (as a sample of any key-value store)
- A MongoDB container (as a sample of any NoSQL database)

To cover most of the external workloads that teams who do not or cannot use the Aspire application model due to different stack or any other reason usually use.

## Test environment (manual) using Minikube

To test the resource server running as a container in Minikube, please run one of the facilitator scripts (`start-minikube.ps1`, `start-minikube.sh`).
Please note that Minikube must be running in order for the scripts to work.

This sample setup includes:
- An Aspire dashboard container
- A RabbitMQ container (as a sample of any message bus)
- A Redis container (as a sample of any key-value store)

Once you've ran either of the scripts, please start the application itself through the solution.
To use the Aspire dashboard, please port forward the Aspire Dashboard container so you can access it through your browser.


## Contributing

Feel free to contribute to this project by opening an issue or a pull request.
