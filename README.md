# RedisSharp 

[![Progress](https://backend.codecrafters.io/progress/redis/5ab46378-9363-41ac-a459-bd1b264393bc)](https://app.codecrafters.io/users/codecrafters-bot?r=2qF)

A Redis-compatible in-memory data store built from scratch in C# as part of the [CodeCrafters "Build Your Own Redis"](https://codecrafters.io/challenges/redis) challenge. Implements the RESP2 protocol over raw TCP, concurrent client handling, full data structure commands, Redis Streams, ACID-like transactions, and optimistic locking — without any Redis libraries.

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
├── Protocol/
│   ├── RespParser.cs            → Parses raw RESP2 bytes into string parts
│   └── RespWriter.cs            → Encodes and writes all RESP2 response types
└── Commands/
    ├── ICommandHandler.cs       → Interface: CommandName + Handle()
    ├── CommandDispatcher.cs     → Routes commands, manages per-connection state
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
    ├── DiscardHandler.cs
    ├── WatchHandler.cs
    └── UnwatchHandler.cs
```

Each client gets its own `Task` and its own `CommandDispatcher` instance — ensuring transaction state, watched keys, and dirty flags are never shared between connections.

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
| `XREAD` | `XREAD [BLOCK ms] STREAMS <key> [key ...] <id> [id ...]` | Reads from streams, optionally blocking |

### Transactions

| Command | Syntax | Description |
|---------|--------|-------------|
| `MULTI` | `MULTI` | Starts a transaction block |
| `EXEC` | `EXEC` | Executes all queued commands atomically |
| `DISCARD` | `DISCARD` | Discards all queued commands and exits transaction |

### Optimistic Locking

| Command | Syntax | Description |
|---------|--------|-------------|
| `WATCH` | `WATCH <key> [key ...]` | Watches keys for modification before EXEC |
| `UNWATCH` | `UNWATCH` | Clears all watched keys |

---

## How It Works

### RESP2 Protocol

Every Redis command is sent as an array of bulk strings:

```
SET foo bar  →  *3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$3\r\nbar\r\n
```

`RespParser` strips the `*` and `$` framing. `RespWriter` handles all response types: simple strings, bulk strings, null bulk strings, integers, arrays, nested arrays, and null arrays.

### Concurrency & Per-Connection State

```
Main loop
 └── AcceptTcpClientAsync()
      └── Task.Run()                    ← one task per client
           └── new CommandDispatcher()  ← one dispatcher per client
                └── ReadAsync loop
```

Each client gets its own `CommandDispatcher`. Transaction state, the command queue, watched keys, and dirty flags all live inside the dispatcher — never in the shared `Store`.

### Unified Type Store (Strategy Pattern)

```csharp
ConcurrentDictionary<string, RedisValue> _store
```

`RedisValue` is an abstract base. `RedisString`, `RedisList`, and `RedisStream` extend it. Adding new types requires zero changes to the dictionary or dispatcher logic.

```
RedisValue (abstract, has IsExpired)
 ├── RedisString   → string Value
 ├── RedisList     → List<string> Items
 └── RedisStream   → List<(string Id, Dictionary<string,string> Fields)> Entries
```

### TTL / Expiry (Lazy)

Expiry timestamps are stored at `SET` time. On every `GET`, if `DateTime.UtcNow > ExpiresAt`, the key is removed and nil returned. No background thread needed.

### Blocking Operations

Both `BLPOP` and `XREAD BLOCK` use `TaskCompletionSource` for zero-CPU suspension:

```
BLPOP / XREAD BLOCK arrives → list/stream empty
  → create TCS → store in waiters dict → await tcs.Task  (suspended, no CPU cost)

RPUSH / XADD arrives
  → check waiters dict → TCS.SetResult(value) → waiter resumes instantly
```

Timeout is handled with `Task.WhenAny(tcs.Task, Task.Delay(timeout))`.

### Redis Streams

- Entry IDs are `<milliseconds>-<sequence>` with strict ordering enforcement
- `*` in sequence position auto-generates the next sequence number
- Full `*` uses current Unix timestamp with auto sequence
- `XRANGE` supports `-` (first entry) and `+` (last entry) as bounds
- `XREAD BLOCK` with `$` reads only entries added after the command was issued

### Transactions (MULTI / EXEC / DISCARD)

```
MULTI   → enter queuing mode → OK
<cmd>   → enqueue, don't execute → QUEUED
EXEC    → execute all queued commands in order → array of results
DISCARD → clear queue, exit transaction mode → OK
```

Transaction state and command queue live in `CommandDispatcher`, not `Store` — one instance per connection, full isolation.

### Optimistic Locking (WATCH / UNWATCH)

```
WATCH key1 key2  → mark keys as watched for this connection
<any write to key1 or key2 by another client>  → key marked dirty
MULTI / EXEC     → if any watched key is dirty → abort, return null array (*-1\r\n)
                 → if no dirty keys → execute normally
UNWATCH          → clear all watched keys and dirty flags
```

Key behaviors:
- `WATCH` on a missing key still tracks it — if the key is created before `EXEC`, the transaction aborts
- `EXEC` and `DISCARD` both automatically call `UNWATCH`
- Multiple keys can be watched in a single `WATCH` call
- Store notifies all watching connections whenever a key is mutated

This is the **optimistic concurrency** model: assume no conflict, but verify before committing. If a conflict is detected, return nil and let the client retry.

---

## Running Locally

```bash
dotnet run
```

```bash
# Optimistic locking
redis-cli WATCH mykey
redis-cli MULTI
redis-cli SET mykey newvalue
redis-cli EXEC          # succeeds if mykey wasn't modified by another client

# If another client ran SET mykey something between WATCH and EXEC:
redis-cli EXEC          # returns (nil) — transaction aborted

# Unwatch
redis-cli WATCH key1 key2
redis-cli UNWATCH       # clears both
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
- [x] RPUSH (single + multiple)
- [x] LRANGE (positive + negative indices)
- [x] LPUSH
- [x] LLEN
- [x] LPOP (single + multiple)
- [x] BLPOP (indefinite + timeout)

### Streams
- [x] TYPE command
- [x] XADD (create, validate IDs)
- [x] Partial + fully auto-generated IDs
- [x] XRANGE (with `-` / `+` bounds)
- [x] XREAD (single + multiple streams)
- [x] XREAD BLOCK (blocking + timeout + `$` cursor)

### Transactions
- [x] INCR
- [x] MULTI / EXEC / DISCARD
- [x] Empty transaction
- [x] Queueing + executing commands
- [x] Failures within transactions
- [x] Multiple concurrent transactions

### Optimistic Locking
- [x] WATCH command
- [x] WATCH inside transaction
- [x] Tracking key modifications
- [x] Watching multiple keys
- [x] Watching missing keys
- [x] UNWATCH command
- [x] Unwatch on EXEC
- [x] Unwatch on DISCARD

---

## Design Decisions

**Per-connection `CommandDispatcher`**
Transaction state, watched keys, and dirty flags are connection-scoped by definition. Putting them in `Store` (shared across all clients) would be a correctness bug. Each `Task.Run` creates a fresh `CommandDispatcher` — isolation is architectural, not guarded by locks.

**`TaskCompletionSource` for blocking commands**
Polling wastes CPU and adds latency. TCS suspends the task at zero CPU cost and wakes it the instant a value is available — the same mechanism .NET's own async I/O uses internally.

**Optimistic over pessimistic locking**
Pessimistic locking (locking a key for the duration of a transaction) blocks all other writers. Optimistic locking assumes no conflict and only validates at commit time — much better for read-heavy workloads where conflicts are rare. Redis chose this model deliberately.

**Unified `_store` with Strategy pattern**
One dictionary, typed values. Adding `RedisHash` or `RedisSet` means one new class — no changes to Store's lookup logic, no changes to the dispatcher.

**Lazy expiry**
A background sweeper adds complexity and concurrency risk. Redis itself uses lazy expiry as the primary mechanism. Checking on read is simpler, correct, and good enough.

---

## What I Learned

- RESP2 wire protocol at the byte level, including nested array encoding
- Raw TCP server design with `TcpListener` and `NetworkStream`
- `async/await` + `Task.Run` for concurrent client handling
- `TaskCompletionSource<T>` for event-driven task suspension and resumption
- `Task.WhenAny` for timeout racing
- Strategy pattern with polymorphic `RedisValue` types
- Why transaction and watch state must be per-connection — and how to enforce it architecturally
- Optimistic concurrency: watch-then-verify is better than lock-then-execute for low-conflict workloads
- Redis Streams entry ID semantics, auto-generation, and the `$` live cursor
- Dirty-key tracking across concurrent connections without global locks