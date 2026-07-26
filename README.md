# PlcBridge

![Build Status](https://github.com/Mugen85/PlcBridge/actions/workflows/dotnet.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Tests](https://img.shields.io/badge/tests-xUnit-25A162)

PlcBridge è uno strumento di studio e simulazione sviluppato per colmare la distanza tra software gestionale (.NET) e hardware industriale (PLC).

Questo progetto nasce come banco di prova per testare la comunicazione TCP/IP e implementare modelli di interazione Request-Response in ambienti industriali, superando i limiti dei modelli a push continuo. È inoltre un banco di prova per pratiche di ingegneria del software moderne: Dependency Injection, Inversion of Control e Test-Driven Development.

## ✨ Caratteristiche

- **Simulatore Server/PLC Stateful:** gestione asincrona delle connessioni sulla porta 5000, con memoria interna che simula i registri di un vero PLC
- **Connessione persistente Client-Server:** una singola connessione TCP rimane attiva per l'intera sessione, permettendo l'invio di più comandi consecutivi senza riconnettersi
- **Logging strutturato con Serilog:** eventi tracciati su console e su file, con rotazione giornaliera e pulizia automatica dei log più vecchi (retention di 3 file)
- **Client di Monitoraggio:** interfaccia a TUI (Terminal User Interface) con Spectre.Console per interrogare e controllare lo stato della macchina in modo leggibile e strutturato, con menu ciclico per richieste multiple e uscita esplicita (`ESCI`)
- **Comandi di lettura:** `READ_PRESSURE`, `READ_TEMP`, `SYSTEM_STATUS`
- **Comandi di controllo attuatori:** `START_PUMP`, `STOP_PUMP` per l'interazione bidirezionale tipica di una vera HMI
- **Protocollo Polling:** implementazione di una logica Master-Slave per la richiesta e l'invio dati
- **Architettura disaccoppiata:** logica del PLC astratta dietro l'interfaccia `IPlcController` e iniettata via costruttore (Dependency Injection)
- **Clean Architecture:** struttura a layer (Core, Infrastructure, Worker, Tests) per un disaccoppiamento totale tra logica di dominio, implementazione hardware e strato di hosting
- **Test Unitari:** suite xUnit che verifica ogni comando del PLC senza dover aprire porte di rete o socket
- **CI/CD Ready:** pipeline GitHub Actions integrata per la validazione automatica del codice ad ogni modifica

## 🚀 Come iniziare

1. Clona la repository:
   ```
   git clone https://github.com/Mugen85/PlcBridge.git
   ```
2. Apri **due terminali** nella cartella del progetto (`PlcBridge.Worker`).
3. Nel primo terminale, avvia il Server:
   ```
   dotnet run server
   ```
4. Nel secondo terminale, avvia il Client:
   ```
   dotnet run client
   ```

> ⚠️ La suite di test (`PlcBridge.Tests`) è momentaneamente **commentata dalla solution**: va aggiornata per allinearla alla nuova Clean Architecture (in particolare ai contratti di `IPlcController` dopo il refactoring). Verrà riattivata a breve.

## ✅ Stato del Progetto

- [x] **Setup Ambiente:** Completato (struttura organizzata in GitHub Projects)
- [x] **Implementazione TCP Base:** Socket TCP/IP funzionanti
- [x] **Implementazione Polling:** Sistema Request/Response (Client/Server) configurato
- [x] **CI/CD Pipeline:** `dotnet.yml` su GitHub Actions — stato **Green** (build superata)
- [x] **Version Control:** Repository Git collegata correttamente al remoto GitHub
- [x] **UI Engineering:** Implementazione TUI (Terminal User Interface) con Spectre.Console
- [x] **Stateful Server & Controllo Attuatori:** memoria di stato interna e comandi di scrittura (`START_PUMP`, `STOP_PUMP`)
- [x] **Refactoring Dependency Injection:** introduzione di `IPlcController` e Inversion of Control
- [ ] **Quality Assurance:** suite di Test Unitari (xUnit) temporaneamente commentata dalla solution, in attesa di aggiornamento post-refactoring Clean Architecture
- [x] **Connessione persistente multi-comando:** client e server mantengono la connessione aperta per l'intera sessione, con loop di lettura/scrittura su entrambi i lati
- [x] **Logging con Serilog:** tracciamento eventi su console e file, con rotazione giornaliera e retention automatica dei log più vecchi
- [x] **Clean Architecture:** refactoring strutturale in layer (Core, Infrastructure, Worker) in preparazione all'integrazione con UI Web (Blazor)
- [x] **Fix Pipeline CI/CD:** risolti problemi di formattazione YAML e percorsi per GitHub Actions

## 🏗️ Architettura e Logica

### Clean Architecture

Per preparare l'applicazione all'integrazione futura con framework web come Blazor e a logiche di livello enterprise, il codice sorgente è stato diviso in layer con responsabilità rigorosamente separate:

- **PlcBridge.Core:** Il "cuore" del dominio. Contiene interfacce astratte (`IPlcService`) e modelli fortemente tipizzati (es. `PlcSystemStatus`). Non ha alcuna dipendenza verso l'esterno.
- **PlcBridge.Infrastructure:** Il "braccio operativo" che implementa i contratti del Core (es. comunicazione di rete, futuri driver Modbus/S7). Dipende dal Core ma non dal Web o dalla Console.
- **PlcBridge.Worker:** Il progetto host d'ingresso, responsabile della configurazione (Dependency Injection, Serilog) e del ciclo di vita dell'applicazione.

### Il ciclo di vita del processo

I programmi server occupano il socket finché non vengono terminati correttamente. La gestione delle risorse di sistema (es. `taskkill /F /IM PlcBridge.exe`) è fondamentale: se il processo resta "appeso", la porta 5000 rimane occupata e impedisce nuove esecuzioni.

### Protocollo Request-Response

Il progetto ha superato il modello a "push continuo" (il server invia dati a prescindere) in favore del modello a **Polling**:

1. Il **Client** richiede un dato specifico (es. `READ_PRESSURE`)
2. Il **Server** valuta la richiesta
3. Solo se valida, il Server risponde

Questo è il cuore di ogni comunicazione Master-Slave nell'automazione industriale.

### Connessione persistente e sessione multi-comando

La prima implementazione apriva e chiudeva una connessione TCP per ogni singolo comando: client e server si scambiavano un messaggio, poi lo `stream` veniva rilasciato (`using` a fine iterazione del loop). Questo andava bene per un comando isolato, ma non permetteva una vera sessione di monitoraggio continuo.

Il refactoring introduce un **loop di comunicazione su entrambi i lati**:

- Il **Client** stabilisce la connessione una sola volta, poi resta in un ciclo che ripropone il menu dopo ogni risposta, finché l'operatore non seleziona esplicitamente `ESCI`.
- Il **Server**, per ogni client accettato, entra in un ciclo interno che continua a leggere comandi sulla stessa connessione finché non riceve un segnale di chiusura pulita (`bytesRead == 0`) o un'eccezione di rete (`IOException`), gestita senza terminare il processo server.

Questo modello riflette meglio una vera sessione HMI-PLC, dove l'operatore invia più richieste consecutive senza dover ristabilire il collegamento ogni volta.

### Logging strutturato e rotazione dei log con Serilog

Sia il server che il client scrivono i propri eventi tramite **Serilog**, con due destinazioni (sink) configurate contemporaneamente:

- **Console:** output leggibile a runtime, con timestamp e livello di log in formato compatto (`[HH:mm:ss LVL] Messaggio`).
- **File:** un file di log per giorno (`logs/plcbridge-YYYYMMDD.txt`), grazie a `RollingInterval.Day`.

Per evitare che la cartella `logs/` cresca indefinitamente nel tempo, è impostato un **limite di retention**:

```csharp
.WriteTo.File(
    "logs/plcbridge-.txt",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 3 // Mantiene solo gli ultimi 3 file
)
```

- Ad ogni nuovo giorno, Serilog crea un nuovo file di log.
- Quando il numero di file supera il limite impostato (3), i file **più vecchi vengono eliminati automaticamente** ad ogni avvio dell'applicazione — non serve alcuna pulizia manuale.
- Questo bilancia due esigenze tipiche in ambito industriale: avere uno storico sufficiente per il debug retrospettivo (es. "cosa è successo ieri quando la pompa si è fermata"), senza che i log occupino spazio disco indefinitamente.

**Nota:** il limite si applica al numero di *file*, non ai singoli eventi di log al loro interno — con `RollingInterval.Day` corrisponde quindi a circa 3 giorni di storico. Se in futuro serve una retention diversa (es. per requisiti di audit più stringenti), basta modificare il valore di `retainedFileCountLimit` o passare a un `rollingInterval` differente (es. `Hour` per rotazioni più frequenti).

### Evoluzione visuale: verso una TUI con Spectre.Console

Per migliorare l'usabilità dello strumento senza passare subito alla complessità del Web (Blazor), è stato introdotto **Spectre.Console**:

- **Motivazione:** le interfacce a riga di comando (CLI) sono standard nell'automazione, ma le tabelle e i colori di Spectre permettono di creare una "dashboard" leggibile istantaneamente dall'operatore.
- **Lezione:** separare la logica di comunicazione (Socket) dalla logica di presentazione (TUI) rende il codice più manutenibile e professionale.

### Stato del server e comandi di scrittura (controllo attuatori)

Per rendere il simulatore un vero sistema industriale, sono stati introdotti due concetti cruciali:

1. **Memoria di Stato (Stateful Server):** il server mantiene variabili globali (`isPumpRunning`, `currentPressure`) che simulano i registri interni di un PLC. Non si limita più a rispondere con dati estemporanei, ma tiene traccia dello stato degli attuatori.
2. **Comandi di Scrittura / Controllo:** oltre alle letture passive (`READ_TEMP`, `READ_PRESSURE`), sono stati aggiunti comandi attivi come `START_PUMP`, `STOP_PUMP` e `SYSTEM_STATUS`. Questo simula l'interazione bidirezionale tipica di una vera HMI (accensione/spegnimento di macchinari).

### Refactoring architetturale: Dependency Injection & Inversion of Control

Per rendere il software pronto per il web (Blazor) e per protocolli di automazione professionali (OPC UA), è stato introdotto un refactoring architetturale:

- **`IPlcController` (interfaccia):** definisce il contratto del PLC (`Start`, `Stop`, `Read`), disaccoppiando la logica di business dall'implementazione concreta.
- **Inversion of Control:** il Server non crea più il PLC al proprio interno, ma lo riceve tramite il costruttore.
- **Vantaggio:** la logica di business è ora isolata e testabile in modo indipendente, senza dover aprire porte di rete o socket reali.

### Qualità: Unit Testing con xUnit

È stata aggiunta una suite di test che verifica ogni comando del PLC. Il codice non è più scritto "a vista", ma guidato dai test: se il test passa, la funzionalità è garantita. Grazie al disaccoppiamento introdotto con `IPlcController`, i test possono validare la logica di business isolatamente, senza dipendere dallo stack di rete.

> **Nota:** in seguito al refactoring verso la Clean Architecture, il progetto `PlcBridge.Tests` è temporaneamente **escluso dalla solution** (commentato), perché i test esistenti erano scritti contro la struttura precedente. Andranno riscritti contro il nuovo contratto `IPlcController` prima di essere riattivati.

### Componenti

| Componente | Descrizione |
|---|---|
| **Server** | Ascolta su `IPAddress.Any` (qualsiasi interfaccia di rete) sulla porta 5000, gestisce i comandi in entrata (lettura e scrittura) tramite un `IPlcController` iniettato, mantiene lo stato interno degli attuatori e gestisce più comandi per connessione in un ciclo persistente |
| **Client** | Si connette una sola volta e resta in sessione: invia comandi stringa in loop, attende la risposta bufferizzata con gestione sicura dei tipi (`?? string.Empty`) e visualizza i dati tramite `AnsiConsole` (tabelle formattate e `SelectionPrompt` per input a prova di errore), fino a uscita esplicita (`ESCI`) |
| **PlcBridge.Tests** | Progetto di test xUnit separato, che valida ogni comando del PLC tramite `IPlcController` senza dipendenze di rete |

## 🐛 Troubleshooting Log

Lezioni raccolte durante lo sviluppo, utili come riferimento futuro.

### 1. Errori di compilazione C# (CS0579 - Duplicate Attributes)
- **Problema:** Errore "L'attributo è duplicato" riferito ad `AssemblyInfo` e `TargetFramework` durante la divisione in progetti separati.
- **Causa:** Il Default Compile Globbing di MSBuild cattura tutti i file `.cs` nelle sottocartelle. Lasciando il file `.csproj` principale nella radice della repo, quest'ultimo includeva e compilava erroneamente anche i file temporanei (`obj/`) generati dagli altri progetti appena creati (Core e Infrastructure).
- **Risoluzione:** Spostato il progetto principale in una cartella dedicata (es. `PlcBridge.Worker`) in modo da isolarne il perimetro di compilazione e separarlo fisicamente dagli altri layer.

### 2. Problemi di annidamento progetti (nesting issues)
- **Problema:** il progetto di test era stato creato dentro la cartella del progetto principale, causando errori `CS0579` (attributi duplicati) e conflitti di Assembly.
- **Risoluzione:**
  - Creazione di una Solution (`.sln`) nella radice del repository.
  - Utilizzo di `dotnet sln add [progetto]` per gestire i due progetti come entità separate.
  - Esclusione esplicita della cartella di test dal progetto principale nel `.csproj`, per evitare la cross-compilazione.

### 3. Connessione interrotta al secondo comando (`IOException` / `SocketException 10053`)
- **Problema:** il client si disconnetteva con un'eccezione fatale non appena si tentava di inviare un secondo comando nella stessa sessione.
- **Causa:** il server apriva la connessione con `using TcpClient`/`using NetworkStream` **dentro** il loop `while (true)` di accettazione, gestendo un solo comando per connessione. Al termine dell'iterazione, gli `using` chiudevano automaticamente il socket, mentre il client presumeva la connessione ancora attiva.
- **Risoluzione:** aggiunto un ciclo interno lato server (`while (client.Connected)`) che continua a leggere/rispondere sulla stessa connessione finché il client non la chiude esplicitamente (`bytesRead == 0`) o si disconnette bruscamente (gestito con `catch (IOException)` senza terminare il processo server).

### 4. Crash a runtime con Spectre.Console (`System.InvalidOperationException`)
- **Problema:** L'app crasha tentando di stampare a video con messaggio: `Encountered malformed markup tag`.
- **Causa:** Un tag di formattazione non valido nella stringa passata ad `AnsiConsole.MarkupLine`. La libreria interpreta tutto ciò che è tra parentesi quadre `[]` come codice colore/stile (es. `[/ red]` invece di `[/red]`).
- **Risoluzione:** Rimosso lo spazio all'interno del tag. Se si stampano variabili dinamiche (o log JSON) che potrebbero contenere parentesi quadre testuali, utilizzare sempre `Markup.Escape()`.

## 💭 Riflessioni Tecniche

La configurazione di una pipeline CI/CD ha chiarito che il software non è solo "scrivere codice", ma creare un processo: avere un test automatico su GitHub garantisce che, anche a distanza, un'eventuale modifica che "rompe" qualcosa venga segnalata immediatamente.

La gestione del "remote already exists" in Git ha insegnato che i messaggi d'errore non vanno temuti, ma letti come indicazioni stradali: Git non vuole creare duplicati, vuole solo sapere quale sia la strada corretta da seguire.

L'adozione di Spectre.Console ha trasformato il Client da un semplice script di test a uno strumento di supervisione vero e proprio, dimostrando che l'usabilità conta quanto la funzionalità.

La gestione della struttura Solution (`.sln`) ha insegnato quanto sia critica la configurazione ambientale: un progetto ben organizzato non è solo più leggibile, è anche più facile da compilare e testare.

Il refactoring verso la Clean Architecture ha dimostrato che un software ben progettato non è solo quello che funziona oggi, ma quello le cui fondamenta sono pronte ad accogliere nuove tecnologie (come Blazor) senza dover riscrivere il cuore del dominio. Isolare i progetti nelle proprie cartelle ha inoltre svelato i meccanismi interni di MSBuild (Globbing) e i comportamenti della CLI .NET.

Il passaggio da connessioni "usa e getta" a una sessione persistente ha chiarito una distinzione fondamentale nella programmazione di rete: la differenza tra "un protocollo che risponde a un comando" e "un protocollo che sostiene una conversazione". Il secondo richiede di pensare esplicitamente al ciclo di vita della connessione su entrambi i lati, non solo alla singola transazione.

## 🛠️ Tech Stack

- C# / .NET 10
- Clean Architecture (Core, Infrastructure, Web/Worker)
- TCP/IP Sockets
- Spectre.Console (TUI)
- Dependency Injection / Inversion of Control
- xUnit (Unit Testing)
- GitHub Actions (CI/CD)

## 🖼️ Screenshot

**Server — sessione completa di comandi ricevuti su connessione persistente:**

![Server: log di sessione con comandi READ/START/STOP/SYSTEM_STATUS](docs/images/server-terminal.png)

**Client — TUI di supervisione (HMI Terminal) con tabelle comando/risposta:**

![Client: TUI Spectre.Console con tabelle Parametro/Stato e Valore](docs/images/client-terminal-monitor.png)

---

*Progetto sviluppato come parte del percorso di crescita professionale nel settore Industrial Software Engineering.*