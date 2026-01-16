import { useState } from "react";
import type { Room } from "../../types";

const DeviceForm = ({
  rooms,
  onAdd,
}: {
  rooms: Room[];
  onAdd: (name: string, roomId: string, type: string) => void;
}) => {
  const [name, setName] = useState("");
  const [roomId, setRoomId] = useState("");
  const [type, setType] = useState("LightBulb");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!roomId) {
      alert("Please select a room first!");
      return;
    }
    onAdd(name, roomId, type);
    setName("");
  };

  return (
    <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 mb-8">
      <h3 className="text-xl font-semibold mb-4 text-gray-700">
        ➕ Add New Device
      </h3>
      <form onSubmit={handleSubmit} className="flex flex-col sm:flex-row gap-3">
        <input
          placeholder="Device Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          className="flex-1 p-2 border border-gray-300 rounded-lg w-full"
        />

        <select
          value={roomId}
          onChange={(e) => setRoomId(e.target.value)}
          required
          className="flex-1 p-2 border border-gray-300 rounded-lg bg-white w-full"
        >
          <option value="" disabled>
            -- Select Room --
          </option>
          {rooms.map((r) => (
            <option key={r.id} value={r.id}>
              {r.name}
            </option>
          ))}
        </select>

        <select
          value={type}
          onChange={(e) => setType(e.target.value)}
          className="p-2 border border-gray-300 rounded-lg bg-white w-full sm:w-auto"
        >
          <option value="LightBulb">💡 Light Bulb</option>
          <option value="TemperatureSensor">🌡️ Temp Sensor</option>
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

export default DeviceForm;
