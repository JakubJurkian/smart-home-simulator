# Smart Home Simulator | IoT Management Platform

![Project Status](https://img.shields.io/badge/status-active-success.svg)
![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=black)

> **Smart Home Simulator** is a fullstack IoT (Internet of Things) simulation platform for managing smart home devices. It provides real-time monitoring, device control, and comprehensive service history tracking through a modern web interface.

---

## Screenshots

|                        Main Page                            |                         User Profile                            |
| :---------------------------------------------------------: | :-------------------------------------------------------------: |
| ![Main Page Placeholder](docs/images/landing-desktop.webp)   | ![User Profile Placeholder](docs/images/user-profile-desktop.webp)  |

|                     Main Mobile                          |                        User Profile Mobile                |
| :------------------------------------------------------: | :-------------------------------------------------------: |
| ![Main Mobile Placeholder](docs/images/landing-mobile.webp) | ![User Profile Mobile Placeholder](docs/images/user-profile-mobile.webp) |

---

## Key Features

### For Users

- **Secure Authentication:** Register/Login with HttpOnly cookies, BCrypt password hashing, and session persistence.
- **Device Management:** Add, rename, toggle, and delete smart devices (Light Bulbs, Temperature Sensors).
- **Room Organization:** Group devices into rooms for better organization and bulk operations.
- **Real-time Monitoring:** Live temperature updates via MQTT protocol and instant UI refresh through WebSockets.
- **Service History:** Maintain detailed maintenance logs for each device with full CRUD operations.
- **Live Search:** Filter devices instantly with pattern-based searching.

### Technical Highlights (Under the Hood)

- **Multi-Protocol Communication:**
  - **MQTT:** IoT simulator publishes temperature data to a broker; backend `MqttListenerService` subscribes and updates the database.
  - **WebSockets (SignalR):** Bidirectional real-time communication for instant UI updates (`RefreshDevices`, `ReceiveTemperature` events).
  - **TCP Socket Server:** Alternative raw text command interface on port `9000` (supports `LOGIN`, `LIST`, `TOGGLE` commands).

- **Clean Architecture:** Full separation of concerns with `Domain`, `Infrastructure`, and `Api` layers following SOLID principles.
- **Entity Framework Core:** TPH (Table Per Hierarchy) inheritance pattern for device polymorphism (`Device` → `LightBulb`, `TemperatureSensor`).
- **Background Services:** `BackgroundService` implementations for MQTT listener and TCP server running concurrently with the API.
- **Comprehensive Testing:** Unit tests (xUnit + Moq), Integration tests, BDD tests (Reqnroll/Gherkin), and Performance tests (NBomber) with >80% code coverage.
- **CI/CD Pipeline:** GitHub Actions workflow for automated testing on every push/PR.

---

## Tech Stack

**Backend:**

- ![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white) **ASP.NET Core 10 Web API**
- ![EF Core](https://img.shields.io/badge/EF_Core-10-512BD4?logo=dotnet&logoColor=white) **Entity Framework Core** (SQLite)
- ![MQTT](https://img.shields.io/badge/MQTT-MQTTnet-660066) **MQTTnet** (IoT Communication)
- ![SignalR](https://img.shields.io/badge/SignalR-WebSockets-512BD4) **SignalR** (Real-time Hub)
- ![Serilog](https://img.shields.io/badge/Serilog-Logging-red) **Serilog** (Structured Logging)

**Frontend:**

- ![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=black) **React 18** (Vite)
- ![TypeScript](https://img.shields.io/badge/TypeScript-5.0-3178C6?logo=typescript&logoColor=white) **TypeScript**
- ![Tailwind](https://img.shields.io/badge/Tailwind_CSS-4.0-06B6D4?logo=tailwindcss&logoColor=white) **Tailwind CSS v4**
- ![SignalR](https://img.shields.io/badge/@microsoft/signalr-Client-512BD4) **SignalR Client**

**Testing & DevOps:**

- ![xUnit](https://img.shields.io/badge/xUnit-Testing-5C2D91) **xUnit + Moq** (Unit Tests)
- ![Reqnroll](https://img.shields.io/badge/Reqnroll-BDD-green) **Reqnroll** (BDD/Gherkin)
- ![NBomber](https://img.shields.io/badge/NBomber-Performance-orange) **NBomber** (Load Testing)
- ![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-CI/CD-2088FF?logo=githubactions&logoColor=white) **GitHub Actions**

---

## Project Architecture

The project follows **Clean Architecture** principles with a modular, scalable structure.

```bash
smart-home-simulator/
├── backend/
│   └── src/
│       ├── SmartHome.Api/             # Controllers, Hubs, DTOs, Background Services
│       │   ├── Controllers/           # REST API endpoints
│       │   ├── Hubs/                  # SignalR WebSocket hub
│       │   ├── BackgroundServices/    # MQTT Listener, TCP Server
│       │   └── Dtos/                  # Data Transfer Objects
│       ├── SmartHome.Domain/          # Core business logic & entities
│       │   ├── Entities/              # Device, Room, User, MaintenanceLog
│       │   └── Interfaces/            # Service & Repository contracts
│       ├── SmartHome.Infrastructure/  # Data access & external services
│       │   ├── Persistence/           # EF Core DbContext
│       │   ├── Repositories/          # Data access implementations
│       │   └── Services/              # Business logic implementations
│       └── SmartHome.Simulator/       # IoT MQTT Publisher (Console App)
├── frontend/
│   └── src/
│       ├── components/                # Reusable UI components
│       │   ├── auth/                  # AuthForm
│       │   ├── devices/               # DeviceCard, DeviceForm
│       │   ├── rooms/                 # RoomManager
│       │   ├── modals/                # MaintenanceModal
│       │   └── user/                  # UserProfile
│       ├── services/                  # API client wrapper
│       └── types.ts                   # TypeScript interfaces
├── tests/
│   ├── SmartHome.UnitTests/           # Unit tests with Moq
│   ├── SmartHome.IntegrationTests/    # API integration tests
│   ├── SmartHome.BDDTests/            # Gherkin/Reqnroll scenarios
│   └── SmartHome.PerformanceTests/    # NBomber load tests
└── .github/
    └── workflows/                     # CI/CD pipeline
```

## API Endpoints

### Devices

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| **POST** | `/api/devices/lightbulb` | Create a new light bulb |
| **POST** | `/api/devices/temperaturesensor` | Create a new sensor |
| **GET** | `/api/devices?search=query` | List devices (with filtering) |
| **PUT** | `/api/devices/{id}/turn-on` | Toggle device state |
| **DELETE** | `/api/devices/{id}` | Delete a device |

### Rooms

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| **POST** | `/api/rooms` | Create a room |
| **GET** | `/api/rooms` | List user's rooms |
| **PUT** | `/api/rooms/{id}` | Rename a room |
| **DELETE** | `/api/rooms/{id}` | Delete room (cascades to devices) |

### Users

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| **POST** | `/api/users/register` | Register new user |
| **POST** | `/api/users/login` | Login (sets HttpOnly cookie) |
| **GET** | `/api/users/me` | Get current session |
| **DELETE** | `/api/users/{id}` | Delete account |

### Logs

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| **POST** | `/api/logs` | Add maintenance entry |
| **GET** | `/api/logs/{deviceId}` | Get device service history |
| **PUT** | `/api/logs/{id}` | Update log entry |
| **DELETE** | `/api/logs/{id}` | Delete log entry |

---

## Getting Started

### Prerequisites

* .NET SDK 10 (or higher)
* Node.js (v18 or higher)
* npm or yarn

### Installation

**1. Clone the repository**

    git clone https://github.com/JakubJurkian/smart-home-simulator.git
    cd smart-home-simulator

**2. Start the Backend API**

    cd backend/src/SmartHome.Api
    dotnet restore
    dotnet run

> API will be available at `http://localhost:5187`

**3. Start the Frontend** (New terminal)

    cd frontend
    npm install
    npm run dev

> App will be available at `http://localhost:5173`

**4. Start the IoT Simulator** (Optional, new terminal)
Publishes random temperature readings every 5 seconds.

    cd backend/src/SmartHome.Simulator
    dotnet run

### TCP Server (Alternative Interface)

With the backend running, connect via PuTTY or any raw TCP client:

* **Host:** `localhost` or `127.0.0.1`
* **Port:** `9000`
* **Connection Type:** Raw

**Available commands:** `LOGIN <email> <password>`, `LIST`, `TOGGLE <deviceId>`

---

## Running Tests

**All tests**

    dotnet test

**Unit tests only**

    dotnet test SmartHome.UnitTests.csproj

**Integration tests**

    dotnet test SmartHome.IntegrationTests.csproj

**BDD tests**

    dotnet test SmartHome.BDDTests.csproj

**Performance tests**

    dotnet run --project SmartHome.PerformanceTests.csproj

### Code Coverage Report

    dotnet test --collect:"XPlat Code Coverage"

    reportgenerator \
      -reports:"**/coverage.cobertura.xml" \
      -targetdir:"coveragereport" \
      -reporttypes:Html

> Report available at: `index.html`

---

## Best Practices Implemented

* **Clean Architecture:** Strict separation between `Domain` (business rules), `Infrastructure` (data access), and `Api` (presentation) layers.
* **Dependency Injection:** All services and repositories registered in DI container for testability and loose coupling.
* **Repository Pattern:** Abstracts data access behind interfaces, enabling easy mocking in tests.
* **TPH Inheritance:** Entity Framework's Table Per Hierarchy for polymorphic device types without complex joins.
* **Background Services:** Long-running MQTT and TCP listeners implemented as hosted services with proper cancellation token handling.
* **Secure Authentication:** HttpOnly cookies with `Secure`, `SameSite=Strict` flags and BCrypt password hashing.
* **Comprehensive Logging:** Serilog with daily file rotation for production debugging.
* **Responsive UI:** Mobile-first design with Tailwind CSS breakpoints.

## License

Distributed under the [MIT License](LICENSE). See `LICENSE` for more information.
