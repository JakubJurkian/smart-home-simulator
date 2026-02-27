import { useState, useEffect } from "react";
import type { User, Device } from "../types";

import { useDevices } from "../hooks/useDevices";
import { useRooms } from "../hooks/useRooms";
import { useSignalR } from "../hooks/useSignalR";

import SearchBar from "../components/common/SearchBar";
import ErrorBanner from "../components/common/ErrorBanner";
import EmptyState from "../components/common/EmptyState";
import RoomManager from "../components/rooms/RoomManager";
import DeviceForm from "../components/devices/DeviceForm";
import DeviceSection from "../components/devices/DeviceSection";
import MaintenanceModal from "../components/modals/MaintenanceModal";

const DashboardPage = ({
  user,
  onLogout,
  showError,
}: {
  user: User;
  onLogout: () => void;
  showError: (msg: string) => void;
}) => {
  const {
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
  } = useDevices(showError, onLogout);

  const { rooms, fetchRooms, addRoom, renameRoom, deleteRoom } = useRooms();

  const [selectedDeviceForLogs, setSelectedDeviceForLogs] =
    useState<Device | null>(null);

  // Fetch data on mount
  useEffect(() => {
    fetchDevices();
    fetchRooms();
  }, [fetchDevices, fetchRooms]);

  // Real-time updates via SignalR
  useSignalR(
    user,
    () => fetchDevices(),
    (deviceId, newTemp) =>
      setTemps((prev) => ({ ...prev, [deviceId]: newTemp })),
  );

  // Cross-hook coordination: room changes affect devices
  const handleRenameRoom = async (id: string, newName: string) => {
    const ok = await renameRoom(id, newName);
    if (ok) fetchDevices();
  };

  const handleDeleteRoom = async (id: string) => {
    const ok = await deleteRoom(id);
    if (ok) fetchDevices();
  };

  const handleRenameDevice = () => {
    // DeviceCard handles the API call; just refresh to sync state
    fetchDevices();
  };

  return (
    <>
      <SearchBar value={searchTerm} onChange={handleSearchChange} />

      {globalError && <ErrorBanner message={globalError} />}

      <RoomManager
        rooms={rooms}
        onAdd={addRoom}
        onDelete={handleDeleteRoom}
        onRename={handleRenameRoom}
      />

      <DeviceForm onAdd={addDevice} rooms={rooms} />

      <DeviceSection
        title="Lighting"
        icon="💡"
        devices={lightbulbs}
        temps={temps}
        onDelete={deleteDevice}
        onToggle={toggleDevice}
        onOpenLogs={setSelectedDeviceForLogs}
        onRename={handleRenameDevice}
      />

      <DeviceSection
        title="Temperature Sensors"
        icon="🌡️"
        devices={sensors}
        temps={temps}
        onDelete={deleteDevice}
        onToggle={toggleDevice}
        onOpenLogs={setSelectedDeviceForLogs}
        onRename={handleRenameDevice}
      />

      {devices.length === 0 && !globalError && (
        <EmptyState hasSearch={searchTerm.length > 0} />
      )}

      {selectedDeviceForLogs && (
        <MaintenanceModal
          deviceId={selectedDeviceForLogs.id}
          deviceName={selectedDeviceForLogs.name}
          onClose={() => setSelectedDeviceForLogs(null)}
        />
      )}
    </>
  );
};

export default DashboardPage;
