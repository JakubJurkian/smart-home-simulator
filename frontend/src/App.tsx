import { useState, useEffect, useCallback } from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

// --- CONFIG & TYPES ---
const API_URL = "http://localhost:5187/api";

export interface Device {
  id: string;
  name: string;
  room: string;
  type: string;
  isOn?: boolean;
  currentTemperature?: number;
}

export interface User {
  id: string;
  username: string;
  email: string;
}

export interface DeviceCardProps {
  device: Device;
  onDelete: (id: string) => void;
  onToggle: (id: string, action: "turn-on" | "turn-off") => void;
  temp?: number;
}

// --- COMPONENTS ---

const UserProfile = ({
  user,
  onBack,
  onUpdateUser,
}: {
  user: User;
  onBack: () => void;
  onUpdateUser: (updatedUser: User) => void;
}) => {
  const [username, setUsername] = useState(user.username);

  // Password change states
  const [isChangingPass, setIsChangingPass] = useState(false);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [msg, setMsg] = useState<{
    text: string;
    type: "success" | "error";
  } | null>(null);

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg(null);

    // Password validation
    if (isChangingPass && newPassword !== confirmPassword) {
      setMsg({ text: "Passwords do not match!", type: "error" });
      return;
    }

    // Assuming endpoint PUT /api/users/{id}
    try {
      const body: { username: string; password?: string } = { username };
      if (isChangingPass && newPassword) {
        body.password = newPassword;
      }

      const res = await fetch(`${API_URL}/users/${user.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
        credentials: "include",
      });

      if (!res.ok) throw new Error("Failed to update profile.");

      setMsg({ text: "Profile updated successfully!", type: "success" });

      // Update state in main app
      onUpdateUser({ ...user, username });

      // Reset password form
      setNewPassword("");
      setConfirmPassword("");
      setIsChangingPass(false);
    } catch (err) {
      console.log(err);
      setMsg({ text: "Error updating profile.", type: "error" });
    }
  };

  return (
    <div className="max-w-2xl mx-auto bg-white p-6 sm:p-8 rounded-xl shadow-md border border-gray-200 mt-8">
      <div className="flex items-center justify-between mb-6 border-b pb-4">
        <h2 className="text-2xl font-bold text-gray-800">👤 User Profile</h2>
        <button
          onClick={onBack}
          className="cursor-pointer text-gray-500 hover:text-gray-800 px-3 py-1 rounded border border-gray-300 hover:bg-gray-100 transition text-sm"
        >
          ← Back to Dashboard
        </button>
      </div>

      {msg && (
        <div
          className={`p-3 rounded mb-4 text-center ${
            msg.type === "success"
              ? "bg-green-100 text-green-700"
              : "bg-red-100 text-red-700"
          }`}
        >
          {msg.text}
        </div>
      )}

      <form onSubmit={handleUpdateProfile} className="space-y-6">
        {/* EMAIL (Read Only) */}
        <div>
          <label className="block text-sm font-medium text-gray-500 mb-1">
            Email (read-only)
          </label>
          <input
            type="email"
            value={user.email}
            disabled
            className="w-full p-3 bg-gray-100 border border-gray-300 rounded-lg text-gray-500 cursor-not-allowed"
          />
        </div>

        {/* USERNAME */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Username
          </label>
          <input
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            className="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
          />
        </div>

        {/* PASSWORD SECTION */}
        <div className="pt-4 border-t border-gray-100">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Password
          </label>

          {!isChangingPass ? (
            <div className="flex gap-4 items-center">
              <input
                type="password"
                value="********"
                disabled
                className="w-full p-3 bg-gray-50 border border-gray-200 rounded-lg text-gray-400"
              />
              <button
                type="button"
                onClick={() => setIsChangingPass(true)}
                className="whitespace-nowrap px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-lg font-medium transition cursor-pointer"
              >
                Change Password
              </button>
            </div>
          ) : (
            <div className="bg-gray-50 p-4 rounded-lg border border-gray-200 space-y-3">
              <input
                type="password"
                placeholder="New Password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                className="w-full p-3 border border-gray-300 rounded-lg bg-white"
              />
              <input
                type="password"
                placeholder="Confirm New Password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className="w-full p-3 border border-gray-300 rounded-lg bg-white"
              />
              <button
                type="button"
                onClick={() => setIsChangingPass(false)}
                className="text-sm text-red-500 hover:underline cursor-pointer"
              >
                Cancel Password Change
              </button>
            </div>
          )}
        </div>

        {/* SAVE BUTTON */}
        <div className="pt-4">
          <button
            type="submit"
            className="w-full py-3 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-lg shadow-md transition-transform active:scale-[0.99] cursor-pointer"
          >
            💾 Save Changes
          </button>
        </div>
      </form>
    </div>
  );
};

const AuthForm = ({
  onLoginSuccess,
}: {
  onLoginSuccess: (user: User) => void;
}) => {
  const [isLoginMode, setIsLoginMode] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [username, setUsername] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);

    const endpoint = isLoginMode ? "/users/login" : "/users/register";
    const body = isLoginMode
      ? { email, password }
      : { username, email, password };

    try {
      const response = await fetch(`${API_URL}${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
        credentials: "include",
      });

      if (response.status >= 500) {
        throw new Error("Server is currently unavailable.");
      }

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.message || "Invalid credentials.");
      }

      if (isLoginMode) {
        onLoginSuccess(data);
      } else {
        alert("Registration successful! Please log in.");
        setIsLoginMode(true);
      }
    } catch (err: unknown) {
      if (err instanceof Error) {
        if (err.message === "Failed to fetch") {
          setError("Unable to connect to the server. Try again later.");
        } else {
          setError(err.message);
        }
      } else {
        setError("Something went wrong! Try again later.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-100 p-4">
      <div className="bg-white p-6 sm:p-8 rounded-xl shadow-lg w-full max-w-md border border-gray-200">
        <h2 className="text-2xl font-bold text-center mb-6 text-blue-600">
          {isLoginMode ? "🔐 Log In" : "📝 Register"}
        </h2>
        {error && (
          <div className="bg-red-100 text-red-700 p-3 rounded-lg mb-4 text-sm text-center font-medium border border-red-200">
            {error}
          </div>
        )}
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          {!isLoginMode && (
            <input
              type="text"
              placeholder="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="p-3 border rounded-lg w-full"
              required
            />
          )}
          <input
            type="email"
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="p-3 border rounded-lg w-full"
            required
          />
          <input
            type="password"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="p-3 border rounded-lg w-full"
            required
          />
          <button
            type="submit"
            disabled={isLoading}
            className={`py-3 rounded-lg font-bold text-white transition-colors w-full ${
              isLoading
                ? "cursor-not-allowed bg-blue-400"
                : "cursor-pointer bg-blue-600 hover:bg-blue-700"
            }`}
          >
            {isLoading
              ? "Connecting..."
              : isLoginMode
              ? "Sign In"
              : "Create Account"}
          </button>
        </form>
        <p className="text-center mt-6 text-sm text-gray-600">
          <button
            onClick={() => setIsLoginMode(!isLoginMode)}
            className={`${
              isLoading
                ? "cursor-not-allowed"
                : "cursor-pointer hover:underline"
            }  text-blue-600 font-semibold `}
            disabled={isLoading ? true : false}
          >
            {isLoginMode ? "Register here" : "Log in here"}
          </button>
        </p>
      </div>
    </div>
  );
};

const ActionButton = ({
  label,
  onClick,
  disabled,
  color,
}: {
  label: string;
  onClick: () => void;
  disabled: boolean;
  color: "green" | "red";
}) => {
  const baseClass =
    "flex-1 py-2 rounded-md text-sm font-medium transition-colors";
  const activeClass =
    color === "green"
      ? "cursor-pointer bg-green-500 hover:bg-green-600 text-white shadow-sm"
      : "cursor-pointer bg-red-500 hover:bg-red-600 text-white shadow-sm";
  const disabledClass =
    color === "green"
      ? "bg-green-200 text-green-800 cursor-not-allowed opacity-50"
      : "bg-red-200 text-red-800 cursor-not-allowed opacity-50";

  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`${baseClass} ${disabled ? disabledClass : activeClass}`}
    >
      {label}
    </button>
  );
};

// --- DEVICE CARD ---
const DeviceCard = ({ device, onDelete, onToggle, temp }: DeviceCardProps) => {
  const isBulb = device.type === "LightBulb";
  const isSensor = device.type === "TemperatureSensor";
  const bgClass = device.isOn
    ? "bg-yellow-50 border-yellow-200"
    : "bg-white border-gray-200";

  return (
    <div
      className={`relative p-5 rounded-xl border shadow-sm transition-all duration-300 hover:shadow-md ${bgClass}`}
    >
      <div className="flex justify-between items-start mb-2">
        <h3 className="text-lg font-bold text-gray-800 flex items-center gap-2 truncate pr-6">
          {isBulb ? "💡" : "🌡️"} <span className="truncate">{device.name}</span>
        </h3>
        <button
          onClick={() => onDelete(device.id)}
          className="cursor-pointer text-gray-400 hover:text-red-500 transition-colors p-1 shrink-0"
          title="Delete"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            className="h-5 w-5"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
            />
          </svg>
        </button>
      </div>
      <p className="text-sm text-gray-500 mb-1 truncate">📍 {device.room}</p>
      <p className="text-xs text-gray-400 font-mono mb-4">
        ID: {device.id.slice(0, 8)}...
      </p>

      {isBulb && (
        <div className="flex gap-2 mt-4">
          <ActionButton
            label="Turn On"
            onClick={() => onToggle(device.id, "turn-on")}
            disabled={!!device.isOn}
            color="green"
          />
          <ActionButton
            label="Turn Off"
            onClick={() => onToggle(device.id, "turn-off")}
            disabled={!device.isOn}
            color="red"
          />
        </div>
      )}

      {isSensor && (
        <div className="mt-4 pt-3 border-t border-gray-100 flex items-center justify-between">
          <span className="text-sm text-gray-500 font-medium uppercase tracking-wide">
            Temperature
          </span>
          <span className="text-3xl font-bold text-blue-600 tabular-nums">
            {temp?.toFixed(1) ?? device.currentTemperature?.toFixed(1) ?? "--"}{" "}
            <span className="text-lg text-gray-400">°C</span>
          </span>
        </div>
      )}
    </div>
  );
};

const DeviceForm = ({
  onAdd,
}: {
  onAdd: (name: string, room: string, type: string) => void;
}) => {
  const [name, setName] = useState("");
  const [room, setRoom] = useState("");
  const [type, setType] = useState("lightbulb");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onAdd(name, room, type);
    setName("");
    setRoom("");
  };

  return (
    <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 mb-8">
      <h3 className="text-xl font-semibold mb-4 text-gray-700">
        ➕ Add New Device
      </h3>
      <form onSubmit={handleSubmit} className="flex flex-col sm:flex-row gap-3">
        <input
          placeholder="Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          className="flex-1 p-2 border border-gray-300 rounded-lg w-full"
        />
        <input
          placeholder="Room"
          value={room}
          onChange={(e) => setRoom(e.target.value)}
          required
          className="flex-1 p-2 border border-gray-300 rounded-lg w-full"
        />
        <select
          value={type}
          onChange={(e) => setType(e.target.value)}
          className="p-2 border border-gray-300 rounded-lg bg-white w-full sm:w-auto"
        >
          <option value="lightbulb">💡 Light Bulb</option>
          <option value="sensor">🌡️ Temp Sensor</option>
        </select>
        <button
          type="submit"
          className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded-lg cursor-pointer transition-colors w-full sm:w-auto"
        >
          Add
        </button>
      </form>
    </div>
  );
};

// --- MAIN APP ---

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [devices, setDevices] = useState<Device[]>([]);
  const [temps, setTemps] = useState<Record<string, number>>({});
  const [actionError, setActionError] = useState<string | null>(null);
  const [globalError, setGlobalError] = useState<string | null>(null);

  const [view, setView] = useState<"dashboard" | "profile">("dashboard");
  const lightbulbs = devices.filter((d) => d.type === "LightBulb");
  const sensors = devices.filter((d) => d.type === "TemperatureSensor");

  const showError = (message: string) => {
    setActionError(message);
    setTimeout(() => setActionError(null), 3000);
  };

  const fetchDevices = useCallback(() => {
    fetch(`${API_URL}/devices`, { credentials: "include" })
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
            : err.message
        );
      });
  }, []);

  useEffect(() => {
    if (user && view === "dashboard") fetchDevices();
  }, [user, view, fetchDevices]);

  const handleLoginSuccess = (userData: User) => setUser(userData);

  const handleLogout = () => {
    fetch(`${API_URL}/users/logout`, {
      method: "POST",
      credentials: "include",
    }).catch(console.error);
    setUser(null);
    setDevices([]);
    setGlobalError(null);
    setActionError(null);
  };

  const handleAdd = (name: string, room: string, type: string) => {
    fetch(`${API_URL}/devices/${type}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, room, type }),
      credentials: "include",
    })
      .then((res) => {
        if (!res.ok) throw new Error("Failed to add device.");
      })
      .catch((err) => showError(err.message));
  };

  const handleToggle = (id: string, action: "turn-on" | "turn-off") => {
    fetch(`${API_URL}/devices/${id}/${action}`, {
      method: "PUT",
      credentials: "include",
    })
      .then((res) => {
        if (!res.ok) throw new Error(`Could not ${action.replace("-", " ")}.`);
      })
      .catch((err) => showError(err.message));
  };

  const handleDelete = (id: string) => {
    if (confirm("Delete this device?")) {
      fetch(`${API_URL}/devices/${id}`, {
        method: "DELETE",
        credentials: "include",
      })
        .then((res) => {
          if (!res.ok) throw new Error("Could not delete device.");
        })
        .catch((err) => showError(err.message));
    }
  };

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

    connection.on("RefreshDevices", () => fetchDevices());
    connection.on("ReceiveTemperature", (deviceId: string, newTemp: number) => {
      setTemps((prev) => ({ ...prev, [deviceId]: newTemp }));
    });

    return () => {
      connection.stop();
    };
  }, [user, fetchDevices]);

  if (!user) return <AuthForm onLoginSuccess={handleLoginSuccess} />;

  return (
    <div className="min-h-screen bg-gray-50 p-4 sm:p-8 font-sans text-gray-800 relative">
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
        <div className="flex flex-col sm:flex-row justify-between items-center mb-8 gap-4 sm:gap-0 bg-white p-4 rounded-xl shadow-sm border border-gray-100">
          <h1 className="text-2xl sm:text-3xl font-bold text-blue-600 flex items-center gap-2">
            🏠 Smart Home{" "}
            <span className="text-gray-400 text-lg font-normal">
              | {user.username}
            </span>
          </h1>

          <div className="flex gap-2 w-full sm:w-auto">
            {view === "dashboard" ? (
              <button
                onClick={() => setView("profile")}
                className="flex-1 sm:flex-none px-4 py-2 bg-blue-50 text-blue-700 hover:bg-blue-100 rounded-lg font-medium transition cursor-pointer"
              >
                👤 Profile
              </button>
            ) : (
              <button
                onClick={() => setView("dashboard")}
                className="flex-1 sm:flex-none px-4 py-2 bg-blue-50 text-blue-700 hover:bg-blue-100 rounded-lg font-medium transition cursor-pointer"
              >
                🏠 Dashboard
              </button>
            )}

            <button
              onClick={handleLogout}
              className="flex-1 sm:flex-none px-4 py-2 bg-gray-200 hover:bg-gray-300 text-gray-700 rounded-lg font-medium transition cursor-pointer"
            >
              🚪 Logout
            </button>
          </div>
        </div>

        {globalError && (
          <div
            className="bg-orange-100 border-l-4 border-orange-500 text-orange-700 p-4 mb-6"
            role="alert"
          >
            <p className="font-bold">System Warning</p>
            <p>{globalError}</p>
          </div>
        )}

        {view === "profile" ? (
          <UserProfile
            user={user}
            onBack={() => setView("dashboard")}
            onUpdateUser={setUser}
          />
        ) : (
          <>
            <DeviceForm onAdd={handleAdd} />

            {/* LIGHTING SECTION */}
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
                      onDelete={handleDelete}
                      onToggle={handleToggle}
                      temp={temps[device.id]}
                    />
                  ))}
                </div>
              </div>
            )}

            {/* SENSOR SECTION */}
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
                      onDelete={handleDelete}
                      onToggle={handleToggle}
                      temp={temps[device.id]}
                    />
                  ))}
                </div>
              </div>
            )}

            {devices.length === 0 && !globalError && (
              <div className="text-center mt-10 p-10 bg-white rounded-xl border border-dashed border-gray-300">
                <p className="text-gray-500 text-lg">No devices found.</p>
                <p className="text-gray-400 text-sm">
                  Use the form above to add your first device.
                </p>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}

export default App;
