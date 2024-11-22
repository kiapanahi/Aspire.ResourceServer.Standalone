# A standalone resource server for Aspire dashboard

> Based on [this](https://github.com/dotnet/aspire/discussions/4440) thread.


## Todo

- [x] Standalone resource server
- [ ] Collecting external containers' logs
- [ ] Handling commands to external containers

## Test environment (manual)

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


## Contributing

Feel free to contribute to this project by opening an issue or a pull request.