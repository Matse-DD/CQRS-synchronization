# Research flow
## Define your research objective


- Wat probleem lossen we op?
  - Synchronisatie tussen 2 databanken zonder dat er data verlies optreed voor CQRS doeleinden.
- Wie zijn de gebruikers?
  - Developers dat gebruik willen maken van CQRS (Command Query Responsibility Segregation)
- Hoe weten we of het werkt?
  - Indien we een write doen naar de write databank en deze veranderingen zichtbaar worden in de read databank.
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
- Als een developer wil ik dat indien de write databank onbereikbaar is ik nog steeds informatie kan opvragen zodat het opvragen van gegevens geen impact ondervind.
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

### CQRS Synchronisatie
Zoals zonet vermeld kan je dus gaan voor aparte databanken voor CQRS. De moeilijkheid hieraan is hoe ga je ervoor zorgen dat de gegevens tussen de twee databanken hetzelfde is. Hiervoor zijn er verschillende opties zoals hieronder te zien is.

### Projector
Een projector zet het evenement of de verandering in data om naar een correct command, zodat de read databank correct kan worden geüpdatet

### Write databank
Op de write databank worden enkel de commands van de gebruiker uitgevoerd. Dit wilt zeggen dat indien de gebruiker informatie wilt aanpassen deze databank zal worden gebruikt.

### Read databank
De read databank word enkel gebruikt om queries uit te voeren dit wilt dus zeggen dat indien de gebruiker informatie wilt opvragen deze databank zal worden gebruikt.

## Compare existing solutions
### Debezium (https://debezium.io/)
Deze oplossing kijkt naar veranderingen in de write databank met behulp van polling eenmaal een verandering word opgemerkt en vertaalt naar events. Vervolgens worden deze events op een message broker (kafka) gepusht. Waar dan naar geluisterd kan worden door verschillende processen deze zullen dit event dan ontvangen. Onder deze processen zal dan een process zijn dat de ontvangen messages omzet naar de juiste commands en deze uitvoeren op de read databank.

### Axon Framework door Axoniq

- debezium (CDC)
- axoniq (event sourcing) (axon framework)
- Django CQRS Library (python CQRS)

## Define requirements
Must haves volgens project
- Implement a domain model with entities (DDD-inspired) for the write side.
- Detect changes in MongoDB.
- Update MySQL projections using one of the chosen approaches.
- Guarantee idempotent updates (no duplicates).
- Provide recovery/replay so the system can catch up after restarts.
- Deliver a demo app that shows synchronization flow.
- Supply a Docker Compose setup (MongoDB, MySQL, and optional broker).
- Document your setup, design choices, and usage.

Non functional volgens project
- Reliability: no data loss on restart; consistent updates.
- Performance: synchronization within a few seconds.
- Testability: ≥ 80% test coverage.
- Observability: logs and metrics that show system state.
- Reproducibility: one-command setup (Docker Compose).
- Documentation: README and demo examples

# Acceptance checkpoint (nog eens opzoeken wat hier bedoeld mee word)


## Evaluate technology options
- Welke progremeer mogelijkheden en gekozen waarom
  - typescript
  - .net
  - java
  - low level?
  - gekozen .NET ZIE RESEARCH IN GOOGLE DRIVE
- architectuur 
  - direct projection
  - change stream
  - change data capture
  - outbox
  - broker
  - uitleg gekozen architectuur + schemas van de diagrammen
- Evaluatie matrix van de bovenstaande onderdelen

### Programmeertaal
#### Low-level programmeertalen
#### Typescript
#### C#
#### Java

### CQRS Synchronisatie mogelijkheden
#### Direct projection 
(Is geen echte term in CQRS by the way maar ik ga ervanuit dat hiermee een CDC bedoeld word met een projectie in)

Deze architectuur zal gebruik maken van een CDC (Change Data Capture) deze zal kijken voor veranderingen in de data van de write databank. Eenmaal er een verandering word waargenomen zal deze verandering worden geprojecteerd door de projector zodat we de read databank kunnen aanpassen.

![Foto van direct projection architectuur](images_research/direct_projector.png)


#### Change stream

#### Outbox

#### Broker

### Event Sourcing


## Run small Proofs of Concept (PoCs)
- code snippets 
  - change data stream
  - uit testen van mogelijkheden
- resultaat + conclusie

## Publicatie & Open Source Strategy
- Repo setup
- Licentie + waarom (vergelijking apache & andere opties)
- CI/CD basics (test coverage, pipeline, main niet pushen (repo rules), ...)
- Release strategy (package registry, docker, scripts) (docker zal het waarschijnlijk zijn aangezien we containers moeten gebruiken vanuit de projectbeschrijving) toch de andere opties eens in overweging nemen
- Release checlist (deliverable)

## Plan & Milestones
Denken aan de verschillende milestones (research, design, PoC, MVP, release) issues, usecases / epics

## Alternatieven overwogen (bij elke optie bijschrijven wat we overwogen hebben en waarom niet linkjes en referenties nodig)






# AL WAT DATA 

# Research CQRS-synchronization
## Synchronization approach
### Direct projection
- Voordelen
  - Simpele architectuur
  - 
- Nadelen
  - Als de projection faalt dan is er geen manier om de verloren data 
  - Constant pollen -> onnodige load op het netwerk
  - Veranderingen in de data moet worden opgemerkt hoe ga je dit doen?


### Change streams
- Voordelen
  - Geen polling
  - Simpel
- Nadelen
  - De overstap naar Event sourcing zal zeer moeilijk zijn
  - Stel dat bepaalde veranderingen niet worden door gevoerd en er inconsistency optreed.
    - word het moeilijk om te beseffen dat er inconsistency is
    - moet je de volledige write databank beginnen vergelijken met de read databank wat arbeidsintensief is
  - MongoDB specifiek

### Outbox
- Voordelen
  - Je kan zie of er inconsistency is door te kijken welke events nog aanwezig zijn in de outbox table
  - Indien events niet doorgaan blijven deze duidelijk aangegeven in de outbox table en kunnen ze opnieuw worden uitgevoerd
  - Duplicate events zijn onmogelijk aangezien de event_id de primaire sleutel is en deze uniek moet zijn in een databank
  - Makkelijk om later over te stappen naar event sourcing
  - Indien er toch inconsistenties zouden optreden (in zeer specifieke edge cases) kan de data van de write databank worden vergeleken met de data van de read databank
- Nadelen
  - Events moeten synchroon worden uitgevoerd
  - Vraagt meer opslag om de events op te slaan je kan natuurlijk gelukte events verwijderen
  - Arbeidsintensief zowel de write databank aanpassen als een nieuw event opslaan

### Broker
- Voordelen
  - Schaalbaar
  - Gemakkelijk aan te passen voor Event Sourcing
- Nadelen
  - Arbeidsintensief zowel de write databank aanpassen als een nieuw event versturen naar de message broker
  - Synchroon de events moeten in de juiste volgorde worden doorgegeven

### Wat we gekozen hebben
outbox + change streams. We hebben voor een combinatie tussen het outbox patroon en change streams. Het idee is om bij een update naar de write databank zowel het update request te sturen naar de mongoDb als een event volgens de Event interface. Het update request zal de entiteit aanpassen dat zich in de mongodb bevind. Het event zal worden geinsert in de outbox. Dit is een ruimte waar de verschillende events in worden opgeslaan. Eenmaal een event klaar is zal dit worden verwijderd. Indien het event gefaalt is word het gemarkeerd als gefaalt zodat het later opnieuw kan worden geprobeerd. 

Nu komt de change stream er bij kijken deze change stream kijkt naar veranderingen op de outbox table. Indien er dus een nieuw event word toegevoegt aan de table zal de projector op de hoogte worden gebracht met de nodige informatie. Deze kan dan het event omzetten naar de juiste vorm en uitvoeren op de mySql databank.

De reden dat er gekozen is voor een combinatie tussen outbox en change stream is dat met het oude outbox patroon het moeilijk is om te weten of er iets veranderd is de outbox. Je zou dan ofwel moeten gebruik maken van polling of de projector op de hoogte brengen dat er iets veranderd is. We hebben er echter voor gekozen dat de applicatie dat schrijft naar de databanken niet op de hoogte moet zijn van de projector. Het is dus mooier om de projector te laten subscriben op de change stream waardoor het op de hoogte word gebracht van mogelijke veranderingen.

De update van het entiteit doen we nog steeds om gemakkelijke te kunnen checken of er geen consistentie problemen zijn. 

Wat indien de mysql kapot is? In dat geval zal de projector het desbetreffende event updaten in de outbox en de state updaten zodat deze de waarde failed krijgt dit zorgt ervoor dat deze later opnieuw kan worden uitgeprobeerd. Het gevaarlijke hieraan is hoe weet je nu of er nog events in de outbox aanwezig zijn die de vorige keer gefaalt zijn. De mogelijke opties zijn om als we een nieuwe change stream binnen krijgen te gaan kijken of er nog iets anders in de outbox staat met als status failed. Een andere mogelijkheid is een constante polling te doen zodat events in de outbox niet opbouwen. Dit zou dan via een andere microservice kunnen verlopen. 

In latere versies zal het echter zo zijn dat de entiteit dat de state bijhoud weg gaat voor de write databank. (kijken of dat dit de correcte benaming is) (de state is van welke data er effectief in de databank aanwezig is bijvoorbeeld bank account -> user_id, naam, balans) Deze latere versies zullen dan gebruik maken van event sourcing.

## Waarom geen broker
De broker is niet nodig in ons voorbeeld aangezien er maar 1 applicatie zit tussen de write & read databank. (dit zou misschien wel nodig zijn indien we een soort van recovery service hebben) dit zorgt voor de gekozen architectuur enkel voor onnodige complexiteit. 

Eigenlijk vervangt de change stream de functie van de message broker in onze architectuur. De projector is dan gesubscribed op de change stream en ontvangt de nieuwe events indien deze worden geinsert.

## Hoe garanderen we ordering, idempotency & recovery

### Ordering
Er is een veld aanwezig bij de events genaamd occured_at dit veld geeft weer wanneer dit event gebeurd is op basis van deze tijd kan de ordering worden bepaald van welk event eerst moet worden uitgevoerd.

### Idempotency
Met het gekozen patroon plaatsen we de verschillende events in de outbox table. De primaire sleutel van deze events zal de event_id zijn van het event. Aangezien databanken willen dat de primaire sleutel uniek is kan je er zeker van zijn dat er geen dubbele events worden opgeslaan en dus ook niet zullen worden uitgevoerd.

### Recovery
# Nog eens goed over nadenken

 op dit moment heb je de data van de write databank dat je nog steeds gewoon veranderd je kan deze dan gewoon vergelijken met de read databank. Je kan ook kijken naar de verschillende events dat aanwezig zijn in de outbox indien hier nog events aanwezig zijn zullen deze eerst moeten worden uitgevoerd. 
 
 (Waar ik op dit moment een beetje schrik voor heb is wat als de read databank word geupdate en dan net voordat de state word aangepast crasht de write databank bijvoorbeeld in dat geval is zal het net uitgevoerde event nog als inprogress worden beschouwt (misschien dat je op basis van de inprogress het kan bepalen???))

## Hoe is het domein model opgebouwt
### Domain


### Application


### Infrastructuur
Connecties naar de verschillende databanken en hoe deze connecties verlopen


## Zijn we aan het overwegen om met event sourcing te experimenteren
Ja 

## Record decisions as Architecture Decision Records (coach).
# OPZOEKEN WAT HIERMEE BEDOEL WORD 
