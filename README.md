# Smart Home Simulator

> Fullstackowa platforma symulacji IoT (Internet of Things)

## O Projekcie

**Smart Home Simulator** to aplikacja umożliwiająca zarządzanie inteligentnym domem. System pozwala użytkownikowi na dodawanie urządzeń, organizację ich w pokoje oraz monitorowanie stanu w czasie rzeczywistym.

Główne funkcjonalności:
* **Zarządzanie urządzeniami:** Dodawanie/usuwanie/edytowanie żarówek i czujników, sterowanie zasilaniem.
* **Symulacja danych:** Czujniki temperatury odbierają dane z symulatora przez protokół **MQTT**.
* **Real-time Monitoring:** Dashboard reaguje natychmiastowo na zmiany dzięki **WebSockets (SignalR)**.
* **Logi Serwisowe:** Historia napraw i konserwacji dla każdego urządzenia.
* **Organizacja:** Grupowanie urządzeń w Pokoje (Rooms).

---

## Technologie

### Backend (API & Services)
C#, ASP.NET Core 10 Web API, Entity Framework Core 10, SQLite, MQTTnet, Serilog

### Frontend (Client)
React, TypeScript, Vite, Tailwind CSS, SignalR

---

## Raport Realizacji Wymagań (Kryteria Oceniania)

Poniżej znajduje się szczegółowe zestawienie zaimplementowanych funkcjonalności w odniesieniu do punktacji projektu.

### 1. HTTP (REST API) - 6 pkt

Aplikacja realizuje pełny CRUD na 4 różnych zasobach.

#### 🟢 Zasoby i Endpointy (CRUD):
| Zasób | Metoda | Endpoint | Opis |
| :--- | :--- | :--- | :--- |
| **Devices** | `POST` | `/api/devices/lightbulb` | Dodanie urządzenia |
| | `GET` | `/api/devices` | Pobranie listy (z filtrowaniem) |
| | `PUT` | `/api/devices/{id}/turn-on` | Zmiana stanu (włącz/wyłącz) |
| | `DELETE` | `/api/devices/{id}` | Usunięcie urządzenia |
| **Users** | `POST` | `/api/users/register` | Rejestracja użytkownika |
| | `GET` | `/api/users/me` | Pobranie aktualnego użytkownika z ciasteczek |
| | `PUT` | `/api/users/{id}` | Aktualizacja danych/hasła |
| | `DELETE` | `/api/users/{id}` | Usunięcie konta |
| **Rooms** | `POST` | `/api/rooms` | Utworzenie pokoju |
| | `GET` | `/api/rooms` | Pobranie pokoi użytkownika |
| | `PUT` | `/api/rooms/{id}` | Zmiana nazwy pokoju |
| | `DELETE` | `/api/rooms/{id}` | Usunięcie pokoju |
| **Logs** | `POST` | `/api/logs` | Dodanie wpisu serwisowego |
| | `GET` | `/api/logs/{deviceId}` | Pobranie historii napraw |
| | `PUT` | `/api/logs/{id}` | Edycja wpisu |
| | `DELETE` | `/api/logs/{id}` | Usunięcie wpisu |

#### 🟢 Dodatkowe wymagania HTTP:
* [x] **Wyszukiwanie wg wzorca:** Parametr `?search=query` w `GET /api/devices`. Filtrowanie po stronie bazy danych (`LIKE`).
* [x] **Logowanie/Wylogowanie (Auth):** Oparte na **ciasteczkach HttpOnly**. Weryfikacja sesji w każdym requeście (`GetCurrentUserId()`).
* [x] **Klient SPA:** Aplikacja React obsługująca wszystkie powyższe endpointy.

### 2. Protokoły: MQTT, WS, SSE - 6 pkt

* [x] **Backend MQTT (3 pkt):**
    * **Biblioteka:** `MQTTnet`.
    * **Implementacja:** `MqttListenerService` działający jako `BackgroundService`.
    * **Działanie:** Nasłuchuje na temat `smarthome/devices/+/temp`, parsuje JSON i aktualizuje stan w bazie danych.
    * **Symulator:** Dodatkowa aplikacja konsolowa publikująca losowe odczyty co 5 sekund.
* [x] **Frontend WebSockets (3 pkt):**
    * **Technologia:** SignalR (`@microsoft/signalr`).
    * **Hub:** `SmartHomeHub`.
    * **Działanie:** Dwukierunkowa komunikacja. Serwer wysyła zdarzenia `RefreshDevices` oraz `ReceiveTemperature`, frontend automatycznie odświeża widok bez przeładowania strony.

### 3. Inne Funkcjonalności - 6 pkt

W projekcie zaimplementowano 6 dodatkowych, zaawansowanych mechanizmów:

1.  **TCP Socket Server:**
    * Alternatywny interfejs sterowania. Nasłuchuje na porcie `9000`.
    * Obsługuje surowe komendy tekstowe: `LOGIN`, `LIST`, `TOGGLE`.
    * Implementacja: `TcpSmartHomeServer.cs`.
2.  **Bezpieczeństwo (Cookies):**
    * Wykorzystanie ciasteczek z flagami `HttpOnly`, `Secure`, `SameSite=Strict`.
    * TTL ustawione na 7 dni.
3.  **Baza Danych (EF Core & SQLite):**
    * Zastosowanie wzorca **TPH (Table Per Hierarchy)** do dziedziczenia urządzeń (`Device` -> `LightBulb`, `Sensor`).
    * Unikalne indeksy na email użytkownika.
4.  **Szyfrowanie Haseł:**
    * Wykorzystanie algorytmu **BCrypt** (`BCrypt.Net-Next`).
    * Hashowanie przy rejestracji, bezpieczna weryfikacja przy logowaniu.
5.  **Logowanie Zdarzeń (Logging):**
    * Integracja z **Serilog**.
    * Zapis logów aplikacyjnych do plików tekstowych w folderze `/logs` (rotacja dzienna).
6.  **Czysta Architektura (Clean Architecture):**
    * Pełna separacja warstw: `Domain` (Core), `Infrastructure` (DB/Repositories), `Api` (Controllers).
    * Zastosowanie **Dependency Injection** (DI Container).

### 4. Aplikacja - 2 pkt

* [x] **Jakość kodu:** TypeScript na frontendzie, C# na backendzie.
* [x] **Obsługa błędów:** Bloki `try-catch` w kontrolerach, globalne powiadomienia o błędach na frontendzie (`showError`).
* [x] **Responsywność:** UI wykonany w **Tailwind CSS v4**, w pełni responsywny (Mobile/Desktop).

---

## Uruchomienie (każdy proces w 3 oddzielnych terminalach)
### Backend
Wymagane: .NET SDK
```bash
cd smart-home-simulator/backend/src/SmartHome.Api
dotnet restore
dotnet run
```
Serwer API ruszy na https://localhost:5187.

### Frontend
Wymagane: Node.js
```bash
cd smart-home-simulator/frontend
npm install
npm run dev
```
Aplikacja dostępna pod http://localhost:5173.

### Symulator MQTT (Opcjonalnie)
```bash
cd smart-home-simulator/backend/src/SmartHome.Simulator
dotnet run
```
Publikuje temperaturę termometrów.

## TCP Sever (do tego musi być włączony Backend)
pobierz aplikację putty
- W HostName (or IP address) wpisz localhost lub 127.0.0.1
- Ustaw Port na 9000,
- Connection Type ustaw na Raw,
- Naciśnij Open.
