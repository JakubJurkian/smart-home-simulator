import { useState, useCallback } from "react";
import { api } from "../services/api";
import type { Room } from "../types";

export function useRooms() {
  const [rooms, setRooms] = useState<Room[]>([]);

  const fetchRooms = useCallback(() => {
    api.rooms
      .getAll()
      .then((res) => res.json())
      .then((data) => setRooms(Array.isArray(data) ? data : []))
      .catch((err) => console.error("Failed to fetch rooms", err));
  }, []);

  const addRoom = useCallback(
    (name: string) => {
      api.rooms
        .add(name)
        .then((res) => {
          if (res.ok) fetchRooms();
          else alert("Failed to add room");
        })
        .catch(() => alert("Error adding room"));
    },
    [fetchRooms],
  );

  const renameRoom = useCallback(
    (id: string, newName: string) => {
      return api.rooms
        .rename(id, newName)
        .then((res) => {
          if (res.ok) fetchRooms();
          else alert("Failed to rename room");
          return res.ok;
        })
        .catch(() => {
          alert("Error renaming room");
          return false;
        });
    },
    [fetchRooms],
  );

  const deleteRoom = useCallback(
    (id: string) => {
      if (
        !confirm(
          "WARNING: Deleting this room will also DELETE ALL DEVICES inside it.\n\nAre you sure?",
        )
      )
        return Promise.resolve(false);

      return api.rooms
        .delete(id)
        .then(() => {
          fetchRooms();
          return true;
        })
        .catch(() => {
          alert("Error deleting room");
          return false;
        });
    },
    [fetchRooms],
  );

  return { rooms, fetchRooms, addRoom, renameRoom, deleteRoom };
}
