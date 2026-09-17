# Rezerwacje JST

Aplikacja do rezerwacji sal i urządzeń w urzędzie gminy. Projekt portfolio — pełny stack **.NET 8 + React + PostgreSQL**.

## Problem

Pracownicy urzędu rezerwują sale mailem i w Excelu. Dochodzi do kolizji (dwie osoby rezerwują tę samą salę w tym samym czasie), brakuje historii i widoku tygodniowego.

## Rozwiązanie

- Rezerwacja sal z walidacją kolizji **po stronie serwera**.
- Role: `Employee` (tworzy własne rezerwacje), `Admin` (zarządza salami i wszystkimi rezerwacjami).
- Widok dostępności sali w danym dniu.
- Soft delete — anulowane rezerwacje i nieaktywne sale nie znikają z historii.

## Stack

**Backend:**
- .NET 8, ASP.NET Core Web API
- Entity Framework Core 8 + PostgreSQL 16
- JWT (Bearer), BCrypt do hashowania haseł
- FluentValidation (walidacja), Swagger/OpenAPI
- xUnit — testy jednostkowe

**Frontend:**
- React 19 + TypeScript + Vite
- Tailwind CSS, TanStack Query, Axios

**Infrastruktura:**
- Docker (PostgreSQL + Adminer)
- GitHub Actions (CI — build + testy)

## Architektura

Clean Architecture w czterech projektach:

```
Rezerwacje.Domain          # encje, enumy (bez zależności)
Rezerwacje.Application     # DTO, interfejsy serwisów
Rezerwacje.Infrastructure  # EF Core, DbContext, serwisy
Rezerwacje.Api             # kontrolery, konfiguracja, DI
```

Zależności płyną jednokierunkowo: `Api → Infrastructure → Application → Domain`.

## Kluczowe decyzje

- **Walidacja kolizji po stronie serwera** — klient można oszukać, serwer jest jedynym źródłem prawdy.
- **Przedziały półotwarte `[start, end)`** — rezerwacje 10:00–11:00 i 11:00–12:00 nie kolidują.
- **JWT z claimami roli** — autoryzacja bezstanowa, łatwy deploy.
- **Soft delete** — anulowana rezerwacja nie znika, tylko zmienia status, żeby historia była spójna.
- **Seed data w Development** — po pierwszym uruchomieniu baza ma przykładowe dane.

## Uruchomienie lokalne

**Wymagania:** Docker Desktop, .NET 8 SDK, Node.js 20+, `dotnet-ef`.

1. Sklonuj repo i wejdź do katalogu:

```
git clone https://github.com/macioczaki/rezerwacje-jst.git
cd rezerwacje-jst
```

2. Uruchom bazę:

```
docker compose up -d
```

3. Backend:

```
cd backend
dotnet build
dotnet run --project src/Rezerwacje.Api
```

Baza zaseeduje się automatycznie. Swagger: http://localhost:5045/swagger

4. Frontend (w drugim terminalu):

```
cd frontend
npm install
npm run dev
```

Aplikacja: http://localhost:5173

## Konta testowe

Po pierwszym uruchomieniu baza zawiera:

| Email | Hasło | Rola |
|---|---|---|
| `admin@example.com` | `Admin123!` | Admin |
| `pracownik@example.com` | `Pracownik123!` | Employee |

## API — przykłady

Rejestracja:

```
curl -X POST http://localhost:5045/api/Auth/register -H "Content-Type: application/json" -d "{\"email\":\"test@example.com\",\"password\":\"Haslo123!\",\"firstName\":\"Jan\",\"lastName\":\"Kowalski\"}"
```

Logowanie:

```
curl -X POST http://localhost:5045/api/Auth/login -H "Content-Type: application/json" -d "{\"email\":\"admin@example.com\",\"password\":\"Admin123!\"}"
```

Lista sal (publiczne):

```
curl http://localhost:5045/api/Rooms
```

Tworzenie rezerwacji (wymaga tokenu):

```
curl -X POST http://localhost:5045/api/Reservations -H "Authorization: Bearer TOKEN" -H "Content-Type: application/json" -d "{\"roomId\":\"UUID\",\"title\":\"Spotkanie\",\"startTime\":\"2026-09-20T10:00:00Z\",\"endTime\":\"2026-09-20T11:00:00Z\"}"
```

## Testy

```
cd backend
dotnet test
```

Aktualnie **42 testy jednostkowe** pokrywające auth, CRUD sal i logikę kolizji rezerwacji.

## Status

W budowie:

- [x] Backend: auth, sale, rezerwacje, kolizje
- [x] Testy jednostkowe + CI
- [ ] Frontend React
- [ ] Deploy

## Licencja

MIT