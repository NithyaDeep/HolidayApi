# Holiday API

A .NET 8 Web API that fetches, stores and queries public holiday data from [Nager.Date](https://date.nager.at/Api).

[![CI](https://github.com/NithyaDeep/HolidayApi/actions/workflows/ci.yml/badge.svg)](https://github.com/NithyaDeep/HolidayApi/actions/workflows/ci.yml)

---

## Tech Stack

- .NET 8 · ASP.NET Core · Entity Framework Core 8 · SQL Server
- Clean Architecture · Repository Pattern · xUnit · NSubstitute
- Serilog · Swagger · GitHub Actions CI

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server or LocalDB
- EF Core CLI: `dotnet tool install --global dotnet-ef`

---

## Getting Started

**1. Clone and restore**
```bash
git clone https://github.com/NithyaDeep/HolidayApi.git
cd HolidayApi
dotnet restore
```

**2. Update connection string**

Edit `src/HolidayApi.API/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HolidayApiDb;Trusted_Connection=True;"
}
```

**3. Run migrations**
```bash
dotnet ef database update \
  --project src/HolidayApi.Infrastructure \
  --startup-project src/HolidayApi.API
```

**4. Run the app**
```bash
dotnet run --project src/HolidayApi.API
```

Swagger opens at `https://localhost:{PORT}` automatically.

---

## Running Tests

```bash
dotnet test
```

All tests run with **no database required** — business logic is tested in isolation using mocks.

---

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/holidays/fetch` | Fetch holidays from Nager API and save to DB |
| GET | `/api/holidays/last-celebrated` | Last 3 celebrated holidays for a country |
| GET | `/api/holidays/weekday-counts` | Holiday counts excluding weekends, sorted descending |
| GET | `/api/holidays/shared` | Shared holiday dates between two countries |

**Supported country codes:** NL, BE, DE, GB, US, FR, AU, CA, JP, SG

---

## Architecture

Clean Architecture — dependencies point inward only.