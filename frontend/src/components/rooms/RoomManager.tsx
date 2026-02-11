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
  const [addError, setAddError] = useState<string | null>(null);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editError, setEditError] = useState<boolean>(false);

  const isDuplicate = (name: string, excludeId?: string) => {
    const normalized = name.trim().toLowerCase();
    return rooms.some(
      (r) => r.id !== excludeId && r.name.toLowerCase() === normalized,
    );
  };

  const handleAddChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setNewRoomName(e.target.value);
    if (addError) setAddError(null);
  };

  const handleAddBlur = () => {
    const trimmed = newRoomName.trim();
    if (trimmed.length > 0) {
      if (trimmed.length < 1 || trimmed.length > 32) {
        setAddError("Name must be 1-32 chars");
      } else if (isDuplicate(trimmed)) {
        setAddError("Name already exists");
      }
    }
  };

  const handleAddSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = newRoomName.trim();

    if (trimmed.length < 1 || trimmed.length > 32 || isDuplicate(trimmed))
      return;

    onAdd(trimmed);
    setNewRoomName("");
    setAddError(null);
  };

  const canAdd =
    newRoomName.trim().length >= 1 &&
    newRoomName.trim().length <= 32 &&
    !isDuplicate(newRoomName.trim()) &&
    !addError;

  // --- EDIT LOGIC ---
  const startEditing = (room: Room) => {
    setEditingId(room.id);
    setEditName(room.name);
    setEditError(false);
  };

  const handleEditChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setEditName(e.target.value);
    setEditError(false);
  };

  const handleEditBlur = () => {
    const trimmed = editName.trim();
    if (
      trimmed.length === 0 ||
      trimmed.length > 32 ||
      isDuplicate(trimmed, editingId!)
    ) {
      setEditError(true);
    }
  };

  const saveEdit = (originalName: string) => {
    const trimmed = editName.trim();
    if (trimmed === originalName) {
      cancelEdit();
      return;
    }

    if (
      editingId &&
      trimmed.length >= 1 &&
      trimmed.length <= 32 &&
      !isDuplicate(trimmed, editingId)
    ) {
      onRename(editingId, trimmed);
      setEditingId(null);
      setEditError(false);
    }
  };

  const cancelEdit = () => {
    setEditingId(null);
    setEditError(false);
  };

  return (
    <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 mb-8">
      <h3 className="text-xl font-semibold mb-4 text-gray-700">
        Manage Rooms
      </h3>

      <div className="flex flex-wrap gap-3 mb-6">
        {rooms.length === 0 && (
          <span className="text-gray-400 text-sm italic">
            No rooms created yet.
          </span>
        )}

        {rooms.map((room) => {
          const isEditing = editingId === room.id;
          const duplicate = isEditing
            ? isDuplicate(editName.trim(), room.id)
            : false;
          const validLength =
            editName.trim().length >= 1 && editName.trim().length <= 32;
          const changed = editName.trim() !== room.name;
          const canSaveEdit = isEditing && validLength && !duplicate && changed;

          return (
            <div
              key={room.id}
              className={`px-3 py-2 rounded-lg text-sm font-medium flex items-center gap-2 border shadow-sm transition-all ${
                isEditing
                  ? "bg-white border-blue-400 ring-2 ring-blue-100"
                  : "bg-blue-50 text-blue-700 border-blue-100 hover:shadow-md"
              }`}
            >
              {isEditing ? (
                <div className="relative flex items-center gap-1">
                  <input
                    value={editName}
                    onChange={handleEditChange}
                    onBlur={handleEditBlur}
                    onKeyDown={(e) => {
                      if (e.key === "Enter") saveEdit(room.name);
                      if (e.key === "Escape") cancelEdit();
                    }}
                    className={`w-32 p-1 text-xs border rounded bg-white focus:outline-none ${
                      editError || duplicate
                        ? "border-red-500 text-red-600"
                        : "border-blue-300"
                    }`}
                    autoFocus
                    maxLength={32}
                  />
                  <button
                    onClick={() => saveEdit(room.name)}
                    disabled={!canSaveEdit}
                    className={`cursor-pointer ${
                      canSaveEdit
                        ? "text-green-600 hover:text-green-800"
                        : "text-gray-300 cursor-not-allowed"
                    }`}
                  >
                    ✓
                  </button>
                  <button
                    onClick={cancelEdit}
                    className="text-gray-400 hover:text-gray-600 cursor-pointer"
                  >
                    ✕
                  </button>

                  {duplicate && (
                    <div className="absolute bottom-full left-0 mb-1 bg-red-500 text-white text-[10px] px-1 rounded whitespace-nowrap">
                      Exists!
                    </div>
                  )}
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
                  >
                    &times;
                  </button>
                </>
              )}
            </div>
          );
        })}
      </div>

      <form
        onSubmit={handleAddSubmit}
        className="flex gap-2 border-t pt-4 items-start"
      >
        <div className="flex flex-col">
          <input
            type="text"
            placeholder="New Room Name..."
            value={newRoomName}
            onChange={handleAddChange}
            onBlur={handleAddBlur}
            maxLength={32}
            className={`p-2 border rounded-lg text-sm focus:ring-2 outline-none w-64 ${
              addError
                ? "border-red-500 focus:ring-red-200"
                : "border-gray-300 focus:ring-blue-500"
            }`}
          />
          {addError && (
            <span className="text-xs text-red-500 mt-1">{addError}</span>
          )}
        </div>
        <button
          type="submit"
          disabled={!canAdd}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition ${
            canAdd
              ? "bg-blue-600 hover:bg-blue-700 text-white cursor-pointer"
              : "bg-gray-300 text-gray-500 cursor-not-allowed"
          }`}
        >
          Add Room
        </button>
      </form>
    </div>
  );
};

export default RoomManager;
