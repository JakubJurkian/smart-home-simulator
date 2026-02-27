import DeviceCard from "./DeviceCard";
import type { Device } from "../../types";

const DeviceSection = ({
  title,
  icon,
  devices,
  temps,
  onDelete,
  onToggle,
  onOpenLogs,
  onRename,
}: {
  title: string;
  icon: string;
  devices: Device[];
  temps: Record<string, number>;
  onDelete: (id: string) => void;
  onToggle: (id: string, action: "turn-on" | "turn-off") => void;
  onOpenLogs: (device: Device) => void;
  onRename: (id: string, newName: string) => void;
}) => {
  if (devices.length === 0) return null;

  return (
    <div className="mb-10 animate-fade-in-up">
      <h2 className="text-xl font-bold text-gray-700 mb-4 flex items-center gap-2 border-b pb-2">
        {icon} {title}{" "}
        <span className="text-sm font-normal text-gray-400">
          ({devices.length})
        </span>
      </h2>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {devices.map((device) => (
          <DeviceCard
            key={device.id}
            device={device}
            onDelete={onDelete}
            onToggle={onToggle}
            onOpenLogs={onOpenLogs}
            onRename={onRename}
            temp={temps[device.id]}
          />
        ))}
      </div>
    </div>
  );
};

export default DeviceSection;
