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

## Architecture

Yuki is built on top of [Discord.NET](https://docs.discordnet.dev/) with an architecture inspired by ASP.NET Core
that leans heavily on Dependency Injection (DI).  The entrypoint to the application is `Program.cs` which creates
then runs a bot client using `ClientBuilder`.

### Services

Services are added to the DI container using the `ClientBuilder.ConfigureServices` method.  In order
to function, an `IClientToken` service must be registered.  This service provides the client with
the secret bot token used to authenticate with Discord's gateway API.  In the case of Yuki,  the `AddConfigService`
extension method handles registration of the `IClientToken` service using the token available in the Environment.

In addition to the services added in the call to `ClientBuilder.ConfigureServices`, an instance of `IDiscordClient`
is always available for injection and can be used to interact with the Discord API when there isn't any
existing context available.

### Components

Components are the building blocks of the Discord bot.  They are special services that implement specific interfaces
and are added to the client using `ClientBuilder.AddComponent`.  There are currently two interfaces that can
be implemented to define a component.

#### Jobs (`IJob`)

A Job is a component that implements the `IJob` interface.  A Job runs a method `OnJobTriggered` on a specified interval `Interval`.

#### Message Handlers (`IMessageHandler`)

A Message Handler is a component that implements the `IMessageHandler` interface.  A Message Handler runs a method `OnMessageReceiveAsync` whenever the bot receives a message from another user.  A message object is passed to the
method as a parameter.

### Database

Yuki uses Azure's CosmosDB, a document DB that's easy to query and doesn't require strict pre-defined schemas.
Entities stored in the database are written under `Models/` and extend the `CosmosModel` abstract class.  Database
operations are performed through `CosmosService` which provides methods for fetching and updating documents.
Documents in the database are tagged with their type name so they can be filtered and deserialized to explicit,
statically typed classes.  This is handled automatically by `CosmosService` and `CosmosModel`.
