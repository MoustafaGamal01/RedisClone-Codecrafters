[![progress-banner](https://backend.codecrafters.io/progress/redis/5ab46378-9363-41ac-a459-bd1b264393bc)](https://app.codecrafters.io/users/codecrafters-bot?r=2qF)

# RedisSharp 
A Redis-compatible in-memory data store built from scratch in C# as part of the [CodeCrafters "Build Your Own Redis"](https://codecrafters.io/challenges/redis) challenge. Implements the RESP2 protocol over raw TCP, concurrent client handling via async/await, core data structures, and TTL expiry.

---

## Architecture

```
Program.cs          → TCP listener loop, accepts clients, spawns tasks
RespParser.cs       → Parses raw RESP2 bytes into string parts
CommandHandler.cs   → Routes parsed commands to the right logic
Store.cs            → Thread-safe in-memory key-value store (ConcurrentDictionary)
RespWriter.cs       → Encodes and writes RESP2 responses to the stream
```

Each client connection runs on its own `Task`, allowing the server to handle multiple concurrent clients without blocking.

---

## Supported Commands

| Command | Syntax | Description |
|---------|--------|-------------|
| `PING` | `PING` | Returns `PONG` |
| `ECHO` | `ECHO <message>` | Returns the message back |
| `SET` | `SET <key> <value> [EX seconds\|PX milliseconds]` | Sets a key with optional expiry |
| `GET` | `GET <key>` | Returns the value, or nil if expired/missing |

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

### TTL / Expiry

Expiry uses **lazy expiry** — the expiry time is stored alongside the value at `SET` time, and checked on every `GET`. Expired keys are removed on access rather than by a background thread.

```
SET foo bar PX 2000
→ stored as ("bar", expiresAt: UtcNow + 2000ms)

GET foo  (after 3 seconds)
→ UtcNow > expiresAt → remove key → return $-1\r\n (nil)
```

---

## Running Locally

```bash
dotnet run
```

The server starts on port `6379`. Test it with `redis-cli`:

```bash
redis-cli PING
# PONG

redis-cli SET foo bar
# OK

redis-cli GET foo
# bar

redis-cli SET temp value PX 2000
redis-cli GET temp   # bar
# wait 3 seconds...
redis-cli GET temp
# (nil)
```

---

## Running Tests

```bash
# Basic commands
redis-cli PING
redis-cli ECHO "hello world"
redis-cli SET name Ahmed
redis-cli GET name

# Expiry
redis-cli SET session token PX 3000
redis-cli GET session          # token
# wait 4 seconds...
redis-cli GET session          # (nil)

# Concurrent clients
Start-Job { redis-cli SET key1 val1 }; redis-cli SET key2 val2
redis-cli GET key1             # val1
redis-cli GET key2             # val2
```

---

## Stages Completed

- [x] TCP server — accepts a single connection
- [x] Handle multiple PING commands on one connection  
- [x] Concurrent clients via `Task.Run`
- [x] ECHO command with RESP parsing
- [x] SET and GET commands
- [x] TTL expiry with EX and PX options

---

## What I Learned

- How Redis clients and servers communicate at the byte level using RESP2
- Building a raw TCP server in .NET with `TcpListener` and `NetworkStream`
- Handling concurrent clients with `async/await` and `Task.Run`
- Why `ConcurrentDictionary` is necessary for thread-safe shared state
- Lazy expiry: why checking on read is simpler and often good enough for low-traffic stores