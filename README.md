# PlcBridge

![Build Status](https://github.com/Mugen85/PlcBridge/actions/workflows/dotnet.yml/badge.svg) ![.NET](https://img.shields.io/badge/.NET-10.0-512BD4) ![Tests](https://img.shields.io/badge/tests-11%20passing-25A162)

PlcBridge è uno strumento di studio e simulazione sviluppato per colmare la distanza tra software gestionale (.NET) e hardware industriale (PLC).

Nasce come banco di prova per la comunicazione TCP/IP e i modelli Request-Response in ambienti industriali, superando i limiti dei modelli a push continuo. È anche un banco di prova per pratiche di ingegneria del software moderne: Dependency Injection, Clean Architecture, .NET Generic Host e Test-Driven Development.

## ✨ Caratteristiche

- **Simulatore PLC Stateful e thread-safe:** memoria interna che simula temperatura, pressione e stato pompa di un vero PLC (`IPlcService` / `PlcSystemStatus`)
- **TCP Server per client esterni:** un `TcpListener` dedicato (porta 5050) accetta connessioni concorrenti da client esterni (script, tool di test, future HMI), esponendo gli stessi comandi del sistema tramite un protocollo testuale request-response, in parallelo al polling interno
- **Polling interno automatico:** `BackgroundService` (`PlcPollingWorker`) che interroga il PLC su un thread separato dalla UI
- **Connessione persistente multi-comando:** sia lato polling interno che lato client TCP, la connessione resta aperta per l'intera sessione, permettendo più comandi consecutivi senza riconnettersi
- **Comandi di lettura:** `READ_PRESSURE` / `GET_STATUS`, `READ_TEMP`
- **Comandi di controllo attuatori:** `START_PUMP`, `STOP_PUMP`
- **Client di Monitoraggio (TUI):** interfaccia con Spectre.Console per visualizzare lo stato macchina
- **Logging strutturato con Serilog:** console + file con rotazione giornaliera (retention 3 file)
- **Clean Architecture:** layer Core / Infrastructure / Worker / Tests totalmente disaccoppiati
- **.NET Generic Host:** DI nativa, configurazione centralizzata, gestione del ciclo di vita
- **Thread-Safety & Graceful Shutdown:** `lock` sui dati condivisi, `CancellationToken` per uno shutdown pulito (tasto **ESC**)
- **Test Unitari (xUnit):** validano `IPlcService` senza aprire porte di rete o socket
- **Integration Test TCP (xUnit):** verifica il flusso completo `TcpClient` → `TcpPlcServer` → `IPlcCommandProcessor` → `SimulatedPlcService` su loopback, usando una porta isolata (`50505`)
- **CI/CD Ready:** pipeline GitHub Actions

## 🚀 Come iniziare

> **Nota sull'evoluzione:** nelle versioni precedenti il progetto richiedeva l'avvio separato di un comando server e un comando client su due terminali distinti. Con Clean Architecture e .NET Generic Host, l'applicazione è oggi un unico eseguibile coeso, che espone anche un endpoint TCP per client esterni.

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
   dotnet run --project PlcBridge.Worker/PlcBridge.csproj
   ```

L'applicazione avvia in automatico il polling interno e il TCP Server sulla porta 5050, mostrando i log a schermo. Per terminare in modo pulito (Graceful Shutdown), premere **ESC**.

Per testare il TCP Server da un client esterno (es. PowerShell):
```powershell
$client = New-Object System.Net.Sockets.TcpClient("127.0.0.1", 5050)
$stream = $client.GetStream()
$writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::UTF8)
$reader = New-Object System.IO.StreamReader($stream, [System.Text.Encoding]::UTF8)
$writer.AutoFlush = $true

$writer.WriteLine("GET_STATUS")
$reader.ReadLine()
```

Per eseguire la suite di test unitari:
```
cd PlcBridge.Tests
dotnet test
```

## ✅ Stato del Progetto

- [x] **Setup, TCP base, Version Control, CI/CD:** completati
- [x] **Stateful Server & Controllo Attuatori:** memoria di stato interna, `START_PUMP` / `STOP_PUMP`
- [x] **Refactoring DI e Clean Architecture:** layer Core / Infrastructure / Worker / Tests
- [x] **Connessione persistente multi-comando** su tutte le sessioni
- [x] **Logging con Serilog:** rotazione giornaliera e retention
- [x] **.NET Generic Host:** `BackgroundService` per il polling asincrono
- [x] **Thread-Safety & Graceful Shutdown:** `lock` + `CancellationToken`
- [x] **Raffinamento del contratto di dominio:** da modello a tag (`IPlcDriver`/`PlcTag`) a servizio tipizzato (`IPlcService`/`PlcSystemStatus`)
- [x] **TCP Server per client esterni:** `TcpListener` dedicato (porta 5050), testato con connessione multi-comando da client PowerShell
- [x] **Quality Assurance:** suite xUnit riallineata a `IPlcService` + integration test TCP

## 🏗️ Architettura e Logica

### Clean Architecture

| Componente | Descrizione |
|---|---|
| **PlcBridge.Core** | Contratti (`IPlcService`) e modelli immutabili (`PlcSystemStatus`). Nessuna dipendenza esterna. |
| **PlcBridge.Infrastructure** | `SimulatedPlcService`: implementazione thread-safe dei contratti del Core (in futuro driver Modbus/S7). |
| **PlcBridge.Worker** | Host `.NET Generic Host`: DI, Serilog, `PlcPollingWorker` (polling interno) e `TcpServerService` (listener esterno); espone la TUI con Spectre.Console. |
| **PlcBridge.Tests** | Suite xUnit che valida `IPlcService` in isolamento. |

Il codice è diviso in layer con responsabilità rigorosamente separate, in preparazione a un'eventuale integrazione futura con una UI web (Blazor) o driver hardware reali.

### Due canali di accesso: polling interno e TCP Server esterno

Il Worker ospita ora **due `BackgroundService` paralleli** che condividono la stessa istanza di `IPlcService`:

- `PlcPollingWorker` — interroga il PLC internamente a ciclo continuo, per il monitoraggio automatico mostrato in TUI;
- `TcpServerService` — un `TcpListener` in ascolto sulla porta 5050, che accetta connessioni multiple e concorrenti da client esterni, instrada i comandi testuali ricevuti (`GET_STATUS`, `START_PUMP`, `STOP_PUMP`) verso `IPlcService` e restituisce la risposta sulla stessa connessione, mantenuta aperta per l'intera sessione.

Questo rende PlcBridge un vero endpoint di rete interrogabile da strumenti esterni (script di test, futuri sistemi HMI/SCADA), oltre che un simulatore auto-contenuto — mentre la thread-safety di `SimulatedPlcService` (tramite `lock`) garantisce che polling interno e client esterni non entrino mai in collisione sullo stesso stato.

### Dal modello a tag al servizio di dominio

La prima versione del Core esponeva un contratto generico a tag (`IPlcDriver`, `PlcTag`). È stato sostituito da `IPlcService`, che opera su un record immutabile (`PlcSystemStatus`) rappresentante l'intero stato macchina: un contratto più espressivo, tipizzato e meno soggetto a errori rispetto all'accesso per chiave stringa.

### Ciclo di vita e `CancellationToken`

Tutte le operazioni asincrone (polling, TCP Server) sono governate da un `CancellationToken` unico, propagato a tutti i layer alla chiusura (tasto ESC). Questo evita "task zombie" e chiude in modo pulito sia le connessioni interne sia il `TcpListener`, liberando la porta senza richiedere un `taskkill`.

### Logging con Serilog

Console (`[HH:mm:ss LVL] Messaggio`) + file giornaliero (`logs/plcbridge-YYYYMMDD.txt`, retention 3 file), configurato centralmente nel layer Worker.

### Unit Testing con xUnit

`PlcBridge.Tests` valida `SimulatedPlcService` tramite `IPlcService`, senza rete né socket:

- `ConnectAsync_ShouldSetStateToConnected`
- `DisconnectAsync_ShouldSetStateToDisconnected`
- `ReadTagAsync_WhenNotConnected_ShouldThrowException`
- `ReadTagAsync_Pressure_ShouldReturnDoubleValue` (range 10.0–15.0 Bar)
- `WriteAndRead_PumpStatus_ShouldUpdateValue`

### Integration Testing del TCP Server

Oltre ai test unitari, la suite contiene `TcpServerIntegrationTests`, che verifica il comportamento del sistema attraverso il vero stack di comunicazione TCP.

Il test:

- avvia un'istanza reale di `TcpPlcServer` sulla porta isolata `50505`;
- apre una connessione reale tramite `TcpClient` verso `127.0.0.1`;
- invia il comando `START_PUMP`;
- legge la risposta dal socket;
- verifica che la risposta sia `OK:PUMP_STARTED`.

In questo modo viene verificata l'integrazione tra **TCP Server, command processor e servizio PLC simulato**, senza dipendere da un processo server esterno.

## 🐛 Troubleshooting Log

| # | Problema | Causa | Risoluzione |
|---|---|---|---|
| 1 | `CS0579` attributi duplicati in fase di split del progetto | Il Default Compile Globbing di MSBuild include anche i file `obj/` generati dagli altri progetti | Progetto principale spostato in una cartella dedicata (`PlcBridge.Worker`) |
| 2 | Progetto di test annidato nel progetto principale → altri `CS0579` | Struttura cartelle errata | Creata una `.sln` in root, `dotnet sln add`, test escluso dal progetto principale |
| 3 | `IOException` / `SocketException 10053` al secondo comando | `using` chiudeva il socket a ogni iterazione | Loop interno `while (client.Connected)` lato server |
| 4 | Crash Spectre.Console: `malformed markup tag` | Spazi nei tag passati a `AnsiConsole.MarkupLine` | Rimosso lo spazio o usato `Markup.Escape()` |
| 5 | `MSB3026`/`MSB3027`, file `.exe` bloccato | Due processi (`server`/`client`) bloccavano l'eseguibile su Windows | Architettura a eseguibile unico (Generic Host); alternativa: `dotnet run --no-build` |
| 6 | `CS0234`/`CS0246`, interfaccia non trovata nonostante `ProjectReference` corretto | File dell'interfaccia in Core creato senza estensione `.cs` | Aggiunta l'estensione mancante |

## 💭 Riflessioni Tecniche

Il passaggio da script procedurali a .NET Generic Host ha mostrato la differenza tra codice che "funziona sul momento" e codice Industrial-Grade: la gestione della concorrenza (`lock`, `CancellationToken`) è ciò che garantisce affidabilità continua quando UI, polling interno e client TCP accedono allo stesso stato.

Il refactoring da `IPlcDriver`/`PlcTag` a `IPlcService`/`PlcSystemStatus` ha rafforzato il principio cardine della Clean Architecture: le interfacce permettono di sostituire la tecnologia sottostante (oggi un simulatore, domani un driver `S7Net`) senza toccare la logica applicativa — e lo stesso principio ha permesso di aggiungere il TCP Server esterno senza modificare il polling interno o il dominio.

## 🛠️ Tech Stack

- C# / .NET 10
- Clean Architecture (Core, Infrastructure, Worker)
- .NET Generic Host & `BackgroundService`
- TCP/IP Sockets (`TcpListener` multi-client + `TcpClient`)
- Spectre.Console (TUI)
- Dependency Injection / Inversion of Control
- xUnit (Unit & Integration Testing)
- GitHub Actions (CI/CD)

## 🖼️ Screenshot

**ULTIMA VERSIONE — TCP Server esterno (porta 5050) con connessione client di prova (PowerShell):**

![Server TCP/IP in ascolto sulla porta 5050 e sessione di comandi da un client PowerShell esterno](docs/images/server-tcp-ip-plcbridge.png)

*(Screenshot di versioni precedenti)*

**Worker — avvio con .NET Generic Host, polling in background e Graceful Shutdown:**

![Worker: bootstrap, connessione al PLC, polling della pressione e arresto pulito con ESC](docs/images/worker-polling-session.png)

**Server — sessione su connessione persistente:**

![Server terminal](docs/images/server-terminal.png)

**Client — TUI di supervisione:**

![Client terminal](docs/images/client-terminal-monitor.png)

---

*Progetto sviluppato come parte del percorso di crescita professionale nel settore Industrial Software Engineering.*