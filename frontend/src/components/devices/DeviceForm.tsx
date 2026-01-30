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
  const [type, setType] = useState("");

  const [nameError, setNameError] = useState<string | null>(null);

  // --- VALIDATION ---
  const isNameValid = name.trim().length >= 1 && name.trim().length <= 32;
  const isRoomSelected = roomId !== "";
  const isTypeSelected = type !== "";

  const canAdd = isNameValid && isRoomSelected && isTypeSelected;

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setName(e.target.value);
    if (nameError) setNameError(null);
  };

  const handleNameBlur = () => {
    const trimmed = name.trim();
    if (trimmed.length > 0) {
      if (trimmed.length < 1 || trimmed.length > 32) {
        setNameError("Name must be 1-32 chars");
      }
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canAdd) return;

    onAdd(name.trim(), roomId, type);

    setName("");
    setType("");
    setNameError(null);
  };

  return (
    <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 mb-8">
      <h3 className="text-xl font-semibold mb-4 text-gray-700">
        ➕ Add New Device
      </h3>
      <form
        onSubmit={handleSubmit}
        className="flex flex-col sm:flex-row gap-3 items-start"
      >
        <div className="flex-1 w-full">
          <input
            placeholder="Device Name"
            value={name}
            onChange={handleNameChange}
            onBlur={handleNameBlur}
            className={`p-2 border rounded-lg w-full outline-none ${
              nameError
                ? "border-red-500 focus:ring-2 focus:ring-red-200"
                : "border-gray-300 focus:ring-2 focus:ring-blue-500"
            }`}
            maxLength={32}
          />
          {nameError && (
            <p className="text-xs text-red-500 mt-1">{nameError}</p>
          )}
        </div>

        <select
          value={roomId}
          onChange={(e) => setRoomId(e.target.value)}
          required
          className="flex-1 p-2 border border-gray-300 rounded-lg bg-white w-full h-10.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
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
          required // 👈 Wymagane HTML
          className="p-2 border border-gray-300 rounded-lg bg-white w-full sm:w-auto h-10.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          {/* 👇 Nowa opcja domyślna */}
          <option value="" disabled>
            -- Select Type --
          </option>
          <option value="LightBulb">💡 Light Bulb</option>
          <option value="TemperatureSensor">🌡️ Temp Sensor</option>
        </select>

        <button
          type="submit"
          disabled={!canAdd}
          className={`font-medium py-2 px-6 rounded-lg transition-colors w-full sm:w-auto h-10.5 ${
            canAdd
              ? "bg-blue-600 hover:bg-blue-700 text-white cursor-pointer"
              : "bg-gray-300 text-gray-500 cursor-not-allowed"
          }`}
        >
          Add
        </button>
      </form>
    </div>
  );
};

export default DeviceForm;
