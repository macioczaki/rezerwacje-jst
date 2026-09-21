# Rezerwacje JST

[![backend](https://github.com/macioczakii/rezerwacje-jst/actions/workflows/backend.yml/badge.svg)](https://github.com/macioczakii/rezerwacje-jst/actions/workflows/backend.yml)
[![frontend](https://github.com/macioczakii/rezerwacje-jst/actions/workflows/frontend.yml/badge.svg)](https://github.com/macioczakii/rezerwacje-jst/actions/workflows/frontend.yml)
[![e2e](https://github.com/macioczakii/rezerwacje-jst/actions/workflows/e2e.yml/badge.svg)](https://github.com/macioczakii/rezerwacje-jst/actions/workflows/e2e.yml)

> **Demo online:** https://rezerwacje-jst.vercel.app
> **API (Swagger):** https://rezerwacje-jst.onrender.com/swagger
> **Konta testowe:**
> - `admin@example.com` / `Admin123!` (Admin)
> - `pracownik@example.com` / `Pracownik123!` (Employee)
>
> ⚠️ Aplikacja działa na darmowych tierach Render i Vercel. Pierwsze żądanie po dłuższej przerwie może potrwać 30–60 sekund (Render budzi uśpiony kontener).

Aplikacja fullstack do rezerwacji sal i urządzeń w urzędzie gminy. Projekt portfolio — **.NET 8 + React 19 + PostgreSQL**.

## Problem

Pracownicy urzędu rezerwują sale mailem i w Excelu. Dochodzi do kolizji (dwie osoby rezerwują tę samą salę w tym samym czasie), brakuje historii i widoku tygodniowego.

## Rozwiązanie

- Rezerwacja sal z **walidacją kolizji po stronie serwera**.
- Role: `Employee` (tworzy własne rezerwacje), `Admin` (zarządza salami, wszystkimi rezerwacjami i widzi audit log).
- Widok **dostępności sali** w danym dniu.
- **Reset hasła przez email** (Brevo API w produkcji, MailHog w dev).
- **Refresh tokeny** z rotacją i wykrywaniem reuse.
- **Audit log** — kto, kiedy i co zmienił w systemie.
- Soft delete — anulowane rezerwacje i nieaktywne sale nie znikają z historii.

## Funkcje

### Uwierzytelnianie i bezpieczeństwo
- Rejestracja i logowanie (JWT)
- Hasła hashowane BCrypt
- Refresh tokeny (access 15 min, refresh 7 dni) z rotacją
- Wykrywanie reuse — próba użycia starego refresh tokenu unieważnia wszystkie sesje użytkownika
- Auto-refresh w Axios — gdy access token wygaśnie, frontend sam go odświeża i powtarza żądanie (kolejkowanie równoległych żądań)
- Reset hasła przez email (jednorazowy token, ważny 30 minut)
- Autoryzacja oparta o role i właściciela zasobu

### Sale
- CRUD sal (admin)
- Soft delete — sala dezaktywowana, historia pozostaje
- Lista dla wszystkich zalogowanych, z filtrem aktywnych

### Rezerwacje
- CRUD rezerwacji z walidacją kolizji
- Przedziały półotwarte `[start, end)` — rezerwacje 10:00–11:00 i 11:00–12:00 nie kolidują
- Walidacja czasu: koniec > początek, nie w przeszłości, max 24 h
- Anulowanie (soft delete) przez właściciela lub admina
- Widok dostępności dziennej — zajęte przedziały

### Audit log
- Każda zmiana encji (`User`, `Room`, `Reservation`) trafia do `AuditLogs`
- Zapis: kto (email + ID), kiedy, co, jakie pola się zmieniły (JSON before/after)
- Endpoint `GET /api/AuditLog` z filtrami (typ encji, użytkownik, zakres dat)
- Strona dla admina z tabelą zdarzeń

## Stack

**Backend:**
- .NET 8, ASP.NET Core Web API
- Entity Framework Core 8 + PostgreSQL 16
- JWT (Bearer), BCrypt
- MailKit (SMTP) + Brevo REST API (produkcja)
- xUnit — testy jednostkowe

**Frontend:**
- React 19 + TypeScript + Vite
- Tailwind CSS 4
- TanStack Query + Axios
- React Router v6

**Testy:**
- xUnit — 53 testy jednostkowe (auth, sale, rezerwacje, kolizje)
- Playwright — 11 testów E2E (logowanie, CRUD sal, rezerwacje, uprawnienia)

**Infrastruktura:**
- Docker Compose (PostgreSQL + Adminer + MailHog)
- GitHub Actions — 3 workflowy (backend, frontend, e2e)
- Render (backend + baza), Vercel (frontend)
- Brevo (email w produkcji)

## Architektura

Clean Architecture w czterech projektach:

```
Rezerwacje.Domain          # encje, enumy (bez zależności)
Rezerwacje.Application     # DTO, interfejsy serwisów
Rezerwacje.Infrastructure  # EF Core, DbContext, serwisy, email
Rezerwacje.Api             # kontrolery, konfiguracja, DI
```

Zależności płyną jednokierunkowo: `Api → Infrastructure → Application → Domain`.

## Kluczowe decyzje

- **Walidacja kolizji po stronie serwera** — klient można oszukać, serwer jest jedynym źródłem prawdy.
- **Przedziały półotwarte `[start, end)`** — rezerwacje 10:00–11:00 i 11:00–12:00 nie kolidują.
- **JWT z claimami roli** — autoryzacja bezstanowa, łatwy deploy.
- **Refresh tokeny z rotacją i wykrywaniem reuse** — każdy refresh unieważnia stary token i tworzy nowy. Próba ponownego użycia starego unieważnia wszystkie aktywne sesje (obrona przed kradzieżą).
- **Auto-refresh w Axios** — równoległe żądania czekają na ten sam refresh (kolejkowanie).
- **Soft delete** — anulowana rezerwacja nie znika, tylko zmienia status, żeby historia była spójna.
- **Audit log przez EF interceptor** — zapis zmian automatyczny, na poziomie `SaveChanges`, bez ręcznego wołania w każdym serwisie.
- **Seed data w Development** — po pierwszym uruchomieniu baza ma przykładowe dane.
- **Email przez Brevo API** — Render (darmowy tier) blokuje wychodzące połączenia SMTP na porcie 587. Używamy REST API przez HTTPS.
- **Testy E2E w CI** — pełny stack (PostgreSQL + backend + frontend) uruchamiany przy każdym pushu.

## Uruchomienie lokalne

**Wymagania:** Docker Desktop, .NET 8 SDK, Node.js 20+, `dotnet-ef`.

1. Sklonuj repo i wejdź do katalogu:

```
git clone https://github.com/macioczakii/rezerwacje-jst.git
cd rezerwacje-jst
```

2. Uruchom bazę i MailHoga:

```
docker compose up -d
```

Kontenery:
- PostgreSQL na `localhost:5432`
- Adminer (UI do bazy) na http://localhost:8081
- MailHog (przechwytywanie maili w dev) na http://localhost:8025

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

## Testy

**Backend (unit):**

```
cd backend
dotnet test
```

53 testy — auth, refresh tokeny, CRUD sal, logika kolizji rezerwacji.

**Frontend (E2E):**

Upewnij się, że backend i frontend działają lokalnie, potem:

```
cd frontend
npm run test:e2e
```

11 testów Playwright — logowanie, CRUD sal, rezerwacje, uprawnienia.

Tryb UI do debugowania:

```
npm run test:e2e:ui
```

**CI:** trzy workflowy GitHub Actions uruchamiają się przy każdym pushu — `backend`, `frontend`, `e2e`.

## API — przykłady

Rejestracja:

```
curl -X POST https://rezerwacje-jst.onrender.com/api/Auth/register \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"test@example.com\",\"password\":\"Haslo123!\",\"firstName\":\"Jan\",\"lastName\":\"Kowalski\"}"
```

Logowanie:

```
curl -X POST https://rezerwacje-jst.onrender.com/api/Auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin@example.com\",\"password\":\"Admin123!\"}"
```

Lista sal:

```
curl https://rezerwacje-jst.onrender.com/api/Rooms
```

## Struktura

```
rezerwacje-jst/
├── backend/
│   ├── src/
│   │   ├── Rezerwacje.Domain/          # encje
│   │   ├── Rezerwacje.Application/     # DTO, interfejsy
│   │   ├── Rezerwacje.Infrastructure/  # EF Core, serwisy, email
│   │   └── Rezerwacje.Api/             # kontrolery, Program.cs
│   ├── tests/
│   │   └── Rezerwacje.UnitTests/       # xUnit
│   └── Dockerfile
├── frontend/
│   ├── src/
│   │   ├── api/                        # axios + moduły API
│   │   ├── auth/                       # kontekst autoryzacji
│   │   ├── components/                 # komponenty wielokrotnego użytku
│   │   ├── pages/                      # strony
│   │   └── lib/                        # helpery
│   ├── e2e/                            # testy Playwright
│   └── playwright.config.ts
├── .github/workflows/                  # CI: backend, frontend, e2e
└── docker-compose.yml
```

## Licencja

MIT