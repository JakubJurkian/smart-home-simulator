import { useState, useEffect } from "react";

const API_URL = "http://localhost:5187/api/devices";

interface Device {
  type: string;
  id: string;
  name: string;
  room: string;
  discriminator: string; // 'LightBulb' or 'TemperatureSensor'
  isOn?: boolean;
  currentTemperature?: number;
}

function App() {
  const [devices, setDevices] = useState<Device[]>([]);
  const [temps, setTemps] = useState<Record<string, number>>({});

  const [newDeviceName, setNewDeviceName] = useState("");
  const [newDeviceRoom, setNewDeviceRoom] = useState("");
  const [newDeviceType, setNewDeviceType] = useState("lightbulb");

  // READ
  const fetchDevices = () => {
    fetch(API_URL)
      .then((res) => res.json())
      .then((data) => setDevices(data))
      .catch((err) => console.error("Network error:", err));
  };

  useEffect(() => {
    fetchDevices();
  }, []);

  // UPDATE
  const handleToggle = (id: string, action: "turn-on" | "turn-off") => {
    fetch(`${API_URL}/${id}/${action}`, {
      method: "POST",
    }).then((res) => {
      if (res.ok) {
        fetchDevices();
      } else {
        alert("Failed to toggle device!");
      }
    });
  };

  // DELETE
  const handleDelete = (id: string) => {
    if (!confirm("Are you sure you want to delete this device?")) return;

    fetch(`${API_URL}/${id}`, {
      method: "DELETE",
    }).then((res) => {
      if (res.ok) fetchDevices();
    });
  };

  // READ DETAILS
  const handleCheckTemp = (id: string) => {
    fetch(`${API_URL}/${id}/temperature`)
      .then((res) => res.json())
      .then((data) => {
        setTemps((prev) => ({ ...prev, [id]: data.temperature }));
      })
      .catch(() => alert("Error reading temperature"));
  };

  // CREATE
  const handleAddDevice = (e: React.FormEvent) => {
    e.preventDefault();
    fetch(`${API_URL}/${newDeviceType}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: newDeviceName, room: newDeviceRoom }),
    }).then((res) => {
      if (res.ok) {
        fetchDevices();
        setNewDeviceName("");
        setNewDeviceRoom("");
      } else {
        alert("Error adding device!");
      }
    });
  };

  console.log(devices);

  return (
    <div className="min-h-screen bg-gray-50 p-8 font-sans text-gray-800">
      <div className="max-w-5xl mx-auto">
        <h1 className="text-3xl font-bold text-center mb-8 text-blue-600">
          🏠 Smart Home Manager
        </h1>

        {/* --- ADD DEVICE FORM --- */}
        <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 mb-8">
          <h3 className="text-xl font-semibold mb-4 text-gray-700">
            ➕ Add New Device
          </h3>
          <form
            onSubmit={handleAddDevice}
            className="flex flex-col sm:flex-row gap-3"
          >
            <input
              placeholder="Name (e.g. Lamp)"
              value={newDeviceName}
              onChange={(e) => setNewDeviceName(e.target.value)}
              required
              className="flex-1 p-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <input
              placeholder="Room (e.g. Living Room)"
              value={newDeviceRoom}
              onChange={(e) => setNewDeviceRoom(e.target.value)}
              required
              className="flex-1 p-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <select
              value={newDeviceType}
              onChange={(e) => setNewDeviceType(e.target.value)}
              className="p-2 border border-gray-300 rounded-lg bg-white focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="lightbulb">💡 Light Bulb</option>
              <option value="sensor">🌡️ Temp Sensor</option>
            </select>
            <button
              type="submit"
              className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-6 rounded-lg transition-colors cursor-pointer"
            >
              Add
            </button>
          </form>
        </div>

        {/* --- DEVICE LIST --- */}
        <div className="flex justify-end mb-4">
          <button
            onClick={fetchDevices}
            className="cursor-pointer text-sm bg-gray-200 hover:bg-gray-300 text-gray-700 py-2 px-4 rounded-lg transition-colors flex items-center gap-2"
          >
            🔄 Refresh List
          </button>
        </div>

        {/* --- GRID --- */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {devices.map((device) => (
            <div
              key={device.id}
              className={`
                relative p-5 rounded-xl border shadow-sm transition-all duration-300 hover:shadow-md
                ${
                  device.isOn
                    ? "bg-yellow-50 border-yellow-200" // On
                    : "bg-white border-gray-200" // Off
                }
              `}
            >
              <div className="flex justify-between items-start mb-2">
                <h3 className="text-lg font-bold text-gray-800 flex items-center gap-2">
                  {device.type === "LightBulb" ? "💡" : "🌡️"}
                  {device.name}
                </h3>
                <button
                  onClick={() => handleDelete(device.id)}
                  className="cursor-pointer text-gray-400 hover:text-red-500 transition-colors p-1"
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

              <p className="text-sm text-gray-500 mb-1">📍 {device.room}</p>
              <p className="text-xs text-gray-400 font-mono mb-4">
                ID: {device.id.slice(0, 8)}...
              </p>

              {/* LOGIC FOR LIGHT BULB */}
              {device.type === "LightBulb" && (
                <div className="flex gap-2 mt-4">
                  <button
                    onClick={() => handleToggle(device.id, "turn-on")}
                    disabled={!!device.isOn}
                    className={`flex-1 py-2 rounded-md text-sm font-medium transition-colors
                      ${
                        device.isOn
                          ? "bg-green-200 text-green-800 cursor-not-allowed opacity-50"
                          : "bg-green-500 hover:bg-green-600 text-white shadow-sm cursor-pointer"
                      }`}
                  >
                    Turn On
                  </button>
                  <button
                    onClick={() => handleToggle(device.id, "turn-off")}
                    disabled={!device.isOn}
                    className={`flex-1 py-2 rounded-md text-sm font-medium transition-colors
                      ${
                        !device.isOn
                          ? "bg-red-200 text-red-800 cursor-not-allowed opacity-50"
                          : "bg-red-500 hover:bg-red-600 text-white shadow-sm cursor-pointer"
                      }`}
                  >
                    Turn Off
                  </button>
                </div>
              )}

              {/* LOGIC FOR SENSOR */}
              {device.type === "TemperatureSensor" && (
                <div className="mt-4 pt-3 border-t border-gray-100 flex justify-between items-center">
                  <button
                    onClick={() => handleCheckTemp(device.id)}
                    className="cursor-pointer text-sm bg-blue-50 hover:bg-blue-100 text-blue-600 py-1 px-3 rounded transition-colors"
                  >
                    Check Temp.
                  </button>
                  <span className="text-lg font-bold text-gray-700">
                    {temps[device.id] !== undefined
                      ? `${temps[device.id]} °C`
                      : "--"}
                  </span>
                </div>
              )}
            </div>
          ))}
        </div>

        {devices.length === 0 && (
          <p className="text-center text-gray-500 mt-10">No devices found.</p>
        )}
      </div>
    </div>
  );
}

export default App;
