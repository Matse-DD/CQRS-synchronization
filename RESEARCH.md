# Research flow

## Define your research objective

- Wat probleem lossen we op?
  - Synchronisatie tussen 2 databanken zonder dat er data verlies optreed voor CQRS doeleinden.
- Wie zijn de gebruikers?
  - Developers dat gebruik willen maken van CQRS (Command Query Responsibility Segregation)
- Hoe weten we of het werkt?
  - Indien we een write doen naar de command databank en deze veranderingen zichtbaar worden in de query databank.
- Wat is in de scope wat is er buiten
  - Synchronisatie
    - Idempotent (geen dubbele events)
    - Geen dataverlies
    - Mogelijkheid tot herstel
  - Ervoor zorgen dat de databanken loosly coupled zijn
  - Event sourcing (op later moment)

## Identify stakeholders and use cases

De stakeholders zullen vooral developers zijn alsook de product owner.

Developer: (duplicate info, data verlies, ...)

- Als een developer wil ik een performant product afleveren zodat de gebruikers geen hinder ondervinden van wat er allemaal gebeurd achter hun rug.
- Als een developer wil ik databanken kunnen synchroniseren zonder problemen zodat ik zonder problemen CQRS kan toepassen over 2 databanken.
- Als een developer wil ik databanken gemakkelijk terug synchronseren indien er een inconsistentie is zodat de databanken gelijk lopen wat betreft data.
- Als een developer wil ik dat indien er bepaalde commands onbedoeld dubbel worden uitgezonden deze niet dubbel worden uitgevoerd (idempotent) zodat mijn data niet inconsistent wordt.
- Als een developer wil ik dat indien de command databank onbereikbaar is ik nog steeds informatie kan opvragen zodat het opvragen van gegevens geen impact ondervind.
- Als een developer wil ik dat indien er een onderdeel van de synchronisatie faalt er geen data verlies optreed zodat ik er zeker van kan zijn dat mijn data tussen de 2 databanken gelijk is.
- Als een developer wil ik dat het verkeer van queries & commands verdeeld is over de databanken zodat er bij veel verkeer geen impact is.
- Als een developer wil ik gemakkelijk een container opbouwen van het CQRS systeem zodat deze gemakkelijk te integreren valt.
- Als een developer wil ik een demo applicatie dat de synchronisatie flow aantoont zodat ik er zeker van ben dat het CQRS systeem werkt.
- Als een developer wil ik dat events die niet verwerkt kunnen worden, apart worden gezet (Dead Letter Queue) zodat ik deze kan analyseren en later opnieuw kan 'replayen' zonder de rest van de queue te blokkeren zodat de events verder kunnen gaan zonder problemen. (Could Have)
- ...

Product owner:

- Als de product owner wil ik dat mijn product met zo weinig mogelijk down-time kan werken zodat ik geen klanten verlies omdat mijn product weeral offline is.
- Als de product owner wil ik dat mijn product niet volledig kapot draait indien er een databank niet meer werkt zodat er steeds een deel werkend zal zijn.
- ...

## Master core concepts

### CQRS concept

CQRS ook gekend als Command Query Responsibility Segregation is op het hoogste niveau niet meer dan het opsplitsen van hoe je commands (acties) en queries (opvragen van gegevens) uitvoert op een databanken. Deze opsplitsing gebeurd al op een architetureel niveau waardoor je een aparte service zal hebben voor de queries & commands.

In dit project word er nog een stap verder gegaan en is de databank waar de commands op worden uitgevoerd een andere databank dan deze waar er word van gelezen.

- https://www.geeksforgeeks.org/system-design/cqrs-command-query-responsibility-segregation/
- https://cqrs.wordpress.com/about/
- https://www.arnaudlanglade.com/difference-between-cqs-and-cqrs-patterns/

### CQRS Synchronisatie

Zoals zonet vermeld kan je dus gaan voor aparte databanken voor CQRS. De moeilijkheid hieraan is hoe ga je ervoor zorgen dat de gegevens tussen de twee databanken hetzelfde is. Hiervoor zijn er verschillende opties zoals in dit research document te zien is.

- https://eventuate.io/docs/manual/eventuate-tram/latest/distributed-data-management.html
- 

### Projector

Een projector zet het evenement of de verandering in data om naar een correct command, zodat de query databank correct kan worden geüpdatet

### Command databank

Op de command databank worden enkel de commands van de gebruiker uitgevoerd. Dit wilt zeggen dat indien de gebruiker informatie wilt aanpassen deze databank zal worden gebruikt.

### Query databank

De query databank word enkel gebruikt om queries uit te voeren dit wilt dus zeggen dat indien de gebruiker informatie wilt opvragen deze databank zal worden gebruikt.

### Event Sourcing

Dit is het principe van het opslaan van verschillende events en deze events toe te passen op data. Het is dan mogelijk om naar een vorige staat van de data terug te keren door de events terug af te spelen vanaf de start staat. Of door het omgekeerde van de events uit te voeren dit hangt af van de implementatie. Event sourcing zorgt voor een duidelijk overzicht van welke acties allemaal ondernomen zijn op de data waardoor de momentele staat bereikt is.

- https://microservices.io/patterns/data/event-sourcing.html
- 

## Compare existing solutions

### Debezium (https://debezium.io/)

Deze oplossing kijkt naar veranderingen in de command databank met behulp van polling eenmaal een verandering word opgemerkt en vertaalt naar events. Vervolgens worden deze events op een message broker (kafka) gepusht. Waar dan naar geluisterd kan worden door verschillende processen deze zullen dit event dan ontvangen. Onder deze processen zal dan een process zijn dat de ontvangen messages omzet naar de juiste commands en deze uitvoeren op de query databank.

### Axon Framework door Axoniq (https://www.axoniq.io/framework)

Deze oplossing is meer Event Sourcing specifiek en zal dus evenementen opslaan in een databank ook gekend als de event store. Er is ook een Tracking Event Processor dat door polling op de hoogte word gebracht van nieuwe events. De Tracking Event Processor houd bij welk event het laatst afgehandeld is. Dit is op basis van de Tracking Token deze geeft weer op welke positie het event is in de event store. De Tracking Event Processor kan dan gewoon kijken naar het volgende Tracking Token voor het volgende event.

De query databank word aangepast door met projections van de events naar een correct commando. Eenmaal dit gelukt is word de Tracking Token geupdate naar de Tracking Token van het zojuiste geslaagde event.

### Revo Framework (https://docs.revoframework.net/)

De verschillende events worden in deze oplossing ook opgeslagen in een Event Store en op een event bus gezet. De nieuwe events komen via de event bus in een async event queue terecht waarna eventlisteners de projectors op de hoogte brengen. Deze projectors zetten de events dan opnieuw om naar commands voor de query databank. Door de event store kan gemiste events worden afgehandeld bij heropstart.

## Define requirements

Functionele requirements:

- Gegarandeerd idempotente updates (geen duplicate events)
- Heropstart/replay mechanisme om mogelijke inconsitenties op te vangen
- Write & read operaties maken gebruik van andere databank en zijn loosly-coupled (niet van elkaar afhankelijk)
- Demo applicatie voor de synchronisatie flow te demonstreren in een echte app.
- Een docker container voor de syncronisatie flow & demo applicatie

Niet functionele requirements:

- Betrouwbaar -> geen data verlies, ...
- Performantie -> synchronisatie binnen enkele seconden
- Testbaarheid -> meer dan 80% test coverage
- Observeerbaar -> logs & metrics van de status
- Reproduceerbaar
- Documentatie, keuzes en gebruik van bepaalde mogelijkheden
- Gebruik van DDD-model
- Gemakkelijk te gebruiken

### Acceptance checkpoint (IS DIT WELL CORRECT NAKIJKEN)

De MVP is een demo applicatie dat gebruik maakt van CQRS met onze synchronisatie implementatie tussen een mongoDb (command databank) en mysql (query databank) dit met een hoge betrouwbaarheid en snelheid.

## Technologie & Architectuur Opties

### Programmeertalen

#### C#

Dit is een programmeertaal gemeaakt door Microsoft. Een groot voordeel aan C# is het .NET eco-systeem hier zijn verschillende libraries aanwezig waar handig gebruik kan worden van gemaakt. E

| Technologie                       | Type        | Voordelen                                                                                                                           | Nadelen                                                                                                     | Conclusie                                                                                                                       |
| :-------------------------------- | :---------- | :---------------------------------------------------------------------------------------------------------------------------------- | :---------------------------------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------ |
| **.NET (C#) / ASP.NET**     | Managed     | **Sterke Type-veiligheid** (ook runtime), volwassen ecosysteem (MediatR, EF Core), snelle development door krachtige tooling. | Iets zwaarder dan Go of Node.js, maar verwaarloosbaar voor deze use-case.                                   | **Gekozen.** Biedt de beste balans tussen veiligheid, snelheid van ontwikkelen en robuuste frameworks voor CQRS.          |
| **Java**                    | Managed     | Vergelijkbaar met C# qua robuustheid en type-veiligheid.                                                                            | Vaak meer boilerplate code nodig; team expertise ligt sterker bij C#.                                       | **Afgevallen.** Technisch capabel, maar minder efficiënt gezien de huidige stack-voorkeur.                               |
| **TypeScript (Node/Deno)**  | Interpreted | Snel op te zetten, JSON-native (handig voor MongoDB).                                                                               | **Weak typing bij runtime**: data-integriteit is lastig te garanderen. Minder volwassen CQRS-tooling. | **Afgevallen.** Risico op data-inconsistentie door gebrek aan strikte runtime types is te groot voor een sync-applicatie. |
| **Go (Golang)**             | Compiled    | Zeer hoge performance, simpele concurrency.                                                                                         | Minder krachtige ORM's/Frameworks vergeleken met EF Core. Verbose error handling vertraagt development.     | **Afgevallen.** Performance winst weegt niet op tegen het gemis aan enterprise features (zoals MediatR).                  |
| **Systeemtalen (C++/Rust)** | Low-level   | Maximale controle en performance.                                                                                                   | **Hoge complexiteit.** Geheugenbeheer is handmatig. Development tijd is drastisch langer.             | **Afgevallen.** Te complex ("heavy lifting" zelf doen) voor een architectuur-gefocust probleem.                           |

| Patroon                     | Omschrijving                                                                               | Voordelen                                                                                                   | Nadelen                                                                                           | Conclusie

### Programmeertaal (extra voordelen/nadelen op schrijven is nu precies wat weinig)

#### Low-level programmeertalen

- Voordelen
  - Performant
  - Heel veel controle over wat er gebeurd
- Nadelen
  - Memory management
  - Minder cadeau (zelf meer schrijven)
    - een betrouwbare ORM waar je gemakkelijk mee kan werken is zelfdzaam dus dan zou je zelf veel meer databank connecties en dergelijke moeten verzorgen wat ook niet vanzelf sprekend is in een low level programmeertaal

#### Typescript

- Voordelen
  - Lichte development stack
  - Zeer goede documentatie
- Nadelen
  - Weak typing tijdens het draaien van de applicatie wat voor onverwachte problemen kan zorgen
  - Veel libraries ondersteunen nog niet alles en vertrouwen ook op veel onderliggende libraries
    - Een goede ORM dat voor verschillende databanken kan worden ingezet is moeilijk te vinden.

#### C#

- Voordelen
  - Een zeer goede ORM (EntityFramwork)
  - Strong typing
  - Goede documentatie
  - Containerisatie is ingebouwd
- Nadelen
  - Veel verschillende manieren om het zelfde te implementeren
    - Je hebt 3 verschillende manieren om een constructor te gebruiken in C#

#### Java

- Voordelen
  - Strong typing
  - Om iets te doen zal je syntax gewijs meestal maar 1 optie hebben
    - Constructors
  - Verbose
- Nadelen
  - Boilerplate
  - Documentatie is minder goed

#### Conclusie

| Criterium                 | Low-level (C++/Rust) | TypeScript (Node.js)       | Java                    | C# (.NET)                    |
| :------------------------ | :------------------- | :------------------------- | :---------------------- | :--------------------------- |
| **Performantie**    | Extreem hoog         | Gemiddeld                  | Hoog                    | Hoog                         |
| **Type Veiligheid** | Strikt               | Matig (enkel compile-time) | Strikt                  | Strikt                       |
| **ORM Kwaliteit**   | Beperkt / Complex    | Matig                      | Goed (veel boilerplate) | Uitstekend (EF Core)         |
| **Dev Snelheid**    | Laag                 | Zeer hoog                  | Gemiddeld               | Hoog                         |
| **Documentatie**    | Versnipperd          | Zeer goed                  | Verspreid               | Uitstekend (Gecentraliseerd) |
| **Containerisatie** | Handmatig            | Goed                       | Goed                    | Uitstekend (Native)          |

**Waarom geen Systeemtalen of TypeScript?**

Bij de taalkeuze zijn zowel low-level systeemtalen (C++, C, Rust) als TypeScript (Deno, Bun, Node) afgevallen, elk omwille van specifieke beperkingen ten opzichte van onze architecturale doelen.

**Systeemtalen** zijn niet weerhouden vanwege de hoge complexiteit en de aanzienlijke hoeveelheid handmatig werk. Hoewel deze talen zeer performant zijn indien correct gebruikt, krijg je "minder cadeau" van het platform; zaken zoals geheugenbeheer moet je zelf afhandelen. Gezien onze focus ligt op de architectuur van een complex synchronisatieproces tussen twee databanken, is deze extra laagdrempelige complexiteit niet gewenst. We verkiezen talen (zoals C# of Java) waarbij frameworks en ORM-ondersteuning het "heavy lifting" doen, wat efficiënter is voor onze gelimiteerde ontwikkeltijd.

**TypeScript** viel af omdat het ecosysteem rondom CQRS en ORM nog niet zo volwassen is als dat van de gevestigde waarden. Daarnaast is de type-veiligheid een kritiek punt. TypeScript is strongly typed tijdens compile-time, maar wordt loosely typed tijdens runtime ("weak typing"). Dit betekent dat als data binnenkomt in een ander formaat dan verwacht, de applicatie niet noodzakelijk klaagt en gewoon verdergaat. Voor een datasync-applicatie die leunt op CQRS, waarbij de correctheid van datastructuren cruciaal is, vormt dit gebrek aan strikte runtime-controle een te groot risico.

Zowel Java als C# zijn zeer goede kandidaten. Ze zijn allebei zeer betrouwbaar en goed uitgewerkt. Met een volwaarde eco-systeem en ondersteuning. Uiteindelijk  zowel Java als C# zijn goede kandidaten de reden dat er uiteindelijk voor C# en .NET gekozen is omdat de documentatie beter is. ... (nog wat redenen)

### CDC mogelijkheden

Ook gekend als Change Data Capture dit is een design pattern dat gebruikt word om veranderingen waar te nemen in een databank. Er zijn verschillende mogelijkheden hieronder enkele opties.

### Log-based

Deze methode zal de veranderingen waarnemen door steeds naar de transactie logs te vragen. En hier dus veranderingen in zal waarnemen.

#### Trigger-based

Deze optie maakt gebruik van databank triggers om veranderingen te zien. Indien er iets veranderd in de databank zal dan een stukje code worden getriggerd en veranderingen aangeven.

#### Query-based

Deze methode zal de databank regelmatig aanvragen en kijken of de current state gelijk is met de vorige state dat het heeft binnen gevraagd. De verschillende veranderingen worden dan gezien.

#### Change Stream

Deze optie is ingebouwd in MongoDB en is dus mongoDB specifiek. MongoDB zal steeds naar iedereen dat het horen wilt sturen wat er veranderd is aan de databank. Hier kan dan op worden gesubscribed.

|               | Schaalbaarheid | Impact    | Latentie  | Volledigheid |
| ------------- | -------------- | --------- | --------- | ------------ |
| Log-based     | Zeer goed      | Zeer laag | Laag      | Volledig     |
| Trigger-based | Laag           | Hoog      | Gemiddeld | Volledig     |
| Query-based   | Minder goed    | Gemiddeld | Hoog      | Laag         |
| Change stream | Zeer goed      | Zeer laag | Zeer laag | Volledig     |

Er is gekozen voor de change stream optie voor de vollegende reden. Change stream heeft bijna dezelfde voordelen als log-based maar scoort beter op latentie dit komt doordat er niet moet worden gepolled (constant vragen of er iets nieuw is). Verder zal er gedurende het project gewerkt worden met mongoDb zoals beschreven in de projectbeschrijving. Indien we toch een andere command databank zouden willen gebruiken is de overstap niet groot naar een andere CDC optie. Het enige wat de CDC uiteindelijk moet doen is aangeven wat er veranderd is in de databank.

### CQRS Synchronisatie mogelijkheden

#### Direct projection

Een direct projection controleert de write database continu op veranderingen in de data. Zodra een verandering wordt waargenomen, projecteert de projector deze direct naar de read database, die vervolgens wordt bijgewerkt.

Een direct projection is echter geen optimale oplossing: het biedt geen mogelijkheid om data later te herstellen en het wordt moeilijk om gemiste veranderingen alsnog te verwerken. Bovendien kunnen veranderingen niet meer worden gedetecteerd zodra ze zijn gepasseerd.

![Foto van direct projection architectuur](images_research/direct_projector_synchronisation.png)

#### Outbox

Deze architectuur slaat de verschillende events op in een table/collectie in de databank. Vervolgens kan dan gekeken worden naar de outbox voor de verschillende evenementen. Wanneer er een verandering optreedt, wordt deze doorgegeven aan een projector, die de aanpassing toepast op de query databank.

Recovery is mogelijk door het laatste geslaagde event bij te houden. Op basis van dit event kan je bepalen wat het volgende event is dat aanwezig is in de outbox. 

Verder is dit atomisch door dat er gebruik word gemaakt van de databank transacties. En omdat de projection kan zeggen tegen de outbox dat een event gelukt is of niet. Dit zorgt ervoor dat er geen Dual-write problem is.

![Foto van outbox architectuur](images_research/outbox_synchronisation.png)

#### Message/Event Broker

Deze oplossing maakt gebruik van een message broker en polling. Je kan een message broker zo worden geconfigureerd dat deze de verschillende events persistent bijhoudt, wat dus wilt zeggen dat de events niet verloren zal gaan indien de message broker uitvalt. Er is eigenlijk geen gemakkelijke manier om het dual-write problem op te lossen. Tenzij je gebruik zou maken van een outbox hiervoor.

![Foto van broker architectuur](images_research/broker_synchronisation.png)

#### Conclusie

|                            | Dual-write problem                               | Schaalbaarheid | Recovery | Event Sourcing later | Snelheid  | Complexiteit |
| -------------------------- | ------------------------------------ | -------------- | -------- | -------------------- | --------- | ------------ |
| **Direct Projector** | Aanwezig                                 | Niet           | Niet     | Slecht               | Normaal   | Zeer simpel  |
| **Outbox**           | Opgelost                            | Goed           | Goed     | Goed                 | Goed      | Complex      |
| **Message Broker**   | Mogelijkheid tot (meer complexiteit) | Zeer goed      | Goed     | Goed                 | Zeer goed | Zeer complex |

De direct projector is geen goede optie aangezien het bij een mogelijk falen van de databank niet zal kunnen recoveren.

De message broker is de meest schaalbare optie maar is zeer complex om te implementeren, verder kan je niet zonder extra complexiteit garanderen dat een event uitgevoert is op de query databank.

De outbox is stabiel en betrouwbaar en is ook de enige manier die zonder veel moeite kan garanderen of een event wel uitgevoerd is of niet.

### Uitkomst

Uiteindelijk is er gekozen voor de volgende technologieën & architectuur opties. C# als programmeertaal, Change Stream als CDC (data veranderingen waarnemen) en voor outbox om de CQRS synchronisatie te regelen. Dit met enkele veranderingen om exactly-once processing te verkrijgen.

Wat het volgende schema maakt:
![Foto gekozen architectuur combinatie outbox + change stream](./images_research/outbox_change_stream_sync.png)

## Run small Proofs of Concept (PoCs)

### Change Stream met MongoDB in Dotnet
Dit codevoorbeeld toont hoe je subscribed op een change stream in dotnet. Als je kan zien is dit redelijk simpel. Je maakt een cursor object aan via de `Watch()` methode (`WatchAsync()` voor async applicaties). De informatie voor dit codevoorbeeld is verkregen via de [MongoDB docs](https://www.mongodb.com/docs/drivers/csharp/current/logging-and-monitoring/change-streams/)

[Volledige code change stream](https://github.com/LanderDebeir/ChangestreamTryout)

```csharp
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("TestDatabase");
var collection = database.GetCollection<BsonDocument>("TestCollection");

var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
    .Match("{operationType: { $in: ['insert', 'delete', 'update'] }}");
var options = new ChangeStreamOptions
{
    FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
};

using var cursor = await collection.WatchAsync(pipeline, options);
Console.WriteLine("Watching for changes...");

_ = Task.Run(async () =>
{
    await Task.Delay(1000);
    await collection.InsertOneAsync(new BsonDocument("Name", "Jack"));
});

foreach (var change in cursor.ToEnumerable())
{
    Console.WriteLine($"Change detected: {change.OperationType}");
    switch (change.OperationType)
    {
        case ChangeStreamOperationType.Insert:
            Console.WriteLine("Action: INSERT");
            Console.WriteLine($"Data: {change.FullDocument}");
            break;
        case ChangeStreamOperationType.Update:
            Console.WriteLine("Action: UPDATE");
            Console.WriteLine($"Changes: {change.UpdateDescription.UpdatedFields}");
            break;
        case ChangeStreamOperationType.Delete:
            Console.WriteLine("Action: DELETE");
            Console.WriteLine($"Deleted ID: {change.DocumentKey}");
            break;
    }
}

```

De change stream geeft volgende events weer bij het gebruik van BackingDocuments indien er een aanpassing is

```js
{ "_id" : { "_data" : "..." }, "operationType" : "update", "clusterTime" : Timestamp(...),
"wallTime" : ISODate("..."), "ns" : { "db" : "sample_restaurants", "coll" : "restaurants" },
"documentKey" : { "_id" : ObjectId("...") }, "updateDescription" : { "updatedFields" : { "cuisine" : "Irish" },
"removedFields" : [], "truncatedArrays" : [] } }
```

De change stream stelt het ook mogelijk om het volledige aangepaste veld te zien bij een aanpassing met FullDocument

```js
{ "_id" : { "$oid" : "69285ec4b7355988a3e5083a" }, "address" : { "building" : "1007", "coord" : [-73.856076999999999, 40.848447], "street" : "Morris Park Ave", "zipcode" : "55555" }, "borough" : "Bronx", "cuisine" : "Bakery", "grades" : [{ "date" : { "$date" : "2014-03-03T00:00:00Z" }, "grade" : "A", "score" : 2 }, { "date" : { "$date" : "2013-09-11T00:00:00Z" }, "grade" : "A", "score" : 6 }, { "date" : { "$date" : "2013-01-24T00:00:00Z" }, "grade" : "A", "score" : 10 }, { "date" : { "$date" : "2011-11-23T00:00:00Z" }, "grade" : "A", "score" : 9 }, { "date" : { "$date" : "2011-03-10T00:00:00Z" }, "grade" : "B", "score" : 14 }], "name" : "Morris Park Bake Shop", "restaurant_id" : "30075445" }
```

#### Nu rest de vraag natuurlijk werkt dit ook in een container?

Dit werkt ook in een container. In deze POC is er gekozen voor Docker, de aanpassingen om dit te laten werken voor een container zijn:

Het aangeven aan de container dat het zal gebruikt worden als een replica-set [Replica set documentation](https://www.mongodb.com/docs/manual/reference/method/rs.initiate/) voor de rest zal de code er hetzelfde uitzien als in bovenstaande voorbeeld.

[Volledige code change stream in container](https://github.com/Or3nges/POC-mongoDB-change-streams-in-docker)

```js
function initiateReplicaSet() {
  try {
    const status = rs.status();
    if (status.ok === 1) {
      print("Replica set already initialized.");
      return;
    }
  } catch (e) {
    print("Replica set not initialized, proceeding...");
  }

  rs.initiate({
    _id: "rs0",
    members: [{ _id: 0, host: "mongo-db:27017" }]
  });

  print("Replica set initiated.");
}

let retries = 10;
while (retries > 0) {
  try {
    initiateReplicaSet();
    break;
  } catch (err) {
    print("MongoDB not ready yet, retrying in 2s...");
    sleep(2000);
    retries--;
  }
}

if (retries === 0) {
  print("Failed to initiate replica set after multiple attempts.");
}
```

## Publicatie & Open Source Strategy
- Repo setup
- Licentie: We hebben gekozen voor een MIT-License voor maximale vrijheid, eenvoudigheid en omdat dit de standaard is voor .NET projecten zoals dit. MIT is permissief en staat non-commercieel en commercieel (en zelf closed source) gebruik toe zonder enige complexe patentclausules.
- CI/CD basics (test coverage, pipeline, main niet pushen (repo rules), ...)
- Release strategy (package registry, docker, scripts) (docker zal het waarschijnlijk zijn, aangezien we containers moeten gebruiken vanuit de projectbeschrijving), toch de andere opties eens in overweging nemen
- Release checklist (deliverable)

## Plan & Milestones

| # | Milestone                 | Duratie  | Description                                                                                     |
| - | ------------------------- | ----- | ----------------------------------------------------------------------------------------------- |
| 1 | Onderzoek                  | 24/11 - 30/11 | Probleemanalyse, technologievergelijking, requirements definiëren en architectuur vastleggen.  |
| 2 | Op start Core implementatie | 01/12 - 07/12 | Opzetten projectstructuur, CI/CD pipelines en start implementatie.         |
| 3 | Core Implentatie       | 08/12 - 21/12 | Verder bouwen van de Sync Service, Outbox implementatie en de koppeling tussen MongoDB en MySQL alsook de demo applicatie voor de flow (MVP). |
| 4 | Code Finalisatie         | 22/12 - 09/01 | Verder uitwerken, extra features, bugfixing, refactoring van CQRS implementatie & demo applicatie. |
| 5 | Thesis Finalisatie       | 10/01 - 18/01 | Afronden van de scriptie, documentatie en formuleren van conclusies.               |

Denken aan de verschillende milestones (research, design, PoC, MVP, release) issues, usecases / epics

## Alternatieven overwogen (bij elke optie bijschrijven wat we overwogen hebben en waarom niet linkjes en referenties nodig)



## Bronnen

CQRS:
- https://cqrs.wordpress.com/about/ (als je binnen deze site wilt navigeren zal u het domein moeten veranderen naar cqrs.wordpress.com)
- https://awesome-architecture.com/cqrs/
- https://medium.com/@90mandalchandan/cqrs-architecture-how-it-works-5f18a36886ea 
- https://www.linkedin.com/pulse/how-do-you-build-fast-lightweight-solution-cqrs-event-daniel-miller-ymjqc/
- https://github.com/leandrocp/awesome-cqrs-event-sourcing 
- https://www.arnaudlanglade.com/difference-between-cqs-and-cqrs-patterns/ 
- https://www.cncf.io/blog/2020/08/13/49940/
- https://eventuate.io/docs/manual/eventuate-tram/latest/distributed-data-management.html

CQRS synhronisatie: 
- https://ricofritzsche.me/cqrs-event-sourcing-projections/ 
- https://en.wikipedia.org/wiki/Change_data_capture
- https://www.mongodb.com/docs/manual/changestreams/
- https://microservices.io/patterns/data/transactional-outbox.html
- https://www.bennyjohns.com/posts/20201115-commands-and-queries-with-a-message-broker
- https://medium.com/@nemagan/handling-data-consistency-in-kafka-techniques-for-exactly-once-processing-40f41b1a0364
- https://microservices.io/patterns/data/transactional-outbox.html

CQRS implementaties:
- Kim van Renterghem (C#): https://github.com/kimVanRenterghemNew/EventSourcingAndCQRS 
- Gregor Young(C#): https://github.com/gregoryyoung/m-r
- Carl Hoberg(Ruby): https://github.com/cavalle/banksimplistic
- Mark Nijhof (C#) https://github.com/MarkNijhof/Fohjin
- (Java) https://github.com/ddd-by-examples/event-source-cqrs-sample/tree/master

Event Sourcing:
- https://awesome-architecture.com/event-sourcing/
- https://www.kurrent.io/webinar-recording-introduction-to-event-sourcing?utm_campaign=Webinar%20-%20Introduction%20to%20Event%20Sourcing&utm_medium=email&_hsmi=348022371&utm_content=348022371&utm_source=hs_automation
- https://microservices.io/patterns/data/event-sourcing.html 
- https://www.baeldung.com/cqrs-event-sourcing-java
- https://www.kurrent.io/event-sourcing
- https://www.kurrent.io/blog/why-event-sourcing/
- https://dev.to/lukasniessen/event-sourcing-cqrs-and-micro-services-real-fintech-example-from-my-consulting-career-1j9b
- https://craigjcox.com/guides/event-sourcing

CDC (Change Data Capture)
- https://en.wikipedia.org/wiki/Change_data_capture
- https://medium.com/@marekchodak/change-data-capture-with-mongodb-change-streams-539a02cf401d
- https://debezium.io/blog/2020/02/10/event-sourcing-vs-cdc/
- https://amsayed.medium.com/the-various-methods-of-change-data-capture-cdc-with-examples-and-code-snippets-e5d6ea14dc88
- https://www.reddit.com/r/dataengineering/comments/moopot/can_someone_help_me_understand_the_difference/
- https://solace.com/blog/cdc-solace-cqrs-enabled-application/
- https://satyadeepmaheshwari.medium.com/understanding-cqrs-and-cdc-a-practical-guide-with-real-world-analogies-f0fce76fc2e6

Debezium:
- https://debezium.io/documentation/faq/  
- https://debezium.io/documentation/reference/stable/architecture.html 
- https://debezium.io/documentation/reference/stable/connectors/mongodb-sink.html 
- https://debezium.io/documentation/reference/stable/connectors/jdbc.html 

Axon Framework:
- https://docs.axoniq.io/axon-framework-reference/4.11/ 
- https://www.baeldung.com/axon-cqrs-event-sourcing  
- https://medium.com/fively/axon-framework-explaining-the-power-of-event-driven-architecture-208b30f5f737 
- https://medium.com/axoniq/demystifying-tracking-event-processors-in-axon-framework-1917c2f16e59 
- https://javadoc.io/doc/org.axonframework/axon-core/3.3.6/org/axonframework/eventhandling/TrackingEventProcessor.html 
