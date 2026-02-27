import { useState, useCallback, useRef, useMemo, useEffect } from "react";
import { api } from "../services/api";
import type { Device } from "../types";

export function useDevices(
  onError: (msg: string) => void,
  onUnauthorized: () => void,
) {
  const [devices, setDevices] = useState<Device[]>([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [temps, setTemps] = useState<Record<string, number>>({});
  const [globalError, setGlobalError] = useState<string | null>(null);

  const searchTermRef = useRef(searchTerm);

  useEffect(() => {
    searchTermRef.current = searchTerm;
  }, [searchTerm]);

  const fetchDevices = useCallback(
    (search?: string) => {
      const query = search ?? searchTermRef.current;

      api.devices
        .getAll(query)
        .then((res) => {
          if (res.status === 401) {
            onUnauthorized();
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
              ? "Connection to server lost."
              : err.message,
          );
        });
    },
    [onUnauthorized],
  );

  const handleSearchChange = useCallback(
    (value: string) => {
      setSearchTerm(value);
      fetchDevices(value);
    },
    [fetchDevices],
  );

  const addDevice = useCallback(
    (name: string, roomId: string, type: string) => {
      api.devices
        .add(type, { name, roomId, type })
        .then((res) => {
          if (!res.ok) throw new Error("Failed to add device.");
          fetchDevices();
        })
        .catch((err) => onError(err.message));
    },
    [fetchDevices, onError],
  );

  const toggleDevice = useCallback(
    (id: string, action: "turn-on" | "turn-off") => {
      api.devices
        .toggle(id, action)
        .then((res) => {
          if (!res.ok)
            throw new Error(`Could not ${action.replace("-", " ")}.`);
        })
        .catch((err) => onError(err.message));
    },
    [onError],
  );

  const deleteDevice = useCallback(
    (id: string) => {
      if (!confirm("Delete this device?")) return;

      api.devices
        .delete(id)
        .then((res) => {
          if (!res.ok) throw new Error("Could not delete device.");
          fetchDevices();
        })
        .catch((err) => onError(err.message));
    },
    [fetchDevices, onError],
  );

  const lightbulbs = useMemo(
    () => devices.filter((d) => d.type === "LightBulb"),
    [devices],
  );

  const sensors = useMemo(
    () => devices.filter((d) => d.type === "TemperatureSensor"),
    [devices],
  );

  return {
    devices,
    lightbulbs,
    sensors,
    temps,
    setTemps,
    searchTerm,
    handleSearchChange,
    fetchDevices,
    globalError,
    addDevice,
    toggleDevice,
    deleteDevice,
  };
}
