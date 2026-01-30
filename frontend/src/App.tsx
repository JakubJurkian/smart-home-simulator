import { useState, useEffect, useCallback } from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

// CONFIG & TYPES
import type { User, Device, Room } from "./types";

// SERVICES
import { api } from "./services/api";

// COMPONENTS
import AuthForm from "./components/auth/AuthForm";
import UserProfile from "./components/user/UserProfile";
import DeviceCard from "./components/devices/DeviceCard";
import DeviceForm from "./components/devices/DeviceForm";
import RoomManager from "./components/rooms/RoomManager";
import MaintenanceModal from "./components/modals/MaintenanceModal";

function App() {
  // --- STATE ---
  const [user, setUser] = useState<User | null>(null);
  const [devices, setDevices] = useState<Device[]>([]);
  const [rooms, setRooms] = useState<Room[]>([]);
  const [temps, setTemps] = useState<Record<string, number>>({});

  const [searchTerm, setSearchTerm] = useState("");

  // UI State
  const [actionError, setActionError] = useState<string | null>(null);
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [selectedDeviceForLogs, setSelectedDeviceForLogs] =
    useState<Device | null>(null);
  const [view, setView] = useState<"dashboard" | "profile">("dashboard");

  // Derived
  const lightbulbs = devices.filter((d) => d.type === "LightBulb");
  const sensors = devices.filter((d) => d.type === "TemperatureSensor");

  const showError = (message: string) => {
    setActionError(message);
    setTimeout(() => setActionError(null), 3000);
  };

  // --- API CALLS ---

  // Accepts optional search string
  const fetchDevices = useCallback((search: string = "") => {
    api.devices
      .getAll(search)
      .then((res) => {
        if (res.status === 401) {
          setUser(null);
          throw new Error("Session expired.");
        }
        if (!res.ok) throw new Error("Failed to fetch devices");
        return res.json();
      })
      .then((data) => {
        setDevices(Array.isArray(data) ? data : []);
        setGlobalError(null);
      })
      .catch((err) => {
        console.error("Fetch failed:", err);
        setGlobalError(
          err.message === "Failed to fetch"
            ? "🔌 Connection to server lost."
            : err.message,
        );
      });
  }, []);

  const fetchRooms = useCallback(() => {
    api.rooms
      .getAll()
      .then((res) => res.json())
      .then((data) => setRooms(Array.isArray(data) ? data : []))
      .catch((err) => console.error("Failed to fetch rooms", err));
  }, []);

  // --- EFFECTS ---
  useEffect(() => {
    if (user && view === "dashboard") {
      fetchDevices(); // Initial fetch (empty search)
      fetchRooms();
    }
  }, [user, view, fetchDevices, fetchRooms]);

  // SignalR Logic
  useEffect(() => {
    if (!user) return;

    const connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5187/smarthomehub")
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    connection
      .start()
      .then(() => console.log("✅ Connected to SignalR Hub"))
      .catch((err) => console.error("❌ SignalR Connection Error:", err));

    connection.on("RefreshDevices", () => fetchDevices(searchTerm));

    connection.on("ReceiveTemperature", (deviceId: string, newTemp: number) => {
      setTemps((prev) => ({ ...prev, [deviceId]: newTemp }));
    });

    return () => {
      connection.stop();
    };
  }, [user, fetchDevices, searchTerm]); // Added searchTerm dependency for SignalR refresh context

  // --- HANDLERS ---

  const handleLoginSuccess = (userData: User) => setUser(userData);

  const handleLogout = () => {
    api.auth.logout().catch(console.error);
    setUser(null);
    setDevices([]);
    setGlobalError(null);
    setActionError(null);
    setSearchTerm("");
  };

  // Search Handler
  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setSearchTerm(value);
    fetchDevices(value); // Live search trigger
  };

  const handleAddDevice = (name: string, roomId: string, type: string) => {
    api.devices
      .add(type, { name, roomId })
      .then((res) => {
        if (!res.ok) throw new Error("Failed to add device.");
        fetchDevices(searchTerm); // Refresh preserving search
      })
      .catch((err) => showError(err.message));
  };

  const handleToggleDevice = (id: string, action: "turn-on" | "turn-off") => {
    api.devices
      .toggle(id, action)
      .then((res) => {
        if (!res.ok) throw new Error(`Could not ${action.replace("-", " ")}.`);
      })
      .catch((err) => showError(err.message));
  };

  const handleDeleteDevice = (id: string) => {
    if (confirm("Delete this device?")) {
      api.devices
        .delete(id)
        .then((res) => {
          if (!res.ok) throw new Error("Could not delete device.");
          fetchDevices(searchTerm);
        })
        .catch((err) => showError(err.message));
    }
  };

  const handleAddRoom = (name: string) => {
    api.rooms
      .add(name)
      .then((res) => {
        if (res.ok) fetchRooms();
        else alert("Failed to add room");
      })
      .catch(() => alert("Error adding room"));
  };

  const handleRenameRoom = (id: string, newName: string) => {
    api.rooms
      .rename(id, newName)
      .then((res) => {
        if (res.ok) {
          fetchRooms();
          fetchDevices(searchTerm);
        } else alert("Failed to rename room");
      })
      .catch(() => alert("Error renaming room"));
  };

  const handleDeleteRoom = (id: string) => {
    if (
      !confirm(
        "WARNING: Deleting this room will also DELETE ALL DEVICES inside it.\n\nAre you sure?",
      )
    )
      return;

    api.rooms
      .delete(id)
      .then(() => {
        fetchRooms();
        fetchDevices(searchTerm);
      })
      .catch(() => alert("Error deleting room"));
  };

  // --- RENDER ---

  if (!user) return <AuthForm onLoginSuccess={handleLoginSuccess} />;

  return (
    <div className="min-h-screen bg-gray-50 p-4 sm:p-8 font-sans text-gray-800 relative">
      {/* ERROR TOAST */}
      {actionError && (
        <div className="fixed bottom-4 left-4 right-4 sm:left-auto sm:right-6 sm:bottom-6 sm:w-auto bg-red-600 text-white px-6 py-4 rounded-lg shadow-2xl flex items-center justify-between gap-4 z-50 animate-bounce">
          <span className="font-medium">{actionError}</span>
          <button
            onClick={() => setActionError(null)}
            className="ml-4 hover:text-gray-200 font-bold cursor-pointer"
          >
            ✕
          </button>
        </div>
      )}

      <div className="max-w-5xl mx-auto">
        {/* HEADER */}
        <div className="flex flex-col sm:flex-row justify-between items-center mb-8 gap-4 sm:gap-0 bg-white p-4 rounded-xl shadow-sm border border-gray-100">
          <h1 className="text-2xl sm:text-3xl font-bold text-blue-600 flex items-center gap-2">
            🏠 Smart Home{" "}
            <span className="text-gray-400 text-lg font-normal">
              | {user.username}
            </span>
          </h1>

          <div className="flex gap-2 w-full sm:w-auto">
            <button
              onClick={() =>
                setView(view === "dashboard" ? "profile" : "dashboard")
              }
              className="px-4 py-2 bg-blue-50 text-blue-700 hover:bg-blue-100 rounded-lg font-medium transition cursor-pointer"
            >
              {view === "dashboard" ? "👤 Profile" : "🏠 Dashboard"}
            </button>
            <button
              onClick={handleLogout}
              className="px-4 py-2 bg-gray-200 hover:bg-gray-300 text-gray-700 rounded-lg font-medium transition cursor-pointer"
            >
              🚪 Logout
            </button>
          </div>
        </div>

        {/* GLOBAL ERROR */}
        {globalError && (
          <div className="bg-orange-100 border-l-4 border-orange-500 text-orange-700 p-4 mb-6">
            <p className="font-bold">System Warning</p>
            <p>{globalError}</p>
          </div>
        )}

        {/* VIEWS */}
        {view === "profile" ? (
          <UserProfile
            user={user}
            onBack={() => setView("dashboard")}
            onUpdateUser={setUser}
            onDeleteAccount={handleLogout}
          />
        ) : (
          <>
            {/* SEARCH BAR */}
            <div className="mb-8">
                <input
                    type="text"
                    placeholder="🔍 Search for devices (e.g. 'Kitchen Light')..."
                    value={searchTerm}
                    onChange={handleSearchChange}
                    className="w-full p-4 rounded-xl border border-gray-200 shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition"
                />
            </div>

            <RoomManager
              rooms={rooms}
              onAdd={handleAddRoom}
              onDelete={handleDeleteRoom}
              onRename={handleRenameRoom}
            />

            <DeviceForm onAdd={handleAddDevice} rooms={rooms} />

            {/* LIGHTS */}
            {lightbulbs.length > 0 && (
              <div className="mb-10 animate-fade-in-up">
                <h2 className="text-xl font-bold text-gray-700 mb-4 flex items-center gap-2 border-b pb-2">
                  💡 Lighting{" "}
                  <span className="text-sm font-normal text-gray-400">
                    ({lightbulbs.length})
                  </span>
                </h2>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
                  {lightbulbs.map((device) => (
                    <DeviceCard
                      key={device.id}
                      device={device}
                      onDelete={handleDeleteDevice}
                      onToggle={handleToggleDevice}
                      onOpenLogs={setSelectedDeviceForLogs}
                      temp={temps[device.id]}
                    />
                  ))}
                </div>
              </div>
            )}

            {/* SENSORS */}
            {sensors.length > 0 && (
              <div className="mb-10 animate-fade-in-up">
                <h2 className="text-xl font-bold text-gray-700 mb-4 flex items-center gap-2 border-b pb-2">
                  🌡️ Temperature Sensors{" "}
                  <span className="text-sm font-normal text-gray-400">
                    ({sensors.length})
                  </span>
                </h2>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
                  {sensors.map((device) => (
                    <DeviceCard
                      key={device.id}
                      device={device}
                      onDelete={handleDeleteDevice}
                      onToggle={handleToggleDevice}
                      onOpenLogs={setSelectedDeviceForLogs}
                      temp={temps[device.id]}
                    />
                  ))}
                </div>
              </div>
            )}

            {devices.length === 0 && !globalError && (
              <div className="text-center mt-10 p-10 bg-white rounded-xl border border-dashed border-gray-300">
                <p className="text-gray-500 text-lg">
                    {searchTerm ? "No devices match your search." : "No devices found."}
                </p>
                {!searchTerm && (
                    <p className="text-gray-400 text-sm">
                    Use the form above to add your first device.
                    </p>
                )}
              </div>
            )}
          </>
        )}
      </div>

      {/* MODAL */}
      {selectedDeviceForLogs && (
        <MaintenanceModal
          deviceId={selectedDeviceForLogs.id}
          deviceName={selectedDeviceForLogs.name}
          onClose={() => setSelectedDeviceForLogs(null)}
        />
      )}
    </div>
  );
}

export default App;