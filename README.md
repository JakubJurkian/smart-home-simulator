# Smart Home Simulator - Testy

### Autor: Jakub Jurkian

### Grupa: 2

---

## Opis projektu

Projekt Smart Home Simulator to kompleksowy system zarządzania inteligentnym domem z interfejsem webowym.
Obejmuje backend w .NET, frontend w React/TypeScript oraz komunikację MQTT.
System posiada logikę użytkownika oraz umożliwia zarządzanie urządzeniami, pomieszczeniami i logami konserwacji.

---

## ✅ Spełnione wymagania projektowe

### 1. Minimalny zakres funkcjonalny aplikacji

| Wymaganie | Status | Lokalizacja | Opis |
|-----------|--------|-------------|------|
| **Co najmniej 6 funkcjonalności z logiką warunkową i walidacją** | ✅ | `backend/src/SmartHome.Domain/`, `MqttListenerService.cs`, `backend/src/SmartHome.Infrastructure/` | Zarządzanie urządzeniami, pomieszczeniami, użytkownikami, logami konserwacji, automatyzacjami, komunikacja MQTT |
| **Co najmniej 3 klasy współpracujące** | ✅ | `backend/src/SmartHome.Domain/Entities/` | Device, Room, User, MaintenanceLog, Automation - encje współpracujące przez serwisy aplikacyjne |
| **Funkcjonalność z historią/rejestrem danych** | ✅ | `backend/src/SmartHome.Domain/Entities/MaintenanceLog.cs` | Rejestr logów konserwacji urządzeń |
| **Funkcjonalność zależna od danych użytkownika** | ✅ | `backend/src/SmartHome.Infrastructure/Services/` | Walidacja uprawnień, autoryzacja operacji na podstawie roli użytkownika |
| **API z pełnym CRUD** | ✅ | `src/SmartHome.Api/Controllers/` | DevicesController, RoomsController, UsersController, MaintenanceLogsController |
| **Funkcjonalność zewnętrzna do mockowania** | ✅ | `src/SmartHome.Infrastructure/` | baza danych przez Entity Framework |

### 2. Wymagania techniczne

| Wymaganie | Status | Lokalizacja | Opis |
|-----------|--------|-------------|------|
| **Kod oddzielony od testów** | ✅ | `tests/` | Struktura katalogów rozdzielająca kod od testów |
| **Sensowna struktura i nazewnictwo** | ✅ | Cały projekt | Architektura Clean Architecture z podziałem na Domain, Application, Infrastructure, Api |
| **Dobre praktyki (SOLID, DRY)** | ✅ | `backend/src` | Dependency Injection, separacja warstw, interfejsy dla serwisów |

### 3. Wymagania dotyczące testów

| Typ testów | Status | Lokalizacja | Opis |
|------------|--------|-------------|------|
| **Testy jednostkowe** | ✅ | `tests/SmartHome.UnitTests/` | Testy logiki biznesowej z użyciem mocków |
| **Testy API (integracyjne)** | ✅ | `tests/SmartHome.IntegrationTests/` | Testy endpointów HTTP |
| **Testy BDD** | ✅ | `tests/SmartHome.BDDTests/` | Scenariusze Gherkin z użyciem Reqnroll |
| **Testy wydajnościowe** | ✅ | `tests/SmartHome.PerformanceTests/` | Testy obciążeniowe endpointów |
| **Code coverage >80%** | ✅ | `coveragereport/` | Raport pokrycia generowany przez Coverlet i ReportGenerator |

### 4. CI/CD Pipeline

| Wymaganie | Status | Lokalizacja | Opis |
|-----------|--------|-------------|------|
| **Pipeline CI** | ✅ | `.github/workflows/` | GitHub Actions uruchamiany przy push/PR do main |
| **Zielone pipeline'y dla wszystkich typów testów** | ✅ | GitHub Actions | Automatyczne uruchamianie wszystkich testów |

---

## Technologie

- **Backend:** .NET 10, ASP.NET Core Web API
- **Frontend:** React, TypeScript
- **Baza danych:** Entity Framework Core
- **Komunikacja IoT:** MQTT
- **Testy jednostkowe:** xUnit, Moq
- **Testy BDD:** Reqnroll (Gherkin)
- **Testy wydajnościowe:** NBomber
- **CI/CD:** GitHub Actions
- **Code Coverage:** Coverlet, ReportGenerator

---

## Uruchomienie testów

### Testy jednostkowe

```bash
dotnet test tests/SmartHome.UnitTests/SmartHome.UnitTests.csproj
```

### Testy integracyjne (API)

```bash
dotnet test tests/SmartHome.IntegrationTests/SmartHome.IntegrationTests.csproj
```

### Testy BDD (Reqnroll)

```bash
dotnet test tests/SmartHome.BDDTests/SmartHome.BDDTests.csproj
```

### Testy wydajnościowe

```bash
dotnet run --project tests/SmartHome.PerformanceTests/SmartHome.PerformanceTests.csproj
```

### Wszystkie testy

```bash
dotnet test
```

### Raport pokrycia kodu (Code Coverage)

```bash
dotnet test --collect:"XPlat Code Coverage"

reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html -classfilters:"-SmartHome.Api.BackgroundServices.TcpSmartHomeServer;-SmartHome.Infrastructure.Migrations.*"
```

Po uruchomieniu raport dostępny w: `coveragereport/index.html`

---

## Pipeline CI/CD

Pipeline GitHub Actions uruchamia się automatycznie przy każdym push i pull request do gałęzi `main`.

### Ręczne uruchomienie pipeline

1. Przejdź do zakładki **Actions** w repozytorium GitHub
2. Wybierz workflow **".NET CI"**
3. Kliknij **Run workflow**

### Lokalne uruchomienie z Docker Compose

```bash
docker-compose up -d
```
