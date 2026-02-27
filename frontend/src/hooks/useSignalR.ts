import { useEffect, useRef } from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import type { User } from "../types";

/**
 * Manages a SignalR connection for real-time device updates.
 *
 * Uses refs for callbacks to avoid reconnecting on every callback change
 * (e.g. when searchTerm changes, we don't need to tear down the connection).
 */
export function useSignalR(
  user: User | null,
  onRefreshDevices: () => void,
  onTemperature: (deviceId: string, temp: number) => void,
) {
  const refreshRef = useRef(onRefreshDevices);
  const tempRef = useRef(onTemperature);

  useEffect(() => {
    refreshRef.current = onRefreshDevices;
  }, [onRefreshDevices]);

  useEffect(() => {
    tempRef.current = onTemperature;
  }, [onTemperature]);

  useEffect(() => {
    if (!user) return;

    const connection = new HubConnectionBuilder()
      .withUrl(
        `${import.meta.env.VITE_API_URL.replace("/api", "")}/smarthomehub`,
        { withCredentials: true },
      )
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    connection
      .start()
      .then(() => console.log("Connected to SignalR Hub"))
      .catch((err) => console.error("SignalR Connection Error:", err));

    connection.on("RefreshDevices", () => refreshRef.current());
    connection.on("ReceiveTemperature", (deviceId: string, newTemp: number) => {
      tempRef.current(deviceId, newTemp);
    });

    return () => {
      connection.stop();
    };
  }, [user]);
}
