# Bid&Go — Backend

REST API and real-time backend for **Bid&Go**, a freight transport bidding platform. Companies post transport requests; independent drivers bid on them; the company picks a winner either by hand or by letting a scoring algorithm choose when bidding closes. The winning bid is paid through Stripe, and the two parties can chat and review each other afterwards.

Built with ASP.NET Core 8 and MySQL. Originally a team university project (see [Credits](#credits)).

**Live demo:** _(add your Render URL — the root path serves the Swagger UI)_

---

## Contents

- [How it works](#how-it-works)
- [Architecture](#architecture)
- [Tech stack](#tech-stack)
- [Running locally](#running-locally)
- [Configuration](#configuration)
- [Running with Docker](#running-with-docker)
- [Database and migrations](#database-and-migrations)
- [API overview](#api-overview)
- [Real-time notifications](#real-time-notifications)
- [Tests](#tests)
- [Deployment](#deployment)
- [Security notes](#security-notes)
- [Credits](#credits)

---

## How it works

There are two kinds of user, and the API enforces the split with JWT claims:

- **Company** — posts a transport request (cargo, dimensions, origin, destination, pickup/delivery dates, maximum price, bidding window), reviews incoming bids, selects a winner, and pays.
- **Driver** — browses open transport requests, places bids, and carries out the transport, updating its status along the way.

A request moves through roughly this lifecycle:

1. A company creates a **transport request** with a bidding window.
2. Drivers place **bids** while the window is open.
3. The company either **accepts a bid manually**, or enables **automatic selection**, in which case a background service scores the bids and picks the winner once the bidding window closes.
4. The company **pays** the winning bid through Stripe.
5. The driver **updates the transport status** as the job progresses.
6. Both sides can **chat** and leave a **review**.

Notifications for these events are pushed to clients over SignalR rather than polled.

## Architecture

A conventional layered ASP.NET Core setup:

```
Controllers/      HTTP endpoints, authorization policies, request validation
Services/         Business logic (bidding rules, selection algorithm, payments, email)
Repositories/     Data access, one per aggregate
Data/             EF Core DbContext and entity models
Migrations/       EF Core schema migrations
```

Controllers depend on service interfaces, services depend on repository interfaces, and everything is wired through the built-in DI container in `Program.cs`. The test project mocks at the interface boundaries, which is why the interfaces exist.

Two pieces sit outside the request/response flow:

- **`AutomaticSelectionBackgroundService`** — an `IHostedService` that periodically looks for transport requests whose bidding window has closed with automatic selection enabled, and resolves the winning bid.
- **`NotificationHub`** — a SignalR hub, mapped at `/notificationHub`, that pushes notifications to connected clients.

External services: **Stripe** for payments, **Cloudflare R2** (S3-compatible, via the Minio client) for image uploads, and **SMTP** for transactional email.

## Tech stack

| Concern | Choice |
|---|---|
| Framework | ASP.NET Core 8 (`net8.0`) |
| Database | MySQL 8, via EF Core + [Pomelo](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql) |
| Auth | JWT bearer tokens; BCrypt for password hashing |
| Real-time | SignalR |
| Payments | Stripe |
| Object storage | Cloudflare R2 (S3-compatible) |
| API docs | Swagger / Swashbuckle |
| Tests | xUnit + Moq, coverage via Coverlet |

## Running locally

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and a MySQL 8 server.

The quickest way to get a database up:

```bash
docker run -d --name bidgo-db -p 3306:3306 \
  -e MYSQL_ROOT_PASSWORD=root -e MYSQL_DATABASE=bidgo mysql:8.0
```

Then run the API:

```bash
cd Bid-Go_Backend
dotnet run
```

In `Development`, if no connection string is configured the app falls back to `server=localhost;database=bidgo;user=root;password=root`, which matches the container above. It applies any pending migrations on startup, so the schema is created for you on first run.

The API listens on the usual Kestrel ports and serves the Swagger UI at the root path (`/`).

You will still need a `Jwt__Key` for the app to start — see below.

## Configuration

**No secrets are committed to this repository.** `appsettings.json` contains the configuration *shape* with empty placeholder values; real values are supplied through environment variables at runtime. ASP.NET Core maps a `__` (double underscore) in an environment variable name to a `:` in configuration, so `Jwt__Key` populates `Jwt:Key`.

| Variable | Required | Purpose |
|---|---|---|
| `ConnectionStrings__default` | Yes (outside Development) | MySQL connection string |
| `Jwt__Key` | **Yes** | Signing key for JWTs. Use a long random value. |
| `Jwt__Issuer` | Yes | Token issuer, e.g. `BidGo` |
| `Jwt__Audience` | Yes | Token audience, e.g. `BidGoUsers` |
| `Jwt__ExpireMinutes` | No | Token lifetime; defaults to the value in `appsettings.json` |
| `Stripe__SecretKey` | For payments | Stripe secret key (use a **test** key for demos) |
| `Stripe__PublishableKey` | For payments | Stripe publishable key |
| `CloudflareR2__AccountId` | For uploads | Cloudflare R2 account ID |
| `CloudflareR2__AccessKeyId` | For uploads | R2 access key ID |
| `CloudflareR2__SecretAccessKey` | For uploads | R2 secret access key |
| `CloudflareR2__BucketName` | For uploads | R2 bucket name |
| `SmtpSettings__Host` | For email | SMTP server hostname |
| `SmtpSettings__Port` | For email | SMTP port, e.g. `587` |
| `SmtpSettings__User` | For email | SMTP username |
| `SmtpSettings__Pass` | For email | SMTP password |
| `PORT` | No | Listening port. Set automatically by most container hosts. |
| `RUN_MIGRATIONS` | No | Set to `false` to skip applying migrations on startup |

The app **fails fast at startup** with an explicit message if `ConnectionStrings__default` or `Jwt__Key` is missing, rather than failing later with an obscure error. The Stripe, R2, and SMTP variables are optional: the app starts without them, but the corresponding features will not work.

## Running with Docker

The Dockerfile lives at `Bid-Go_Backend/Dockerfile` but **copies from the repository root**, so the build context must be the root:

```bash
docker build -f Bid-Go_Backend/Dockerfile -t bidgo-backend .
```

```bash
docker run -p 8080:8080 \
  -e PORT=8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e "ConnectionStrings__default=server=<host>;port=3306;database=bidgo;user=<user>;password=<password>" \
  -e "Jwt__Key=<a-long-random-key>" \
  -e "Jwt__Issuer=BidGo" \
  -e "Jwt__Audience=BidGoUsers" \
  bidgo-backend
```

Then open <http://localhost:8080/> for the Swagger UI.

If your MySQL is a managed service that requires TLS (Aiven, PlanetScale, Azure), append `;SslMode=Required` to the connection string.

## Database and migrations

EF Core migrations live in `Bid-Go_Backend/Migrations/`. They are **applied automatically on startup**, so a brand-new empty database is brought up to the current schema the first time the app boots. Set `RUN_MIGRATIONS=false` to disable that, for instance if you would rather apply migrations as a separate deployment step.

To create a new migration after changing an entity:

```bash
cd Bid-Go_Backend
dotnet ef migrations add <Name>
```

The schema comprises `Users`, `TransportRequests`, `Bids`, `Payments`, `Chats`, `Messages`, `Notifications`, and `Reviews`.

## API overview

All routes are prefixed with `/api`. Interactive documentation for every endpoint, with request and response schemas, is served at the root path by Swagger — that is the authoritative reference; the table below is a map.

| Route prefix | Purpose | Access |
|---|---|---|
| `/api/auth` | Login, token issuing, current user | Anonymous to log in |
| `/api/register` | Company and driver registration | Anonymous |
| `/api/profile` | View and edit the current user's profile | Authenticated |
| `/api/transports` | Create/read/update transport requests; update transport status | Company to create; authenticated to read |
| `/api/pageTransports` | Paged and filtered transport request listings | Authenticated |
| `/api/bids` | Place, edit, cancel and list bids | Driver to bid |
| `/api/bids/manual` | Manually accept or reject a bid | Company |
| `/api/payments` | Stripe payment intents for the winning bid | Company |
| `/api/chats` | Conversations and messages between the two parties | Authenticated |
| `/api/notifications` | Fetch and mark notifications read | Authenticated |
| `/api/reviewRequest` | Leave and read reviews | Authenticated |
| `/api/history` | Past transports for the current user | Driver or Company |

Authorization is enforced with two policies, `DriverOnly` and `CompanyOnly`, which require a `userType` claim on the JWT.

To call a protected endpoint from Swagger: log in via `/api/auth`, copy the returned token, click **Authorize**, and enter `Bearer <token>`.

## Real-time notifications

The SignalR hub is mapped at `/notificationHub`. Clients connect to it to receive notifications (new bid received, bid accepted, transport status changed) without polling.

Because it relies on websockets, the API must be hosted somewhere that keeps a process alive and does not terminate long-lived connections. It also means **HTTPS redirection is deliberately disabled in the app**: the platform terminates TLS at its edge and forwards plain HTTP to the container, so redirecting inside the app causes loops and breaks the websocket handshake. `UseForwardedHeaders` is configured so the app still sees the original scheme and client IP.

## Tests

```bash
dotnet test
```

`Bid-Go.Tests/` contains xUnit tests for every controller and service, using Moq to stub the repository interfaces. Coverage is collected with Coverlet.

## Deployment

The app is a standard container: give it a MySQL connection string and a `PORT`, and it runs anywhere that can run a Docker image.

The live demo runs on:

- **Backend** — [Render](https://render.com) free web service, built from `Bid-Go_Backend/Dockerfile` with the repository root as the build context.
- **Database** — [Aiven](https://aiven.io) free MySQL. Requires TLS, so the connection string ends with `;SslMode=Required`.
- **Uploads** — Cloudflare R2.

Both free tiers idle out: the Render service spins down after 15 minutes without traffic and takes about a minute to wake, so the first request after a quiet period is slow. That is a cost of the free tier, not a bug.

Stripe runs in **test mode** on the demo. Use card `4242 4242 4242 4242` with any future expiry and any CVC — no real payment is taken.

## Security notes

This repository previously had credentials committed in `appsettings.json`. The history has been rewritten to remove them and **all affected credentials have been rotated**; the values that appear in the original upstream repository are dead. Configuration is now environment-driven, and `.gitignore` blocks the files that used to carry secrets.

Two things a reader should know about the original design, which are honest limitations of a university project rather than things to copy:

- CORS is configured with `AllowAnyOrigin`, which is fine for a public read-mostly demo API using bearer tokens, but is not what you would ship for a product.
- The Flutter client's integration tests connect **directly to the database** rather than going through this API.

## Credits

Bid&Go was built as a team project for the Software Development Laboratory course. The full commit history in this repository preserves everyone's contributions.

Originally developed under the [LDSGrupo04](https://github.com/LDSGrupo04) organization. This repository is a mirror maintained for demo and portfolio purposes.
