import { useState } from "react";
import type { Room } from "../../types";

const RoomManager = ({
  rooms,
  onAdd,
  onDelete,
  onRename,
}: {
  rooms: Room[];
  onAdd: (name: string) => void;
  onDelete: (id: string) => void;
  onRename: (id: string, newName: string) => void;
}) => {
  const [newRoomName, setNewRoomName] = useState("");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");

  const handleAddSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newRoomName.trim()) return;
    onAdd(newRoomName);
    setNewRoomName("");
  };

  const startEditing = (room: Room) => {
    setEditingId(room.id);
    setEditName(room.name);
  };

  const saveEdit = () => {
    if (editingId && editName.trim()) {
      onRename(editingId, editName);
      setEditingId(null);
    }
  };

  const cancelEdit = () => {
    setEditingId(null);
  };

  return (
    <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 mb-8">
      <h3 className="text-xl font-semibold mb-4 text-gray-700">
        🏠 Manage Rooms
      </h3>

      <div className="flex flex-wrap gap-3 mb-6">
        {rooms.length === 0 && (
          <span className="text-gray-400 text-sm italic">
            No rooms created yet.
          </span>
        )}

        {rooms.map((room) => (
          <div
            key={room.id}
            className="bg-blue-50 text-blue-700 px-3 py-2 rounded-lg text-sm font-medium flex items-center gap-2 border border-blue-100 shadow-sm transition-all hover:shadow-md"
          >
            {editingId === room.id ? (
              <div className="flex items-center gap-1">
                <input
                  value={editName}
                  onChange={(e) => setEditName(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") saveEdit();
                    if (e.key === "Escape") cancelEdit();
                  }}
                  className="w-24 p-1 text-xs border border-blue-300 rounded bg-white focus:outline-none"
                  autoFocus
                />
                <button
                  onClick={saveEdit}
                  className="text-green-600 hover:text-green-800 cursor-pointer"
                >
                  ✓
                </button>
                <button
                  onClick={cancelEdit}
                  className="text-gray-400 hover:text-gray-600 cursor-pointer"
                >
                  ✕
                </button>
              </div>
            ) : (
              <>
                <span
                  onDoubleClick={() => startEditing(room)}
                  className="cursor-pointer select-none"
                  title="Double click to edit"
                >
                  {room.name}
                </span>

                <button
                  onClick={() => startEditing(room)}
                  className="text-blue-300 hover:text-blue-600 cursor-pointer ml-1"
                >
                  ✏️
                </button>

                <span className="text-blue-200">|</span>

                <button
                  onClick={() => onDelete(room.id)}
                  className="text-red-300 hover:text-red-500 font-bold leading-none cursor-pointer text-lg"
                  title="Delete Room & All Devices inside"
                >
                  &times;
                </button>
              </>
            )}
          </div>
        ))}
      </div>

      <form onSubmit={handleAddSubmit} className="flex gap-2 border-t pt-4">
        <input
          type="text"
          placeholder="New Room Name..."
          value={newRoomName}
          onChange={(e) => setNewRoomName(e.target.value)}
          className="p-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 outline-none w-64"
        />
        <button
          type="submit"
          className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg text-sm font-medium transition cursor-pointer"
        >
          Add Room
        </button>
      </form>
    </div>
  );
};

export default RoomManager;
