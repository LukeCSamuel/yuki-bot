# Yuki Bot

## Development Setup

Create a local environment configuration:
```bash
cp .env.example .env
```
Update the new file with your own Discord Bot token to enable testing.

Start the application using Docker:
```bash
docker compose up -d
```

Note that it may take a few minutes for the CosmosDB emulator container to start up.

You may need to restore packages after starting the containers since they are included in the volume
mount:
```bash
dotnet restore
```
