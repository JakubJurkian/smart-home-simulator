import { useState, useEffect } from "react";

// CONFIG & TYPES
const API_URL = "http://localhost:5187/api";

export interface Device {
  id: string;
  name: string;
  room: string;
  type: string;
  isOn?: boolean;
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
  onCheckTemp: (id: string) => void;
  temp?: number;
}

// COMPONENTS

// Component: Login / Registration Form (ADDED)
const AuthForm = ({ onLoginSuccess }: { onLoginSuccess: (user: User) => void }) => {
  const [isLoginMode, setIsLoginMode] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [username, setUsername] = useState("");
  const [error, setError] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    const endpoint = isLoginMode ? "/users/login" : "/users/register";
    const body = isLoginMode ? { email, password } : { username, email, password };

    try {
      const response = await fetch(`${API_URL}${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });

      const data = await response.json();

      if (!response.ok) throw new Error(data.message || "Error occurred");

      if (isLoginMode) {
        onLoginSuccess(data);
      } else {
        alert("Registration successful! Please log in.");
        setIsLoginMode(true);
      }
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : "An error occurred";
      setError(errorMessage);
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-100">
      <div className="bg-white p-8 rounded-xl shadow-lg w-full max-w-md border border-gray-200">
        <h2 className="text-2xl font-bold text-center mb-6 text-blue-600">
          {isLoginMode ? "🔐 Log In" : "📝 Register"}
        </h2>
        {error && <div className="bg-red-100 text-red-700 p-3 rounded mb-4 text-sm text-center">{error}</div>}
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          {!isLoginMode && (
            <input type="text" placeholder="Username" value={username} onChange={e => setUsername(e.target.value)} className="p-3 border rounded-lg" required />
          )}
          <input type="email" placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} className="p-3 border rounded-lg" required />
          <input type="password" placeholder="Password" value={password} onChange={e => setPassword(e.target.value)} className="p-3 border rounded-lg" required />
          <button type="submit" className="bg-blue-600 text-white py-3 rounded-lg font-bold hover:bg-blue-700 transition-colors cursor-pointer">
            {isLoginMode ? "Sign In" : "Create Account"}
          </button>
        </form>
        <p className="text-center mt-6 text-sm text-gray-600">
          {isLoginMode ? "No account? " : "Already have an account? "}
          <button onClick={() => setIsLoginMode(!isLoginMode)} className="text-blue-600 font-semibold hover:underline cursor-pointer">
            {isLoginMode ? "Register here" : "Log in here"}
          </button>
        </p>
      </div>
    </div>
  );
};

// Button component
const ActionButton = ({ label, onClick, disabled, color }: { label: string, onClick: () => void, disabled: boolean, color: "green" | "red" }) => {
  const baseClass = "flex-1 py-2 rounded-md text-sm font-medium transition-colors";
  const activeClass = color === "green" 
    ? "bg-green-500 hover:bg-green-600 text-white shadow-sm" 
    : "bg-red-500 hover:bg-red-600 text-white shadow-sm";
  const disabledClass = color === "green" 
    ? "bg-green-200 text-green-800 cursor-not-allowed opacity-50" 
    : "bg-red-200 text-red-800 cursor-not-allowed opacity-50";

  return (
    <button onClick={onClick} disabled={disabled} className={`${baseClass} ${disabled ? disabledClass : activeClass}`}>
      {label}
    </button>
  );
};

// Single Device Card
const DeviceCard = ({ device, onDelete, onToggle, onCheckTemp, temp }: DeviceCardProps) => {
  const isBulb = device.type === "LightBulb";
  const isSensor = device.type === "TemperatureSensor";
  const bgClass = device.isOn ? "bg-yellow-50 border-yellow-200" : "bg-white border-gray-200";

  return (
    <div className={`relative p-5 rounded-xl border shadow-sm transition-all duration-300 hover:shadow-md ${bgClass}`}>
      <div className="flex justify-between items-start mb-2">
        <h3 className="text-lg font-bold text-gray-800 flex items-center gap-2">
          {isBulb ? "💡" : "🌡️"} {device.name}
        </h3>
        <button onClick={() => onDelete(device.id)} className="text-gray-400 hover:text-red-500 transition-colors p-1" title="Delete">
          <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
          </svg>
        </button>
      </div>
      <p className="text-sm text-gray-500 mb-1">📍 {device.room}</p>
      <p className="text-xs text-gray-400 font-mono mb-4">ID: {device.id.slice(0, 8)}...</p>

      {isBulb && (
        <div className="flex gap-2 mt-4">
          <ActionButton label="Turn On" onClick={() => onToggle(device.id, "turn-on")} disabled={!!device.isOn} color="green" />
          <ActionButton label="Turn Off" onClick={() => onToggle(device.id, "turn-off")} disabled={!device.isOn} color="red" />
        </div>
      )}

      {isSensor && (
        <div className="mt-4 pt-3 border-t border-gray-100 flex justify-between items-center">
          <button onClick={() => onCheckTemp(device.id)} className="text-sm bg-blue-50 hover:bg-blue-100 text-blue-600 py-1 px-3 rounded transition-colors">
            Check Temp.
          </button>
          <span className="text-lg font-bold text-gray-700">{temp !== undefined ? `${temp} °C` : "--"}</span>
        </div>
      )}
    </div>
  );
};

// Form component
const DeviceForm = ({ onAdd }: { onAdd: (name: string, room: string, type: string) => void }) => {
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
      <h3 className="text-xl font-semibold mb-4 text-gray-700">➕ Add New Device</h3>
      <form onSubmit={handleSubmit} className="flex flex-col sm:flex-row gap-3">
        <input placeholder="Name" value={name} onChange={e => setName(e.target.value)} required className="flex-1 p-2 border border-gray-300 rounded-lg" />
        <input placeholder="Room" value={room} onChange={e => setRoom(e.target.value)} required className="flex-1 p-2 border border-gray-300 rounded-lg" />
        <select value={type} onChange={e => setType(e.target.value)} className="p-2 border border-gray-300 rounded-lg bg-white">
          <option value="lightbulb">💡 Light Bulb</option>
          <option value="sensor">🌡️ Temp Sensor</option>
        </select>
        <button type="submit" className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded-lg cursor-pointer transition-colors">Add</button>
      </form>
    </div>
  );
};

// MAIN APP

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [devices, setDevices] = useState<Device[]>([]);
  const [temps, setTemps] = useState<Record<string, number>>({});

  const fetchDevices = () => {
    fetch(`${API_URL}/devices`)
      .then(res => res.json())
      .then(setDevices)
      .catch(console.error);
  };

  useEffect(() => {
    if (user) fetchDevices();
  }, [user]);

  // Handlers
  const handleLoginSuccess = (userData: User) => {
    setUser(userData);
  };

  const handleLogout = () => {
    setUser(null);
    setDevices([]);
  };

  const handleAdd = (name: string, room: string, type: string) => {
    fetch(`${API_URL}/devices/${type}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name, room }),
    }).then(res => res.ok ? fetchDevices() : alert("Error adding device"));
  };

  const handleToggle = (id: string, action: "turn-on" | "turn-off") => {
    fetch(`${API_URL}/devices/${id}/${action}`, { method: "POST" })
      .then(res => res.ok ? fetchDevices() : alert("Error toggling device"));
  };

  const handleDelete = (id: string) => {
    if (confirm("Delete this device?")) {
      fetch(`${API_URL}/devices/${id}`, { method: "DELETE" })
        .then(res => res.ok && fetchDevices());
    }
  };

  const handleCheckTemp = (id: string) => {
    fetch(`${API_URL}/devices/${id}/temperature`)
      .then(res => res.json())
      .then(data => setTemps(prev => ({ ...prev, [id]: data.temperature })))
      .catch(() => alert("Error reading temp"));
  };

  // Conditional rendering
  if (!user) {
    return <AuthForm onLoginSuccess={handleLoginSuccess} />;
  }

  return (
    <div className="min-h-screen bg-gray-50 p-8 font-sans text-gray-800">
      <div className="max-w-5xl mx-auto">
        
        {/* Header with Logout */}
        <div className="flex justify-between items-center mb-8">
          <h1 className="text-3xl font-bold text-blue-600">
             🏠 Smart Home <span className="text-gray-400 text-lg ml-2 font-normal">| {user.username}</span>
          </h1>
          <button 
            onClick={handleLogout} 
            className="cursor-pointer bg-gray-200 hover:bg-gray-300 text-gray-700 px-4 py-2 rounded-lg text-sm font-medium transition-colors"
          >
            🚪 Logout
          </button>
        </div>
        
        <DeviceForm onAdd={handleAdd} />

        <div className="flex justify-end mb-4">
          <button onClick={fetchDevices} className="cursor-pointer text-sm bg-gray-200 hover:bg-gray-300 text-gray-700 py-2 px-4 rounded-lg flex items-center gap-2">
            🔄 Refresh List
          </button>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {devices.map(device => (
            <DeviceCard 
              key={device.id} 
              device={device} 
              onDelete={handleDelete}
              onToggle={handleToggle}
              onCheckTemp={handleCheckTemp}
              temp={temps[device.id]}
            />
          ))}
        </div>
        
        {devices.length === 0 && <p className="text-center text-gray-500 mt-10">No devices found.</p>}
      </div>
    </div>
  );
}

export default App;