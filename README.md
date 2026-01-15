# group-05-CQRS-synchronization

Build a CQRS Synchronization between MongoDB (Write) and MySQL (Read)

By Youri Haentjens, Pratik Lohani, Lander Debeir & Matse De Deyn

## Contents
- [Usage](#usage)
- [Configuration](#configuration)
    - [How to get SEQ api key](#how-to-get-seq-api-key)
- [Structure of events](#structure-of-events)
- [Demo](#demo)
- [Tests](#tests)
- [Links](#links)

## Usage

In order to utilize the application, you will have to build the container by running:

```bash
dotnet publish src/Main/Main.csproj --os linux --arch x64 /t:PublishContainer               
```

Then you can utilize it in your application by adding it to a docker compose file:

```yaml
services:
    command-db:
        #command db configuration
    
    query-db:
        #query db configuration
    
    seq:
      image: datalust/seq
      container_name: seq
      restart: always
      volumes
        - #data storage configuration
      env_file: .seq.env
      ports:
        - #configure ports
      networks
        - #network name

    cqrs-sync:
        image: cqrs-sync:latest
        restart: always
        depends_on:
            command-db: service_healthy
            query-db: service_healthy
        env_file: .cqrs-sync.env
```

If the environment variables are correcly set, the service will automatically connect to the two databases and update the query database when necessary.

## Configuration

A short explanation of the necessary environment variables:

- CONNECTION_STRING_COMMAND_DB: connection string of the command database
- CONNECTION_STRING_QUERY_DB: connection string of the query database
- SEQ_SERVER_URL= url of the SEQ log monitoring dashboard
- SEQ_API_KEY= API key of the SEQ log monitoring dashboard

### How to get SEQ api key
In order to monitor the logs, the seq framework is provided in the docker compose. However the API key will still need to be provided. This section explains how you can obtain it.

First, you will need to compose the containers. Then you visit the seq application (the link to the application can be found in docker).

When you launch the application you will arrive on this screen.
![SEQ dashboard](./images_readme/seq5.png)

From there you will need to click on the icon in the top right corner. This will open a menu in which you will need to select "API keys"(the circled option)
![SEQ submenu](./images_readme/seq1.png)

You will arrive on this screen:
![SEQ api keys](./images_readme/seq2.png)
Click on the add API key button and fill in the following form:
![SEQ API key form](./images_readme/seq3.png)
Just give it a title and set the minimum level to Information and click Save changes.

You will get a  pop-up showing the api key, which you need to copy and put in the env files

## Structure of Events
Events are structured like this

```json
{
  "eventId": "c7a15639-872a-40bc-af08-7d790740b2fa", //UUID
  "occurredAt": "2026-01-15T09:59:00Z",
  "aggregateName": "string",
  "status": "PENDING",
  "eventType": "INSERT" | "UPDATE" | "DELETE",
  "payload": {
    /*
    depends on the typze of event
    - for insert it will be the properties incluiding id,
    - for update it will be the condition and the changes
    - for delete it will be the condition 
    
    below here are how the types of the values should be structured
    */
    "string": "'value'", // don't forget the single quotes around the value
    "number": "123",
    "boolean": "true" | "false",
  }
}
```
The status when sent should be `PENDING`. When it is handled the status will change to `DONE`.

Below are some examples:
```json
//INSERT
{
  "id": "491ba616-fe56-4a01-a86f-0efd7913a73b",
  "aggregateName": "cars",
  "eventType": "INSERT",
  "occurredAt": "2026-01-03T16:08:01.139Z",
  "payload": {
    "id": "'15c17874-33ce-4d18-ad09-4fec29f22d2e'",
    "milage": "10",
    "driving": "true",
    "name": "'BMW'",
    "price": "50500.58"
  },
  "status": "PENDING"
}

//UPDATE
{
  "id": "c56423c2-ec67-4202-9646-a08be1386a92",
  "aggregateName": "cars",
  "eventType": "UPDATE",
  "occurredAt": "2026-01-14T10:18:13.000Z",
  "payload": {
    "condition": {
      "milage": "10",
      "price": "price > 50000"
    },
    "change":{
      "price": "* 5",
      "name" : "'Audi'"
    }
  },
  "status": "PENDING"
}

//DELETE
{
  "id": "abf41769-6efb-4f94-b5dd-d9442ea6d8e8",
  "aggregateName": "cars",
  "eventType": "DELETE",
  "occurredAt": ISODate("2026-01-14T10:18:13.000Z"),
  "payload": {
    "condition": {
      "milage": ">= 10",
      "price": "price > 50000"
    },
  },
  "status": "PENDING"
}
```

## Demo

The demo (in the repos below) is a simple user management application using a MongoDB command database and a MySQL query database

## Tests
In order to run the integration tests you have to first setup the docker environment by running
```bash
docker compose -f ./test/IntegrationTests/config/docker-compose.yml up -d
```
Then you can run all tests (unit and integration) by running
```bash
dotnet test
```



## Links

- [Demo frontend](https://github.com/howest-ti-sep/group-05-CQRS-demo-frontend)
- [Demo backend](https://github.com/howest-ti-sep/group-05-CQRS-synchronization-demo-backend)
- [Demo environment (one command to run)](https://github.com/howest-ti-sep/group-05-cqrs-synchronization-environment-production)
- [Research](./RESEARCH.md)
- [Architecture](https://miro.com/welcomeonboard/bTVjNWdhY0FmTHB6WWFMUDNqQ25veHhUczI2SnFHN1gwVlMxa0xJRVlqMFdXMEJWSVdmcXdGWlFNRjlTbGpJdUNwcDQ2V3kzMFBDZmcwYTBldENnVDUwRkpVZ1Mvby9VeldlTnpCRzZSNGZuVmNqQ1VkUy9aZ3ZoZTYyWDFSY3RBd044SHFHaVlWYWk0d3NxeHNmeG9BPT0hdjE=?share_link_id=954245417354)
