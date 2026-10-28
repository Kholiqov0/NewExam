# TicketApi

REST API for selling event tickets. ASP.NET Core Web API, Clean Architecture (Domain / Application / Infrastructure / API), EF Core + PostgreSQL, Identity + JWT, RabbitMQ, SMTP, background services, caching.

## Requirements

- .NET SDK 10 (projects target `net10.0`)
- PostgreSQL 14+
- RabbitMQ (default port `5672`, `guest` / `guest`)
- Redis (default port `6379`)
- SMTP account (for Gmail an app password is required)
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

## Configuration

All settings are in `API/appsettings.json`:

| Section | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string (database `ticketapi`) |
| `ConnectionStrings:Redis` | Redis connection string |
| `Jwt` | `Key` (32+ chars), `Issuer`, `Audience`, `ExpiresMinutes` |
| `Email` | `Host`, `Port`, `UseSsl`, `UserName`, `Password`, `From` |
| `RabbitMq` | `Host`, `Port`, `UserName`, `Password`, `QueueName` |
| `Booking` | `HoldMinutes` (default 10), `ExpiryCheckSeconds` (default 30) |

Before running, set your own values in the `Email` section (`UserName`, `Password`, `From`) and check the PostgreSQL password.

## Run order

1. Start PostgreSQL, RabbitMQ and Redis.
2. Apply migrations (the application does not apply them automatically). Run from the solution root:

```
dotnet ef database update -p Infrastructure -s API
```

3. Build and run:

```
dotnet build
dotnet run --project API
```

4. Open Swagger: `http://localhost:5193/swagger`

To create a new migration:

```
dotnet ef migrations add <Name> -p Infrastructure -s API -o Persistence/Migrations
```

## Admin account

In the Development environment `DbSeeder` creates the roles (`Admin`, `Organizer`, `User`) and the administrator:

- email: `admin@mail.com`
- password: `Admin123`

New users registered via `POST /api/auth/register` always get the `User` role. An administrator can change a role with `PUT /api/users/{id}/role` (`Organizer` or `User`); the new role is active after the next login.

## Authorization in Swagger

1. `POST /api/auth/login`, copy the `token` from the response.
2. Click **Authorize** and paste the token (without the word `Bearer`).

Dates must be sent in UTC with the `Z` suffix, for example `2026-12-20T18:00:00Z`.

## Cache

**Choice: `IDistributedCache` on Redis.**

Why: the cache lives outside the application process, so it survives restarts and stays consistent if several instances are running. The events catalog is read often and changes rarely, so it is a good fit.

How it works:
- Cached: `GET /api/events/{id}` and `GET /api/events`. Keys: `events:id:{id}` and `events:list:{version}:{all query parameters}`, so every parameter combination is a separate entry.
- Entries have a sliding expiration (2 minutes) and an absolute expiration (30 minutes).
- Invalidation: after an event is created or updated, and after the number of seats changes (booking created, cancelled, expired) the by-id key is removed and the list version key is replaced, so all cached lists become unreachable at once.
- Only DTOs are cached. Bookings are never cached. Seat availability for a booking is always checked in the database.
- Every cache access is logged: `Cache HIT key=...` or `Cache MISS key=...`.

## Emails and queue

Emails are never sent inside a request. `BookingService` saves the booking, then publishes `{ "bookingId": 15, "type": "Created" }` to RabbitMQ (`Created`, `Confirmed`, `Cancelled`, `Expired`). `BookingQueueConsumer` reads the queue and calls `BookingNotificationService`, which loads the booking, event and user and sends the email through SMTP. `ack` is sent after the email is sent; on error the message is logged and rejected with `nack` (`requeue: false`). If RabbitMQ is unavailable, the request still returns 200 and the error is logged.

## Background services

- `BookingQueueConsumer` reads the RabbitMQ queue.
- `BookingExpiryWorker` runs every `Booking:ExpiryCheckSeconds` seconds, moves expired `Pending` bookings to `Expired`, returns the seats and publishes the `Expired` message.

## What is implemented

- All 15 endpoints from the task: auth (register, login, me), users (list, change role), categories (list, create), events (list, get, create, update), bookings (create, my, pay, cancel).
- Clean Architecture with 4 projects, Result pattern, pagination and filtering, Identity + JWT, roles, RabbitMQ, SMTP, two background services, caching, EF Core migrations, Swagger with Authorize button.
- Additional endpoints (not required by the task): `POST /api/auth/forgot-password`, `POST /api/auth/verify-code`, `POST /api/auth/reset-password`. The reset code email is sent directly from the request.
