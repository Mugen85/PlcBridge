# PlcBridge

![Build Status](https://github.com/Mugen85/PlcBridge/actions/workflows/dotnet.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

PlcBridge è uno strumento di studio e simulazione sviluppato per colmare la distanza tra software gestionale (.NET) e hardware industriale (PLC).

Questo progetto nasce come banco di prova per testare la comunicazione TCP/IP e implementare modelli di interazione Request-Response in ambienti industriali, superando i limiti dei modelli a push continuo.

## ✨ Caratteristiche

- **Simulatore Server/PLC Stateful:** gestione asincrona delle connessioni sulla porta 5000, con memoria interna che simula i registri di un vero PLC
- **Client di Monitoraggio:** interfaccia a TUI (Terminal User Interface) con Spectre.Console per interrogare e controllare lo stato della macchina in modo leggibile e strutturato
- **Comandi di lettura:** `READ_PRESSURE`, `READ_TEMP`, `SYSTEM_STATUS`
- **Comandi di controllo attuatori:** `START_PUMP`, `STOP_PUMP` per l'interazione bidirezionale tipica di una vera HMI
- **Protocollo Polling:** implementazione di una logica Master-Slave per la richiesta e l'invio dati
- **CI/CD Ready:** pipeline GitHub Actions integrata per la validazione automatica del codice ad ogni modifica

## 🚀 Come iniziare

1. Clona la repository:
   ```
   git clone https://github.com/Mugen85/PlcBridge.git
   ```
2. Apri il terminale nella cartella del progetto.
3. Avvia il Server:
   ```
   dotnet run server
   ```
4. Avvia il Client (in un altro terminale):
   ```
   dotnet run client
   ```

## ✅ Stato del Progetto

- [x] **Setup Ambiente:** Completato (struttura organizzata in GitHub Projects)
- [x] **Implementazione TCP Base:** Socket TCP/IP funzionanti
- [x] **Implementazione Polling:** Sistema Request/Response (Client/Server) configurato
- [x] **CI/CD Pipeline:** `dotnet.yml` su GitHub Actions — stato **Green** (build superata)
- [x] **Version Control:** Repository Git collegata correttamente al remoto GitHub
- [x] **UI Engineering:** Implementazione TUI (Terminal User Interface) con Spectre.Console
- [x] **Stateful Server & Controllo Attuatori:** memoria di stato interna e comandi di scrittura (`START_PUMP`, `STOP_PUMP`)

## 🏗️ Architettura e Logica

### Il ciclo di vita del processo

I programmi server occupano il socket finché non vengono terminati correttamente. La gestione delle risorse di sistema (es. `taskkill /F /IM PlcBridge.exe`) è fondamentale: se il processo resta "appeso", la porta 5000 rimane occupata e impedisce nuove esecuzioni.

### Protocollo Request-Response

Il progetto ha superato il modello a "push continuo" (il server invia dati a prescindere) in favore del modello a **Polling**:

1. Il **Client** richiede un dato specifico (es. `READ_PRESSURE`)
2. Il **Server** valuta la richiesta
3. Solo se valida, il Server risponde

Questo è il cuore di ogni comunicazione Master-Slave nell'automazione industriale.

### Evoluzione visuale: verso una TUI con Spectre.Console

Per migliorare l'usabilità dello strumento senza passare subito alla complessità del Web (Blazor), è stato introdotto **Spectre.Console**:

- **Motivazione:** le interfacce a riga di comando (CLI) sono standard nell'automazione, ma le tabelle e i colori di Spectre permettono di creare una "dashboard" leggibile istantaneamente dall'operatore.
- **Lezione:** separare la logica di comunicazione (Socket) dalla logica di presentazione (TUI) rende il codice più manutenibile e professionale.

### Nuova evoluzione: stato del server e comandi di scrittura (controllo attuatori)

Per rendere il simulatore un vero sistema industriale prima del salto verso il web (Blazor), sono stati introdotti due concetti cruciali:

1. **Memoria di Stato (Stateful Server):** il server ora mantiene variabili globali (`isPumpRunning`, `currentPressure`) che simulano i registri interni di un PLC. Non si limita più a rispondere con dati estemporanei, ma tiene traccia dello stato degli attuatori.
2. **Comandi di Scrittura / Controllo:** oltre alle letture passive (`READ_TEMP`, `READ_PRESSURE`), sono stati aggiunti comandi attivi come `START_PUMP`, `STOP_PUMP` e `SYSTEM_STATUS`. Questo simula l'interazione bidirezionale tipica di una vera HMI (accensione/spegnimento di macchinari).

### Componenti

| Componente | Descrizione |
|---|---|
| **Server** | Ascolta su `IPAddress.Any` (qualsiasi interfaccia di rete) sulla porta 5000, gestisce i comandi in entrata (lettura e scrittura) e mantiene lo stato interno degli attuatori |
| **Client** | Si connette, invia un comando stringa e attende la risposta bufferizzata, con gestione sicura dei tipi (`?? string.Empty`) e visualizza i dati tramite `AnsiConsole` (tabelle formattate e `SelectionPrompt` per input a prova di errore) |

## 🐛 Troubleshooting Log

Lezioni raccolte durante lo sviluppo, utili come riferimento futuro.

### 1. Errore di file bloccato (MSB3026)
- **Problema:** il compilatore falliva perché l'eseguibile era in uso.
- **Causa:** il server (`PlcBridge.exe`) era ancora attivo in background.
- **Risoluzione:** `taskkill /F /IM PlcBridge.exe`, o preferibilmente terminazione corretta con `Ctrl+C`.

### 2. Errori di compilazione C# (CS8803, CS1022, CS0260)
- **Problema:** errori su "missing partial modifier" o "top-level statements".
- **Causa:** parentesi graffe `}` fuori posto, che rompevano la struttura della classe.
- **Risoluzione:** revisione gerarchica delle parentesi: la classe deve contenere tutto il codice.

### 3. Git: "remote origin already exists"
- **Problema:** impossibilità di aggiungere il remote durante la configurazione iniziale.
- **Risoluzione:** `git remote set-url origin [URL]` sovrascrive il collegamento esistente in modo pulito.

### 4. Gestione input nullo
- **Problema:** warning del compilatore su possibili input nulli.
- **Risoluzione:** utilizzo dell'operatore `?? string.Empty` per garantire sicurezza di tipo.

### 5. Configurazione CI/CD (GitHub Actions)
- **Problema:** incertezza sulla struttura delle cartelle.
- **Risoluzione:** percorso esatto `.github/workflows/dotnet.yml`. Semaforo verde = codice validato.

### 6. Installazione pacchetti NuGet
- **Problema:** necessità di utilizzare librerie esterne (Spectre.Console).
- **Risoluzione:** utilizzo del comando `dotnet add package Spectre.Console`. Fondamentale assicurarsi di essere nella cartella corretta (file `.csproj`) prima di eseguire il comando.

## 💭 Riflessioni Tecniche

La configurazione di una pipeline CI/CD ha chiarito che il software non è solo "scrivere codice", ma creare un processo: avere un test automatico su GitHub garantisce che, anche a distanza, un'eventuale modifica che "rompe" qualcosa venga segnalata immediatamente.

La gestione del "remote already exists" in Git ha insegnato che i messaggi d'errore non vanno temuti, ma letti come indicazioni stradali: Git non vuole creare duplicati, vuole solo sapere quale sia la strada corretta da seguire.

L'adozione di Spectre.Console ha trasformato il Client da un semplice script di test a uno strumento di supervisione vero e proprio, dimostrando che l'usabilità conta quanto la funzionalità.

## 🛠️ Tech Stack

- C# / .NET
- TCP/IP Sockets
- Spectre.Console (TUI)
- GitHub Actions (CI/CD)

## 🖼️ Screenshot

**Client — avvio pompa e system status:**

![Client controllo pompa e stato sistema](docs/images/client-screenshot-1.png)

**Client — riepilogo comandi e risposte:**

![Client riepilogo comandi](docs/images/client-screenshot-2.png)

**Server — ricezione comando e attuazione:**

![Server comando attuatore](docs/images/server-screenshot.png)

---

*Progetto sviluppato come parte del percorso di crescita professionale nel settore Industrial Software Engineering.*