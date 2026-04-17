# RedisSharp 

[![Progress](https://backend.codecrafters.io/progress/redis/5ab46378-9363-41ac-a459-bd1b264393bc)](https://app.codecrafters.io/users/codecrafters-bot?r=2qF)

A Redis-compatible in-memory data store built from scratch in C# as part of the [CodeCrafters "Build Your Own Redis"](https://codecrafters.io/challenges/redis) challenge. Implements the RESP2 protocol over raw TCP, concurrent client handling, full data structure commands, Redis Streams, and ACID-like transactions — without any Redis libraries.

---

## Architecture

```
RedisSharp/
├── Program.cs                   → TCP listener loop, accepts clients, spawns tasks
├── Core/
│   ├── Store.cs                 → Unified thread-safe in-memory store
│   ├── RedisValue.cs            → Abstract base with expiry logic
│   ├── RedisString.cs           → String value type
│   ├── RedisList.cs             → List value type
│   └── RedisStream.cs           → Stream value type
|   └── ClientContext.cs
|   └── ClientHandler.cs 
├── Protocol/
│   ├── RespParser.cs            → Parses raw RESP2 bytes into string parts
│   └── RespWriter.cs            → Encodes and writes all RESP2 response types
└── Commands/
    ├── ICommandHandler.cs       → Interface: CommandName + Handle()
    ├── CommandDispatcher.cs     → Routes commands, manages per-connection transaction state
    ├── PingHandler.cs
    ├── EchoHandler.cs
    ├── SetHandler.cs
    ├── GetHandler.cs
    ├── IncrHandler.cs
    ├── TypeHandler.cs
    ├── RPushHandler.cs
    ├── LPushHandler.cs
    ├── LRangeHandler.cs
    ├── LPopHandler.cs
    ├── LLenHandler.cs
    ├── BLPopHandler.cs
    ├── XAddHandler.cs
    ├── XRangeHandler.cs
    ├── XReadHandler.cs
    ├── MultiHandler.cs
    ├── ExecHandler.cs
    └── DiscardHandler.cs
```

Each client connection gets its own `Task` and its own `CommandDispatcher` instance — ensuring transaction state is never shared between connections.

---

## Supported Commands

### Strings

| Command | Syntax | Description |
|---------|--------|-------------|
| `PING` | `PING` | Returns `PONG` |
| `ECHO` | `ECHO <message>` | Returns the message back |
| `SET` | `SET <key> <value> [EX seconds\|PX milliseconds]` | Sets a key with optional expiry |
| `GET` | `GET <key>` | Returns the value, or nil if expired/missing |
| `INCR` | `INCR <key>` | Atomically increments an integer value |
| `TYPE` | `TYPE <key>` | Returns the type of the value at a key |

### Lists

| Command | Syntax | Description |
|---------|--------|-------------|
| `RPUSH` | `RPUSH <key> <value> [value ...]` | Appends one or more elements to the tail |
| `LPUSH` | `LPUSH <key> <value> [value ...]` | Prepends one or more elements to the head |
| `LRANGE` | `LRANGE <key> <start> <stop>` | Returns elements in range (supports negative indices) |
| `LLEN` | `LLEN <key>` | Returns the length of the list |
| `LPOP` | `LPOP <key> [count]` | Removes and returns elements from the head |
| `BLPOP` | `BLPOP <key> <timeout>` | Blocking pop — waits until element available or timeout |

### Streams

| Command | Syntax | Description |
|---------|--------|-------------|
| `XADD` | `XADD <key> <id\|*> <field> <value> ...` | Appends entry with auto or manual ID |
| `XRANGE` | `XRANGE <key> <start> <end>` | Returns entries in ID range (`-` and `+` supported) |
| `XREAD` | `XREAD [BLOCK ms] STREAMS <key> [key ...] <id> [id ...]` | Reads from one or more streams, optionally blocking |

### Transactions

| Command | Syntax | Description |
|---------|--------|-------------|
| `MULTI` | `MULTI` | Starts a transaction block |
| `EXEC` | `EXEC` | Executes all queued commands atomically |
| `DISCARD` | `DISCARD` | Discards all queued commands and exits transaction |

---

## How It Works

### RESP2 Protocol

Every Redis command is sent as an array of bulk strings:

```
SET foo bar  →  *3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$3\r\nbar\r\n
```

`RespParser` strips the `*` and `$` framing, leaving `["SET", "foo", "bar"]`. `RespWriter` handles all response types: simple strings, bulk strings, null bulk strings, integers, arrays, nested arrays, and null arrays.

### Concurrency & Per-Connection State

```
Main loop
 └── AcceptTcpClientAsync()
      └── Task.Run()              ← one task per client
           └── new CommandDispatcher()   ← one dispatcher per client
                └── ReadAsync loop
```

Each client gets its own `CommandDispatcher` instance. Transaction state (`MULTI`/`EXEC`) lives inside the dispatcher, never in the shared `Store` — so one client's transaction never affects another.

### Unified Type Store (Strategy Pattern)

```csharp
ConcurrentDictionary<string, RedisValue> _store
```

`RedisValue` is an abstract base. `RedisString`, `RedisList`, and `RedisStream` extend it. Store retrieves a `RedisValue` and pattern-matches to the specific type. Adding new types requires zero changes to the dictionary logic.

```
RedisValue (abstract, has IsExpired)
 ├── RedisString   → string Value
 ├── RedisList     → List<string> Items
 └── RedisStream   → List<(string Id, Dictionary<string,string> Fields)> Entries
```

### TTL / Expiry (Lazy)

Expiry timestamps are stored at `SET` time. On every `GET`, if `DateTime.UtcNow > ExpiresAt`, the key is removed and nil is returned. No background thread needed.

### Blocking Operations

Both `BLPOP` and `XREAD BLOCK` use `TaskCompletionSource` for zero-CPU suspension:

```
Client calls BLPOP / XREAD BLOCK
  → list/stream empty → create TCS → store in waiters dict → await tcs.Task

RPUSH / XADD arrives
  → check waiters dict → dequeue TCS → tcs.SetResult(value) → waiter resumes instantly
```

Timeout is handled with `Task.WhenAny(tcs.Task, Task.Delay(timeout))`.

### Redis Streams (XADD / XRANGE / XREAD)

- Entry IDs are `<milliseconds>-<sequence>` pairs with strict ordering enforcement
- `*` in the sequence position auto-generates the next sequence number
- Full `*` ID uses current Unix timestamp with auto sequence
- `XRANGE` supports `-` (first entry) and `+` (last entry) as bounds
- `XREAD BLOCK` with `$` as ID reads only entries added after the command was issued

### Transactions (MULTI / EXEC / DISCARD)

```
MULTI  → dispatcher enters queuing mode, returns OK
<cmd>  → command is queued, returns QUEUED (not executed)
EXEC   → all queued commands execute in order, returns array of results
DISCARD → queue is cleared, transaction mode exits, returns OK
```

Key design decision: transaction state and the command queue live in `CommandDispatcher`, not `Store`. This guarantees per-connection isolation — a requirement for correctness in a concurrent server.

Error handling inside transactions:
- `EXEC` without `MULTI` → returns error
- `MULTI` inside `MULTI` → returns error (no nesting)
- Command errors inside a transaction execute and return their individual errors without aborting the rest

---

## Running Locally

```bash
dotnet run
```

```bash
# Strings
redis-cli PING
redis-cli SET foo bar EX 10
redis-cli GET foo
redis-cli INCR counter

# Lists
redis-cli RPUSH mylist a b c
redis-cli LRANGE mylist 0 -1
redis-cli BLPOP mylist 5

# Streams
redis-cli XADD mystream '*' name Sara surname Sanfilippo
redis-cli XRANGE mystream - +
redis-cli XREAD STREAMS mystream 0-0

# Transactions
redis-cli MULTI
redis-cli SET foo bar
redis-cli INCR counter
redis-cli EXEC

# Discard
redis-cli MULTI
redis-cli SET foo bar
redis-cli DISCARD
```

---

## Stages Completed

### Core
- [x] TCP server + PING
- [x] Multiple commands per connection
- [x] Concurrent clients
- [x] ECHO
- [x] SET / GET
- [x] TTL expiry (EX / PX)

### Lists
- [x] RPUSH (single + multiple elements)
- [x] LRANGE (positive + negative indices)
- [x] LPUSH
- [x] LLEN
- [x] LPOP (single + multiple)
- [x] BLPOP (indefinite + timeout)

### Streams
- [x] TYPE command
- [x] XADD (create stream, validate IDs)
- [x] Partial auto-generated IDs
- [x] Fully auto-generated IDs
- [x] XRANGE (with `-` / `+` bounds)
- [x] XREAD (single + multiple streams)
- [x] XREAD BLOCK (blocking + timeout + `$` cursor)

### Transactions
- [x] INCR (1/3, 2/3, 3/3)
- [x] MULTI
- [x] EXEC
- [x] Empty transaction
- [x] Queueing commands
- [x] Executing a transaction
- [x] DISCARD
- [x] Failures within transactions
- [x] Multiple transactions

---

## Design Decisions

**Per-connection `CommandDispatcher` instead of global transaction state**
Redis transactions are connection-scoped. Using a `static` field on `Store` for transaction state would cause Client 1's `MULTI` to affect Client 2 — a correctness bug under concurrency. Each `Task.Run` creates a fresh `CommandDispatcher`, isolating state completely.

**`TaskCompletionSource` for blocking commands**
Polling with `Task.Delay` in a loop wastes CPU and adds latency. `TaskCompletionSource` suspends the task at zero CPU cost and wakes it the instant a value is available — same mechanism used by .NET's own async I/O primitives.

**Unified `_store` with Strategy pattern**
One dictionary, typed values. Adding a new Redis type (Hashes, Sets) means creating one new class — no changes to Store's core lookup or dispatcher routing logic.

**Lazy expiry over active expiry**
A background sweeper thread adds complexity and concurrency risk. Redis itself uses lazy expiry as the primary mechanism (with occasional active sweeps for memory pressure). At this scale, checking on read is simpler and correct.

---

## What I Learned

- RESP2 wire protocol at the byte level, including nested array encoding for Streams
- Raw TCP server design with `TcpListener` and `NetworkStream` in .NET
- `async/await` and `Task.Run` for concurrent client handling without thread-per-client overhead
- `TaskCompletionSource<T>` for event-driven task suspension and resumption
- `Task.WhenAny` for timeout racing
- Strategy pattern with polymorphic `RedisValue` types behind a unified store
- Why transaction state must be per-connection, not shared — and how to enforce that architecturally
- Redis Streams entry ID semantics, auto-generation, and the `$` live cursor
- Lazy expiry: simple, correct, and how real Redis works