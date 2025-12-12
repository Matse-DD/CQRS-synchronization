# group-05-CQRS-synchronization

Build a CQRS Synchronization between MongoDB (Write) and MySQL (Read)

By Youri Haentjens, Pratik Lohani, Lander Debeir & Matse De Deyn

## Usage

In order to utilize the application, you will have to build the container by running:

```bash
dotnet publish src/Main/Main.csproj --os linux --arch x64 /t:PublishContainer               
```

Than you can utilize it in your application by adding it to a docker compose file:

```yaml
services:
    command-db:
        #command db configuration
    
    query-db:
        #query db configuration

    cqrs-sync:
        image: cqrs-sync:latest
        restart: always
        depends_on:
            command-db: service_healthy
            query-db: service_healthy
        env_file: .cqrs-sync.env

```

if environment variables are correcly set, the service will automatically connect to the two databases and update the query database when necessary.

## Environment variables

a short explanation of the necessary environment variables:

- ASPNETCORE_ENVIRONMENT: environment in which the service runs in (`Production` reccomended)
- CONNECTION_STRING_COMMAND_DB: connection string of the command database
- CONNECTION_STRING_QUERY_DB: connection string of the query database

## Demo

- [Demo frontend](https://github.com/howest-ti-sep/group-05-CQRS-demo-frontend)
- [Demo backend](https://github.com/howest-ti-sep/group-05-CQRS-synchronization-demo-backend)
