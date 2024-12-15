# Yuki Bot

## Development Setup

### Prerequisites

You will need to create your own Discord bot for testing changes during local development.
Log in to the [Discord developer portal](https://discord.com/developers) and create a new application.
Enable the bot user and copy the token provided.  Add your bot to a server you'd like to use for testing.

The following commands should be ran from inside the project directory:
```bash
cd YukiBot
```

Create a local environment configuration:
```bash
cp .env.example .env
```
Update the new file with your own Discord Bot token to enable testing.

### Running the Application

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
