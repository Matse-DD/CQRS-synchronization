# Research flow
## Define your research objective
De bedoeling van de research is om de correcte architectuur te kiezen voor synchronisatie tussen 2 databanken (CQRS). En of er de mogelijkheid bestaat om later Event Sourcing te gebruiken. Verder is het ook belangrijk dat we een goede keuze maken voor de programmeertaal bepaalde talen zullen de implementatie moeilijker of makkelijker maken.

## Identify stakeholders and use cases
De stakeholders zullen vooral developers zijn alsook de product owner. De developers willen een schaalbaar, performant product afleveren hiervoor is CQRS een mogleijkheid. De product owner wilt dat de applicatie vlot draait en dat de impact van een downtime zo klein mogelijk is.

Developer: (duplicate info, data verlies, ...)
- Als een developer wil ik een performant product afleveren.
- Als een developer wil ik databanken kunnen synchroniseren zonder problemen.
- Als een developer wil ik databanken gemakkelijk terug synchronseren indien er een inconsistentie is.
- Als een developer wil ik dat indien er bepaalde commands onbedoeld dubbel worden uitgezonden deze niet dubbel worden uitgevoerd (idempotent).
- Als een developer wil ik dat indien de write databank onbereikbaar is ik nog steeds informatie kan opvragen.
- Als een developer wil ik dat indien er een onderdeel van de CQRS faalt er geen data verlies optreed.
- Als een developer wil ik dat het verkeer van queries & commands verdeeld is over de databanken.
- ...

Product owner:
- Als de product owner wil ik dat mijn product zo vlot mogelijk kan werken
- Als de product owner wil ik dat mijn product met zo weinig mogelijk down time kan werken
- Als de product owner wil ik dat mijn product niet volledig plat valt indien er een databank niet meer werkt.
- ...

## Master core concepts
UITLEG CQRS
### CQRS concept
### CQRS Synchronisatie mogelijkheden
#### Direct projection
#### Change stream
#### Change Data Capture (niet 100% zeker)
#### Outbox
#### Broker
### Event Sourcing

## Compare existing solutions
- debezium


## Define requirements
## Evaluate technology options
## Run small Proofs of Concept (PoCs)

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
