# RedisSharp 

[![Progress](https://backend.codecrafters.io/progress/redis/5ab46378-9363-41ac-a459-bd1b264393bc)](https://app.codecrafters.io/users/codecrafters-bot?r=2qF)

A Redis-compatible in-memory data store built from scratch in C# as part of the [CodeCrafters "Build Your Own Redis"](https://codecrafters.io/challenges/redis) challenge. Implements the RESP2 wire protocol over raw TCP, concurrent client handling, full data structure commands, Redis Streams, ACID-like transactions, optimistic locking, Pub/Sub, RDB/AOF persistence, master-replica replication, geospatial commands, and authentication — without any Redis libraries.

---

## Architecture

```
RedisSharp/
├── Program.cs
├── GlobalUsings.cs
├── Core/
│   ├── Store.cs                        → Unified facade coordinating sub-services
│   ├── DatabaseStorage.cs              → Key-value store operations
│   ├── GeoService.cs                   → Geospatial operations
│   └── PubSubBroker.cs                 → Pub/Sub channel management
├── Redis/
│   ├── RedisValue.cs                   → Abstract base with expiry logic
│   ├── RedisString.cs
│   ├── RedisList.cs
│   ├── RedisStream.cs
│   └── RedisSortedSet.cs
├── Common/
│   ├── Geo/
│   │   ├── GeohashEncoder.cs
│   │   ├── GeohashDecoder.cs
│   │   └── HaversineCalculator.cs
│   └── Interfaces/
│       └── IReplicationRole.cs
├── Network/
│   ├── ClientHandler.cs
│   ├── ClientContext.cs
│   └── Replication/
│       ├── Master.cs
│       └── Replica.cs
├── Protocol/
│   ├── RespParser.cs
│   ├── RespWriter.cs
│   └── NullStream.cs
└── Commands/
    ├── CommandHandler.cs               → Reflection-based registry, transaction routing
    ├── ICommandHandler.cs
    ├── Key/                            → SET, GET, INCR, TYPE, DEL, EXPIRE, TTL...
    ├── List/                           → RPUSH, LPUSH, LRANGE, LPOP, LLEN, BLPOP
    ├── Stream/                         → XADD, XRANGE, XREAD
    ├── SortedSet/                      → ZADD, ZRANGE, ZRANK, ZCARD, ZSCORE, ZREM
    ├── Geo/                            → GEOADD, GEOPOS, GEODIST, GEOSEARCH
    ├── Transaction/                    → MULTI, EXEC, DISCARD, WATCH, UNWATCH
    ├── PubSub/                         → SUBSCRIBE, UNSUBSCRIBE, PUBLISH
    ├── Replication/                    → INFO, REPLCONF, PSYNC, WAIT
    ├── Config/                         → CONFIG GET/SET, KEYS
    └── Connection/                     → PING, ECHO, AUTH, ACL (WHOAMI, GETUSER, SETUSER)
```

---

## Supported Commands

### Strings
| Command | Description |
|---------|-------------|
| `PING` / `ECHO` | Connectivity and echo |
| `SET <key> <value> [EX\|PX]` | Set with optional expiry |
| `GET <key>` | Get value or nil |
| `INCR <key>` | Atomically increment integer |
| `TYPE <key>` | Returns value type |

### Lists
| Command | Description |
|---------|-------------|
| `RPUSH` / `LPUSH` | Append / prepend elements |
| `LRANGE` | Range query with negative index support |
| `LPOP [count]` | Remove from head |
| `LLEN` | List length |
| `BLPOP <timeout>` | Blocking pop with timeout |

### Sorted Sets
| Command | Description |
|---------|-------------|
| `ZADD` | Add member with score |
| `ZRANGE` | List members by rank (negative indices) |
| `ZRANK` / `ZCARD` / `ZSCORE` / `ZREM` | Rank, count, score, remove |

### Streams
| Command | Description |
|---------|-------------|
| `XADD` | Append entry with auto/partial/manual ID |
| `XRANGE` | Range query (`-` and `+` supported) |
| `XREAD [BLOCK ms]` | Read with optional blocking and `$` cursor |

### Transactions
| Command | Description |
|---------|-------------|
| `MULTI` / `EXEC` / `DISCARD` | Transaction lifecycle |
| `WATCH` / `UNWATCH` | Optimistic locking |

### Pub/Sub
| Command | Description |
|---------|-------------|
| `SUBSCRIBE` / `UNSUBSCRIBE` | Channel subscription |
| `PUBLISH` | Deliver message to subscribers |

### Geospatial
| Command | Description |
|---------|-------------|
| `GEOADD` | Add location with geohash score |
| `GEOPOS` | Return decoded coordinates |
| `GEODIST [m\|km\|mi\|ft]` | Haversine distance between two members |
| `GEOSEARCH` | Radius-based location search |

### Authentication
| Command | Description |
|---------|-------------|
| `ACL SETUSER` / `ACL GETUSER` / `ACL WHOAMI` | User management |
| `AUTH <username> <password>` | Authenticate connection |

### Replication
| Command | Description |
|---------|-------------|
| `INFO replication` | Role, replid, offset, replica count |
| `REPLCONF` / `PSYNC` | Handshake and full resync |
| `WAIT <n> <timeout>` | Block until N replicas acknowledge |

---

## How It Works

### RESP2 Protocol
Every command is a RESP array of bulk strings. `RespParser` strips framing, leaving clean command parts. `RespWriter` handles all response types including nested arrays for Streams, Transactions, and ACL responses.

### Concurrency & Per-Connection State
Each client gets its own `Task.Run` and its own `ClientContext` — transactions, watched keys, subscriptions, and auth state are never shared between connections. `Store` uses `ConcurrentDictionary` for the shared keyspace and `lock` for thread-safe nested operations.

### Reflection-Based Command Registry
`CommandHandler` uses assembly scanning to automatically discover and register all `ICommandHandler` implementations — no manual registration list. Adding a new command requires only creating the handler class; the registry picks it up at startup (Open/Closed Principle).

### Store Facade (SRP)
`Store` delegates to three focused sub-services:
- `DatabaseStorage` — key-value operations, expiry, version tracking
- `GeoService` — geohash encoding/decoding, Haversine calculations
- `PubSubBroker` — subscriber registry, message delivery

### Unified Type Store (Strategy Pattern)
```
ConcurrentDictionary<string, RedisValue>
 ├── RedisString   → string Value + ExpiresAt
 ├── RedisList     → List<string> Items
 ├── RedisStream   → List<(Id, Fields)> Entries
 └── RedisSortedSet → SortedSet<(double score, string value)>
```

### Blocking Operations
`BLPOP` and `XREAD BLOCK` use `TaskCompletionSource<T>` — zero-CPU suspension, instant wake-up on `SetResult`. Timeout via `Task.WhenAny(tcs.Task, Task.Delay(timeout))`.

### Transactions + Optimistic Locking
`MULTI/EXEC/DISCARD` with per-connection command queuing. `WATCH` records key versions; `EXEC` aborts if any watched key was mutated by a concurrent writer. Auto-UNWATCH on `EXEC`/`DISCARD`.

### Pub/Sub
`PubSubBroker` maps channel → `List<NetworkStream>`. `PUBLISH` takes a snapshot copy of the list (thread-safe) and writes directly to each subscriber stream — zero intermediary, instant delivery.

### Geospatial
`GEOADD` encodes coordinates as 52-bit geohash scores using bit-interleaving (Morton code), stored in a Sorted Set. `GEODIST` decodes scores back to coordinates then applies Haversine with Redis's exact Earth radius (6372797.560856 m).

### Replication
Replica performs PING → REPLCONF → PSYNC handshake, receives empty RDB, then processes propagated write commands via `NullStream` (responses discarded). Master tracks per-replica offsets for `WAIT` coordination using REPLCONF GETACK.

### Authentication
Passwords stored as SHA-256 hashes in `Store` keyed by username. `AUTH` hashes the provided password and compares against stored hashes. Connection enforcement happens at the `ClientContext` level after authentication succeeds.

### Persistence
- **RDB**: `RdbLoader` hydrates the keyspace from a binary file on startup including expiry timestamps
- **AOF**: Append-only file tracks write commands; configurable directory, filename, and filtering

---

## Design Decisions

**Reflection-based command registry** — `CommandHandler` scans the assembly for `ICommandHandler` implementations and resolves their constructors automatically. New commands self-register; no central list to maintain. This enforces OCP without ceremony.

**Store as facade over sub-services** — splitting `Store` into `DatabaseStorage`, `GeoService`, and `PubSubBroker` gives each class one reason to change (SRP) while `Store` remains the single entry point for command handlers.

**`TaskCompletionSource` for blocking commands** — zero CPU suspension, instant wake. Same primitive .NET uses internally for async I/O.

**`NullStream` for replica command processing** — propagated commands run through the identical handler pipeline as client commands. Responses discarded. Zero changes to existing handlers.

**Optimistic over pessimistic locking** — watch-then-verify at commit time. Better for low-conflict workloads. Redis's own design choice.

**Direct `NetworkStream` references in PubSub** — `PUBLISH` writes directly to subscriber streams with no message queue intermediary. Zero-copy, zero-latency delivery.

**Lazy expiry** — check on read, remove on access. How real Redis works for most keys.

---

## What I Learned

- RESP2 wire protocol at the byte level including nested arrays
- Raw TCP server design, concurrent client handling without thread-per-client overhead
- `TaskCompletionSource<T>`, `Task.WhenAny` for async coordination patterns
- Strategy pattern, Facade pattern, Open/Closed Principle in a real codebase
- Reflection-based service discovery and dependency injection
- Per-connection state isolation as an architectural invariant
- Optimistic concurrency with key version tracking
- Geohash encoding (Morton code / bit interleaving) and Haversine formula
- Master-replica replication: handshake, RDB transfer, offset tracking, coordinated ACKs
- SHA-256 password hashing and connection-level authentication enforcement
- AOF write-ahead logging and RDB snapshot loading