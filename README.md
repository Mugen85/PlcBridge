# PlcBridge

![Build Status](https://github.com/Mugen85/PlcBridge/actions/workflows/dotnet.yml/badge.svg) ![.NET](https://img.shields.io/badge/.NET-10.0-512BD4) ![Tests](https://img.shields.io/badge/tests-5%20passing-25A162)

PlcBridge è uno strumento di studio e simulazione sviluppato per colmare la distanza tra software gestionale (.NET) e hardware industriale (PLC).

Questo progetto nasce come banco di prova per testare la comunicazione TCP/IP e implementare modelli di interazione Request-Response in ambienti industriali, superando i limiti dei modelli a push continuo. È inoltre un banco di prova per pratiche di ingegneria del software moderne: Dependency Injection, Inversion of Control, Clean Architecture, .NET Generic Host e Test-Driven Development.

## ✨ Caratteristiche

- **Simulatore Server/PLC Stateful:** gestione asincrona delle connessioni sulla porta 5000, con memoria interna thread-safe che simula i registri di un vero PLC
- **Connessione persistente Client-Server:** una singola connessione TCP rimane attiva per l'intera sessione, permettendo l'invio di più comandi consecutivi senza riconnettersi
- **Logging strutturato con Serilog:** eventi tracciati su console e su file, con rotazione giornaliera e pulizia automatica dei log più vecchi (retention di 3 file), integrato nativamente nell'Host dell'applicazione
- **Client di Monitoraggio:** interfaccia a TUI (Terminal User Interface) con Spectre.Console per interrogare e controllare lo stato della macchina in modo leggibile e strutturato
- **Comandi di lettura:** `READ_PRESSURE`, `READ_TEMP`, `SYSTEM_STATUS`
- **Comandi di controllo attuatori:** `START_PUMP`, `STOP_PUMP` per l'interazione bidirezionale tipica di una vera HMI
- **Protocollo Polling e BackgroundService:** implementazione di una logica Master-Slave per la richiesta e l'invio dati, eseguita in un thread asincrono separato dalla UI
- **Servizio di dominio tipizzato:** logica del PLC astratta dietro `IPlcService`, con lo stato del sistema rappresentato dal record immutabile `PlcSystemStatus`, in sostituzione del precedente modello generico a tag
- **Clean Architecture:** struttura a layer (Core, Infrastructure, Worker, Tests) per un disaccoppiamento totale tra logica di dominio, implementazione hardware e strato di hosting
- **.NET Generic Host:** gestione enterprise del ciclo di vita dell'applicazione, Dependency Injection nativa e configurazione centralizzata
- **Thread-Safety & Graceful Shutdown:** utilizzo del costrutto `lock` per proteggere i dati concorrenti tra Worker e UI, e utilizzo dei `CancellationToken` per terminare le connessioni in modo pulito
- **Test Unitari:** suite xUnit riattivata che verifica ogni comando del PLC tramite `IPlcService`, senza dover aprire porte di rete o socket
- **CI/CD Ready:** pipeline GitHub Actions integrata per la validazione automatica del codice ad ogni modifica

## 🚀 Come iniziare

> **Nota sull'evoluzione:** nelle versioni precedenti il progetto richiedeva l'avvio separato di un comando server e un comando client su due terminali distinti. Con il passaggio alla Clean Architecture e al .NET Generic Host, l'applicazione è ora un unico eseguibile coeso.

1. Clona la repository:
   ```
   git clone https://github.com/Mugen85/PlcBridge.git
   ```
2. Spostati nella cartella del Worker:
   ```
   cd PlcBridge.Worker
   ```
3. Avvia il sistema:
   ```
   dotnet run
   ```

L'applicazione avvierà automaticamente il motore di polling in background e mostrerà i log a schermo. Per terminare il processo in modo pulito (Graceful Shutdown), premere **ESC**.

Per eseguire la suite di test unitari:
```
cd PlcBridge.Tests
dotnet test
```

## ✅ Stato del Progetto

- [x] **Setup Ambiente:** Completato (struttura organizzata in GitHub Projects)
- [x] **Implementazione TCP Base:** Socket TCP/IP funzionanti
- [x] **Implementazione Polling:** Sistema Request/Response (Client/Server) configurato
- [x] **CI/CD Pipeline:** `dotnet.yml` su GitHub Actions — stato **Green** (build superata)
- [x] **Version Control:** Repository Git collegata correttamente al remoto GitHub
- [x] **UI Engineering:** Implementazione TUI (Terminal User Interface) con Spectre.Console
- [x] **Stateful Server & Controllo Attuatori:** memoria di stato interna e comandi di scrittura (`START_PUMP`, `STOP_PUMP`)
- [x] **Refactoring Dependency Injection:** introduzione di `IPlcDriver` (poi evoluto in `IPlcService`) e Inversion of Control
- [x] **Connessione persistente multi-comando:** client e server mantengono la connessione aperta per l'intera sessione, con loop di lettura/scrittura su entrambi i lati
- [x] **Logging con Serilog:** tracciamento eventi su console e file, con rotazione giornaliera e retention automatica dei log più vecchi
- [x] **Clean Architecture:** refactoring strutturale in layer (Core, Infrastructure, Worker) in preparazione all'integrazione con UI Web (Blazor)
- [x] **Fix Pipeline CI/CD:** risolti problemi di formattazione YAML e percorsi per GitHub Actions
- [x] **Refactoring .NET Generic Host:** migrazione da script procedurale ad architettura a Host con `BackgroundService` per il polling asincrono
- [x] **Thread-Safety:** gestione concorrenza dati tramite `lock` e spegnimento controllato tramite `CancellationToken`
- [x] **Raffinamento del contratto di dominio:** sostituzione del modello generico a tag (`IPlcDriver`, `PlcTag`) con un servizio di dominio dedicato (`IPlcService`, `PlcSystemStatus`)
- [x] **Quality Assurance:** suite di Test Unitari (xUnit) riallineata al contratto `IPlcService` e riattivata nella solution

## 🏗️ Architettura e Logica

### Clean Architecture

Per preparare l'applicazione all'integrazione futura con framework web come Blazor e a logiche di livello enterprise, il codice sorgente è diviso in layer con responsabilità rigorosamente separate:

- **PlcBridge.Core:** Il "cuore" del dominio. Contiene esclusivamente contratti astratti (`IPlcService`) e modelli immutabili fortemente tipizzati (es. il record `PlcSystemStatus`). Non ha alcuna dipendenza verso l'esterno (database, rete o console).
- **PlcBridge.Infrastructure:** Il "braccio operativo" che implementa i contratti del Core. Il `SimulatedPlcService` gestisce, in modo thread-safe, lo stato di pompe, temperature e pressioni simulate. Dipende dal Core ma non dal Web o dalla Console.
- **PlcBridge.Worker:** Il progetto host d'ingresso. Non contiene logica di business, ma assembla i pezzi configurando la Dependency Injection, Serilog, il motore di polling (`PlcPollingWorker`) e la UI.

| Componente | Descrizione |
|---|---|
| **PlcBridge.Core** | Contratti (`IPlcService`) e modelli di dominio immutabili (`PlcSystemStatus`). Nessuna dipendenza esterna. |
| **PlcBridge.Infrastructure** | Implementazione concreta e thread-safe dei contratti del Core (`SimulatedPlcService`; in futuro driver Modbus/S7). |
| **PlcBridge.Worker** | Host `.NET Generic Host`: configura DI, Serilog e il `BackgroundService` di polling (`PlcPollingWorker`); espone la TUI con Spectre.Console. |
| **PlcBridge.Tests** | Suite xUnit che valida `IPlcService` in isolamento, senza dipendenze di rete o socket. |

### Dal paradigma Server/Client al Generic Host

L'applicazione è evoluta da un semplice script con argomenti da riga di comando (`if (args[0] == "server")`) a un'applicazione industriale basata su .NET Generic Host.

Il cuore del sistema è un `BackgroundService` (`PlcPollingWorker`) che gira su un thread separato, garantendo che le operazioni di rete asincrone (le interrogazioni al PLC) non blocchino mai il thread principale dedicato all'interfaccia utente.

### Dal modello a tag al servizio di dominio (`IPlcService`)

La prima versione del Core esponeva un contratto generico basato su tag (`IPlcDriver`, `PlcTag`), pensato per essere agnostico rispetto al protocollo industriale sottostante. Questo approccio è stato sostituito da un'interfaccia di dominio più espressiva e sicura: `IPlcService`, che opera direttamente su un record immutabile (`PlcSystemStatus`) rappresentante lo stato completo della macchina (temperatura, pressione, stato pompa). Il risultato è un contratto fortemente tipizzato, più leggibile e meno soggetto a errori rispetto all'accesso per chiave stringa tipico del modello a tag.

### Thread-Safety e Concorrenza (il costrutto `lock`)

Separando la UI dal Worker di rete, si crea un problema di concorrenza: la UI legge costantemente i valori per aggiornare lo schermo, mentre il Worker li sovrascrive quando riceve nuovi dati.

Per evitare Race Condition o letture di memoria corrotte, il `SimulatedPlcService` implementa un semaforo interno (`lock` dedicato) che protegge l'accesso ai registri simulati. Questo assicura che gli aggiornamenti siano operazioni atomiche, anche in previsione di futuri client TCP concorrenti oltre al worker di background.

### Il ciclo di vita del processo e i `CancellationToken`

Tutte le operazioni asincrone e i loop infiniti sono governati da un `CancellationToken`. Se viene richiesta la chiusura dell'app (es. pressione del tasto ESC), il token propaga il segnale a tutti i layer. Questo previene il fenomeno dei "task zombie", chiudendo in modo pulito le socket di rete e liberando le risorse di sistema. Nelle versioni iniziali, terminare male il processo (es. con `taskkill`) manteneva la porta 5000 occupata.

### Protocollo Request-Response e Polling

Il progetto ha superato il modello a "push continuo" (il server invia dati a prescindere) in favore del modello a **Polling**:

1. Il **Master** (Worker) richiede un dato specifico (es. `READ_PRESSURE`)
2. Lo **Slave** (PLC/Simulatore) valuta la richiesta
3. Solo se valida, lo Slave risponde

Questo è il cuore di ogni comunicazione Master-Slave nell'automazione industriale.

### Connessione persistente e sessione multi-comando

La primissima implementazione apriva e chiudeva una connessione TCP per ogni singolo comando (utilizzando `using` a fine iterazione del loop). Il refactoring ha introdotto un loop di comunicazione interno (`while (client.Connected)`) che permette di mantenere la socket aperta, riflettendo meglio una vera sessione HMI-PLC dove l'operatore o il worker inviano richieste consecutive a ciclo continuo.

### Logging strutturato e rotazione dei log con Serilog

Sia l'host che i worker scrivono i propri eventi tramite **Serilog**, configurato centralmente nel layer Worker, con due destinazioni (sink) attive contemporaneamente:

- **Console:** output leggibile a runtime, con timestamp e livello di log in formato compatto (`[HH:mm:ss LVL] Messaggio`)
- **File:** un file di log per giorno (`logs/plcbridge-YYYYMMDD.txt`), grazie a `RollingInterval.Day`
- **Retention Limit:** mantiene solo gli ultimi 3 file di log per evitare il riempimento dei dischi, cancellando automaticamente i più vecchi

### Qualità: Unit Testing con xUnit

Il progetto `PlcBridge.Tests` è stato riallineato al contratto `IPlcService` post-refactoring ed è riattivato nella solution. La suite verifica il comportamento del `SimulatedPlcService` in isolamento, senza aprire porte di rete o socket:

- **`ConnectAsync_ShouldSetStateToConnected`:** verifica che dopo la connessione lo stato passi a `ConnectionState.Connected`
- **`DisconnectAsync_ShouldSetStateToDisconnected`:** verifica che la disconnessione riporti lo stato a `ConnectionState.Disconnected`
- **`ReadTagAsync_WhenNotConnected_ShouldThrowException`:** verifica che una lettura tentata senza connessione attiva sollevi una `InvalidOperationException`
- **`ReadTagAsync_Pressure_ShouldReturnDoubleValue`:** verifica che la pressione restituisca un valore `double` compreso nel range simulato (10.0–15.0 Bar)
- **`WriteAndRead_PumpStatus_ShouldUpdateValue`:** verifica che una scrittura sullo stato della pompa sia effettivamente persistita e rileggibile

Grazie al disaccoppiamento introdotto da `IPlcService`, ogni test istanzia direttamente il `SimulatedPlcService` tramite l'interfaccia, rendendo la suite veloce e indipendente dallo stack di rete o dall'host.

## 🐛 Troubleshooting Log

Lezioni raccolte durante lo sviluppo, utili come riferimento futuro.

### 1. Errori di compilazione C# (CS0579 - Duplicate Attributes)
- **Problema:** Errore "L'attributo è duplicato" riferito ad `AssemblyInfo` e `TargetFramework` durante la divisione in progetti separati.
- **Causa:** Il Default Compile Globbing di MSBuild cattura tutti i file `.cs` nelle sottocartelle. Lasciando il file `.csproj` principale nella radice della repo, quest'ultimo includeva e compilava erroneamente anche i file temporanei (`obj/`) generati dagli altri progetti appena creati (Core e Infrastructure).
- **Risoluzione:** Spostato il progetto principale in una cartella dedicata (es. `PlcBridge.Worker`) in modo da isolarne il perimetro di compilazione e separarlo fisicamente dagli altri layer.

### 2. Problemi di annidamento progetti (nesting issues)
- **Problema:** il progetto di test era stato creato dentro la cartella del progetto principale, causando errori `CS0579` (attributi duplicati) e conflitti di Assembly.
- **Risoluzione:** Creazione di una Solution (`.sln`) nella radice del repository, utilizzo di `dotnet sln add [progetto]` ed esclusione della cartella di test dal progetto principale.

### 3. Connessione interrotta al secondo comando (`IOException` / `SocketException 10053`)
- **Problema:** il client si disconnetteva con un'eccezione fatale non appena si tentava di inviare un secondo comando nella stessa sessione.
- **Causa:** gli `using` chiudevano automaticamente il socket al termine della prima iterazione del loop.
- **Risoluzione:** aggiunto un ciclo interno lato server (`while (client.Connected)`) che continua a leggere/rispondere sulla stessa connessione.

### 4. Crash a runtime con Spectre.Console (`System.InvalidOperationException`)
- **Problema:** l'app crasha tentando di stampare a video con messaggio: `Encountered malformed markup tag`.
- **Causa:** un tag di formattazione non valido nella stringa passata ad `AnsiConsole.MarkupLine` (spazi all'interno delle parentesi quadre).
- **Risoluzione:** rimosso lo spazio all'interno del tag o utilizzato `Markup.Escape()`.

### 5. Errore MSB3026/MSB3027 - Il file è bloccato da un altro processo
- **Problema:** tentando di lanciare `dotnet run client` mentre un altro terminale eseguiva `dotnet run server`, la compilazione falliva con l'errore `The process cannot access the file...`.
- **Causa:** in Windows, un programma in esecuzione blocca il file `.exe`. Il comando `dotnet run` esegue una build di default prima dell'avvio e non riusciva a sovrascrivere l'eseguibile bloccato dal server in esecuzione.
- **Risoluzione:** passaggio all'architettura coesa a singolo eseguibile (Generic Host) che elimina la necessità di due processi paralleli. *(Alternativa appresa per casi simili: usare `dotnet run --no-build`)*.

### 6. Spazio dei nomi o Interfaccia non trovata (`CS0234`, `CS0246`)
- **Problema:** il progetto Infrastructure non trovava l'interfaccia del servizio del Core, nonostante il `ProjectReference` fosse configurato correttamente in MSBuild.
- **Causa:** il file dell'interfaccia nel progetto Core era stato creato accidentalmente senza l'estensione `.cs`. Il compilatore ignora i file senza estensione, rendendo il progetto Core compilabile ma di fatto privo dell'interfaccia.
- **Risoluzione:** aggiunta l'estensione `.cs` al file sorgente.

## 💭 Riflessioni Tecniche

Il passaggio da script procedurali a un'architettura .NET Generic Host ha evidenziato la differenza tra scrivere codice che "funziona sul momento" e codice "Industrial-Grade".

La gestione della concorrenza ha dimostrato l'importanza di prevedere gli scenari peggiori: in fabbrica un software non può bloccarsi perché UI e I/O tentano di accedere alla stessa variabile. Il costrutto `lock` e l'uso del `CancellationToken` garantiscono l'affidabilità continua.

L'errore sul file bloccato (MSB3026) è stata un'ottima lezione sul funzionamento del sistema operativo Windows e sui meccanismi di build impliciti della CLI di .NET (`dotnet run` vs `dotnet build`).

Il refactoring del contratto da `IPlcDriver`/`PlcTag` a `IPlcService`/`PlcSystemStatus` ha rafforzato il principio cardine della Clean Architecture: le interfacce permettono di sostituire la tecnologia sottostante senza che la logica del programma se ne accorga. Un domani, il passaggio dal `SimulatedPlcService` a un driver `S7Net` per un PLC fisico richiederà solo la modifica di una riga nella Dependency Injection.

## 🛠️ Tech Stack

- C# / .NET 10
- Clean Architecture (Core, Infrastructure, Worker)
- .NET Generic Host & `BackgroundService`
- TCP/IP Sockets
- Spectre.Console (TUI)
- Dependency Injection / Inversion of Control
- xUnit (Unit Testing)
- GitHub Actions (CI/CD)

## 🖼️ Screenshot

*(Screenshot delle versioni precedenti)*

**Server — sessione su connessione persistente:**

![Server terminal](docs/images/server-terminal.png)

**Client — TUI di supervisione:**

![Client terminal](docs/images/client-terminal-monitor.png)

**ULTIMA VERSIONE Worker — avvio unico con .NET Generic Host, polling in background e Graceful Shutdown:**

![Worker: bootstrap, connessione al PLC, polling della pressione e arresto pulito con ESC](docs/images/worker-polling-session.png)

---

*Progetto sviluppato come parte del percorso di crescita professionale nel settore Industrial Software Engineering.*