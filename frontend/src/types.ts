export interface Room {
  id: string;
  name: string;
  userId: string;
}

export interface MaintenanceLog {
  id: string;
  deviceId: string;
  title: string;
  description: string;
  createdAt: string;
}

export interface Device {
  id: string;
  name: string;
  roomId: string;
  room: Room;
  type: string;
  isOn?: boolean;
  currentTemperature?: number;
}

export interface User {
  id: string;
  username: string;
  email: string;
}

export interface DeviceCardProps {
  device: Device;
  onDelete: (id: string) => void;
  onToggle: (id: string, action: "turn-on" | "turn-off") => void;
  onOpenLogs: (device: Device) => void;
  temp?: number;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface AddDeviceRequest {
  name: string;
  roomId: string;
  type: string;
}

export interface LogRequest {
  deviceId?: string;
  title: string;
  description: string;
}
