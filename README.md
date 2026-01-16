# group-05-CQRS-synchronization

Build a CQRS Synchronization between MongoDB (Write) and MySQL (Read)

By Youri Haentjens, Pratik Lohani, Lander Debeir & Matse De Deyn

## Introduction

This project provides a **CQRS (Command Query Responsibility Segregation) synchronization system** that keeps a MongoDB write database and a MySQL read database in sync in near real-time.

**What is it?**  
A production-ready synchronization service that automatically propagates changes from MongoDB (optimized for writes) to MySQL (optimized for reads and queries), enabling you to separate write and read concerns in your application architecture.

**Who is it for?**  
Developers and teams building applications that need to:
- Scale read and write operations independently
- Use different databases optimized for different purposes (document storage vs. relational queries)
- Implement event-driven architectures with reliable data synchronization

**Why use it?**  
- **Separation of Concerns**: Keep write models (MongoDB aggregates/entities) separate from read models (MySQL projections)
- **Performance**: Optimize each database for its specific purpose
- **Reliability**: Built-in recovery and replay mechanisms ensure no data loss
- **Easy Integration**: One-command Docker setup with clear configuration
- **Production-Ready**: Includes monitoring, logging (SEQ), idempotent updates, and comprehensive error handling

Built with Domain-Driven Design (DDD) principles and follows the Ports-and-Adapters (Hexagonal) architecture pattern.

## Contents
- [group-05-CQRS-synchronization](#group-05-cqrs-synchronization)
  - [Contents](#contents)
  - [Usage](#usage)
  - [Features](#features)
  - [Configuration](#configuration)
    - [How to get SEQ api key](#how-to-get-seq-api-key)
  - [Structure of Events](#structure-of-events)
  - [Demo](#demo)
  - [Tests](#tests)
  - [Troubleshooting](#troubleshooting)
    - [Databases are not yet started](#databases-are-not-yet-started)
    - [Events are formatted incorrectly](#events-are-formatted-incorrectly)
    - [No primary key in my database](#no-primary-key-in-my-database)
    - [Insert doesn't work](#insert-doesnt-work)
    - [String is not correctly loaded in the database](#string-is-not-correctly-loaded-in-the-database)
    - [Update and delete are doing difficult about numbers and booleans](#update-and-delete-are-doing-difficult-about-numbers-and-booleans)
    - [New events are not getting processed](#new-events-are-not-getting-processed)
    - [Where are the logs located](#where-are-the-logs-located)
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

## Features

- (CQRS) Synchronization between a read and write database.
- Recovery and Replay mechanisms.
- Monitoring of logs (via SEQ).
- Webapi (on the webApi branch and accessible via http://localhost:5000/swagger/index.html once you launch it).

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
    depends on the type of event
    - for insert it will be the properties including id,
    - for update it will be the condition and the changes
    - for delete it will be the condition 
    
    below here are how the types of the values should be structured
    */
    "string": "'value'", // don't forget the single quotes around the value
    "number": 123, // if using a number inside a update or delete it should be wrapped inside double quotes.
    "boolean": true | false, // if using a boolean inside a update or delete it should be wrapped inside double quotes.
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
    "milage": 10,
    "driving": true,
    "name": "'BMW'",
    "price": 50500.58
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
      "price": "* 5", // You can place only the wanted change.
      "milage": "milage * 10", // You can repeat the property name if wanted.
      "name" : "'Audi'" // "name = 'Audi'" is also correct.
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

The [demo (in the repos below)](#links) is a simple user management application using a MongoDB command database and a MySQL query database

## Tests
In order to run the integration tests you have to first setup the docker environment by running
```bash
docker compose -f ./test/IntegrationTests/config/docker-compose.yml up -d
```
Then you can run all tests (unit and integration) by running
```bash
dotnet test
```

## Troubleshooting
*A failed event shall not block the synchronisation from further processing*

### Databases are not yet started
wait until the databases are started and try again later

### Events are formatted incorrectly
Problems with events can be found in logging.

### No primary key in my database
The service recognizes the property id as primary key if this is not available it will not put a primary key in the database the format of this id is the size of a GUID.

### Insert doesn't work
It is possible that the property names used contain illegal characters. The illegal characters are *, /, -, +, =, <, > and depending on the used querydatabase this list may differ.

### String is not correctly loaded in the database
You have to wrap the string values explicity with single quotes.

```json
// Correct
"payload": {
  "name": "'John'",
  "lastname": "'West'"
}

// Incorrect
"payload": {
  "name": "John",
  "lastname": "West"
}
```

### Update and delete are doing difficult about numbers and booleans
You can't use pure booleans and numbers inside update and delete there is a condition or change expected and should be placed inside a string.

```json
// Correct
"payload": {
  "condition": {
    "milage": "10",
    "price": "price > 5000",
    "horsepower": ">120"
  },
  "change": {
    "price": "5",
    "driving": "true"
  }
}

// Incorrect
"payload": {
  "condition": {
    "milage": 10,
    "price": "price > 5000", // this part is still correct
    "horsepower": ">120" // this part is still correct
  },
  "change": {
    "price": 5,
    "driving": true
  }
}
```

### New events are not getting processed
Verify the status of the event this should be "PENDING" when sending. 

After getting processed the status should update to "DONE" and you will see this inside the logs. If this is not the case check the logs and verify used values in your event.

### Where are the logs located
The logs can be found inside *SEQ* and in the running synchronisation service.

## Links
- [Demo frontend](https://github.com/howest-ti-sep/group-05-CQRS-demo-frontend)
- [Demo backend](https://github.com/howest-ti-sep/group-05-CQRS-synchronization-demo-backend)
- [Demo environment (one command to run)](https://github.com/howest-ti-sep/group-05-cqrs-synchronization-environment-production)
- [Research](./RESEARCH.md)
- [Architecture](https://miro.com/welcomeonboard/bTVjNWdhY0FmTHB6WWFMUDNqQ25veHhUczI2SnFHN1gwVlMxa0xJRVlqMFdXMEJWSVdmcXdGWlFNRjlTbGpJdUNwcDQ2V3kzMFBDZmcwYTBldENnVDUwRkpVZ1Mvby9VeldlTnpCRzZSNGZuVmNqQ1VkUy9aZ3ZoZTYyWDFSY3RBd044SHFHaVlWYWk0d3NxeHNmeG9BPT0hdjE=?share_link_id=954245417354)
