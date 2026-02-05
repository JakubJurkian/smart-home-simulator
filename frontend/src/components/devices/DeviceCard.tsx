import { useState } from "react";
import { api } from "../../services/api";
import type { DeviceCardProps } from "../../types";

// Helper component
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

const DeviceCard = ({
  device,
  onDelete,
  onToggle,
  temp,
  onOpenLogs,
}: DeviceCardProps) => {
  const isBulb = device.type === "LightBulb";
  const isSensor = device.type === "TemperatureSensor";
  const bgClass = device.isOn
    ? "bg-yellow-50 border-yellow-200"
    : "bg-white border-gray-200";

  const [isEditing, setIsEditing] = useState(false);
  const [editedName, setEditedName] = useState(device.name);
  const [nameError, setNameError] = useState<string | null>(null);

  // --- VALIDATION LOGIC ---
  const isNameChanged = editedName.trim() !== device.name;
  const isNameValid =
    editedName.trim().length >= 1 && editedName.trim().length <= 32;
  const canSave = isNameChanged && isNameValid;

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setEditedName(e.target.value);
    if (nameError) setNameError(null); // Clear error on typing
  };

  const handleNameBlur = () => {
    const len = editedName.trim().length;
    if (len === 0) setNameError("Name required");
    else if (len > 32) setNameError("Max 32 chars");
  };

  const handleSaveName = async () => {
    if (!canSave) return;

    try {
      const res = await api.devices.rename(device.id, editedName.trim());
      if (!res.ok) throw new Error("Failed to rename");

      // Direct mutation for instant UI feedback
      device.name = editedName.trim();
      setIsEditing(false);
      setNameError(null);
    } catch (err) {
      console.log(err);
      alert("Error renaming device");
    }
  };

  const handleCancel = () => {
    setEditedName(device.name);
    setIsEditing(false);
    setNameError(null);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && canSave) handleSaveName();
    if (e.key === "Escape") handleCancel();
  };

  return (
    <div
      className={`relative p-5 rounded-xl border shadow-sm transition-all duration-300 hover:shadow-md ${bgClass}`}
    >
      <div className="flex justify-between items-start mb-2">
        <div className="flex-1 pr-2">
          {isEditing ? (
            <div className="relative">
              <div className="flex items-center gap-1">
                <input
                  value={editedName}
                  onChange={handleNameChange}
                  onBlur={handleNameBlur}
                  onKeyDown={handleKeyDown}
                  className={`w-full p-1 border rounded text-sm font-bold text-gray-800 bg-white focus:outline-none focus:ring-2 ${
                    nameError
                      ? "border-red-500 focus:ring-red-200"
                      : "border-blue-300 focus:ring-blue-500"
                  }`}
                  autoFocus
                  maxLength={32}
                />
                <button
                  onClick={handleSaveName}
                  disabled={!canSave}
                  className={`p-1 rounded transition-colors ${
                    canSave
                      ? "text-green-600 hover:bg-green-100 cursor-pointer"
                      : "text-gray-300 cursor-not-allowed"
                  }`}
                  title="Save"
                >
                  ✓
                </button>
                <button
                  onClick={handleCancel}
                  className="text-red-500 hover:bg-red-100 p-1 rounded cursor-pointer"
                  title="Cancel"
                >
                  ✕
                </button>
              </div>
              {/* Absolute error message for tight spaces */}
              {nameError && (
                <div className="absolute top-full left-0 text-[10px] text-red-500 bg-white px-1 mt-0.5 rounded shadow-sm z-10">
                  {nameError}
                </div>
              )}
            </div>
          ) : (
            <h3 className="text-lg font-bold text-gray-800 flex items-center gap-2 group">
              <span className="shrink-0 text-xl">{isBulb ? "💡" : "🌡️"}</span>

              <span
                className="flex-1 min-w-0 break-all leading-tight cursor-default"
                title={device.name}
              >
                {device.name}
              </span>

              <button
                onClick={() => setIsEditing(true)}
                className="shrink-0 opacity-0 group-hover:opacity-100 text-gray-400 hover:text-blue-500 transition-opacity p-1 text-sm cursor-pointer"
                title="Rename"
              >
                ✏️
              </button>
            </h3>
          )}
        </div>

        {/* Action Buttons (Logs / Delete) */}
        <div className="flex gap-1 shrink-0 ml-2">
          <button
            onClick={() => onOpenLogs(device)}
            className="cursor-pointer text-gray-400 hover:text-blue-500 transition-colors p-1"
            title="Service Logs"
          >
            🛠️
          </button>
          <button
            onClick={() => onDelete(device.id)}
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
      </div>

      <p className="text-sm text-gray-500 mb-1 truncate">
        📍 {device.room?.name || "Unknown"}
      </p>
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

export default DeviceCard;
