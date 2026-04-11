[![progress-banner](https://backend.codecrafters.io/progress/redis/5ab46378-9363-41ac-a459-bd1b264393bc)](https://app.codecrafters.io/users/codecrafters-bot?r=2qF)

# RedisSharp

A Redis-compatible in-memory data store built from scratch in C# as part of the [CodeCrafters "Build Your Own Redis"](https://codecrafters.io/challenges/redis) challenge. Implements the RESP2 protocol over raw TCP, concurrent client handling via async/await, full string and list commands, TTL expiry, and blocking operations.

---

## Architecture

```
RedisSharp/
├── Program.cs               → TCP listener loop, accepts clients, spawns tasks
├── Core/
│   ├── Store.cs             → Unified thread-safe in-memory store (single ConcurrentDictionary)
│   ├── RedisValue.cs        → Abstract base class with expiry logic
│   ├── RedisString.cs       → Concrete string value type
│   └── RedisList.cs         → Concrete list value type
├── Protocol/
│   ├── RespParser.cs        → Parses raw RESP2 bytes into string parts
│   └── RespWriter.cs        → Encodes and writes RESP2 responses to the stream
└── Commands/
    └── CommandHandler.cs    → Routes parsed commands to the right handler
```

Each client connection runs on its own `Task`, allowing the server to handle multiple concurrent clients without blocking the main loop.

---

## Supported Commands

### Strings

| Command | Syntax | Description |
|---------|--------|-------------|
| `PING` | `PING` | Returns `PONG` |
| `ECHO` | `ECHO <message>` | Returns the message back |
| `SET` | `SET <key> <value> [EX seconds\|PX milliseconds]` | Sets a key with optional expiry |
| `GET` | `GET <key>` | Returns the value, or nil if expired/missing |

### Lists

| Command | Syntax | Description |
|---------|--------|-------------|
| `RPUSH` | `RPUSH <key> <value> [value ...]` | Appends one or more elements to the tail |
| `LPUSH` | `LPUSH <key> <value> [value ...]` | Prepends one or more elements to the head |
| `LRANGE` | `LRANGE <key> <start> <stop>` | Returns elements in index range (supports negative indices) |
| `LLEN` | `LLEN <key>` | Returns the length of the list |
| `LPOP` | `LPOP <key> [count]` | Removes and returns elements from the head |
| `BLPOP` | `BLPOP <key> <timeout>` | Blocking pop — waits until an element is available or timeout expires |

---

## How It Works

### RESP2 Protocol

Redis clients communicate using the **Redis Serialization Protocol (RESP2)**. Every command is sent as an array of bulk strings:

```
SET foo bar
→ *3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$3\r\nbar\r\n
```

`RespParser` strips the `*` (array) and `$` (length) lines, leaving just the command parts: `["SET", "foo", "bar"]`.

### Concurrency

```
Main loop
 └── AcceptTcpClientAsync()   ← blocks until a client connects
      └── Task.Run()          ← each client gets its own async task
           └── ReadAsync loop ← reads commands, writes responses
```

`Store` uses `ConcurrentDictionary` to ensure thread-safe reads and writes across all client tasks.

### Unified Type Store (Strategy Pattern)

All Redis value types share a single dictionary:

```csharp
ConcurrentDictionary<string, RedisValue> _store
```

`RedisValue` is an abstract base class. `RedisString` and `RedisList` extend it, each carrying their own data. Store retrieves a `RedisValue` and casts to the specific type only where needed — adding a new type like `RedisHash` requires zero changes to the dictionary logic.

```
RedisValue (abstract)
 ├── RedisString  → string Value
 └── RedisList    → List<string> Items
```

### TTL / Expiry

Expiry uses **lazy expiry** — the expiry timestamp is stored at `SET` time and checked on every `GET`. Expired keys are removed on access.

```
SET foo bar PX 2000
→ stored as RedisString { Value="bar", ExpiresAt=UtcNow+2000ms }

GET foo  (after 3 seconds)
→ IsExpired = true → remove key → return $-1\r\n (nil)
```

### Blocking Operations (BLPOP)

`BLPOP` uses `TaskCompletionSource<string>` to suspend a client task without blocking the thread, then wake it up when `RPUSH` adds a value.

```
BLPOP arrives → list empty?
  YES → create TCS → store in waiters queue → await tcs.Task  (suspended)
  NO  → pop immediately and return

RPUSH arrives → waiters queue for this key?
  YES → dequeue oldest TCS → tcs.SetResult(value)  (wakes up BLPOP client)
  NO  → add to list normally
```

Timeout is handled by racing `tcs.Task` against `Task.Delay` using `Task.WhenAny`:

```csharp
var winner = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(timeout)));
// timeout won → return *-1\r\n
// waitTask won → return ["key", "value"]
```

Multiple clients blocking on the same key are served in FIFO order — the one waiting longest gets the value first.

---

## Running Locally

```bash
dotnet run
```

The server starts on port `6379`. Test it with `redis-cli`:

```bash
# Strings
redis-cli PING
redis-cli SET foo bar
redis-cli GET foo
redis-cli SET temp value PX 2000
# wait 3 seconds...
redis-cli GET temp        # (nil)

# Lists
redis-cli RPUSH mylist a b c
redis-cli LRANGE mylist 0 -1
redis-cli LPUSH mylist z
redis-cli LLEN mylist
redis-cli LPOP mylist
redis-cli LPOP mylist 2

# Blocking
# Terminal 1:
redis-cli BLPOP mylist 0
# Terminal 2:
redis-cli RPUSH mylist hello
# Terminal 1 immediately prints: 1) "mylist"  2) "hello"

# Blocking with timeout:
redis-cli BLPOP mylist 2
# (nil) after 2 seconds if nothing pushed
```

---

## Stages Completed

### Core
- [x] TCP server — accepts a single connection
- [x] Handle multiple PING commands on one connection
- [x] Concurrent clients via `Task.Run`
- [x] ECHO command with RESP parsing
- [x] SET and GET commands
- [x] TTL expiry with EX and PX options

### Lists
- [x] RPUSH — create list and append elements
- [x] RPUSH — append multiple elements
- [x] LRANGE — positive index slicing
- [x] LRANGE — negative index slicing
- [x] LPUSH — prepend elements
- [x] LLEN — query list length
- [x] LPOP — remove single element
- [x] LPOP — remove multiple elements
- [x] BLPOP — blocking retrieval (indefinite wait)
- [x] BLPOP — blocking retrieval with timeout

---

## Design Decisions

**Why `TaskCompletionSource` for BLPOP?**
A busy-wait loop (`while list.Empty sleep`) would hold a thread hostage. `TaskCompletionSource` suspends the task at zero CPU cost and wakes it instantly when signaled — the correct async pattern for event-driven coordination.

**Why a unified `_store` dictionary?**
Real Redis has a single keyspace — you can't `SET` and `LPUSH` to the same key simultaneously. Separate dictionaries would allow that inconsistency. The unified store with typed `RedisValue` objects enforces correct behavior and makes adding new types (Hashes, Sets) a one-file change.

**Why lazy expiry?**
A background expiry thread adds complexity and threading risk for marginal gain at this scale. Checking on read is simpler, correct, and is actually how Redis handles it for most keys.

---

## What I Learned

- How Redis clients and servers communicate at the byte level using RESP2
- Building a raw TCP server in .NET with `TcpListener` and `NetworkStream`
- Handling concurrent clients with `async/await` and `Task.Run`
- Why `ConcurrentDictionary` is necessary for thread-safe shared state
- `TaskCompletionSource<T>` for suspending and resuming tasks across concurrent clients
- `Task.WhenAny` for racing async operations against a timeout
- Strategy pattern — polymorphic value types behind a unified store interface
- Lazy expiry: why checking on read is simpler and often good enough