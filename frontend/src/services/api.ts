import { API_URL } from "../config";
import type {
  AddDeviceRequest,
  LoginRequest,
  RegisterRequest,
  User,
} from "../types";
// --- HELPERS ---

const getOptions = (method: string, body?: unknown): RequestInit => {
  const options: RequestInit = {
    method,
    headers: {},
    credentials: "include",
  };

  if (body) {
    options.headers = { "Content-Type": "application/json" };
    options.body = JSON.stringify(body);
  }

  return options;
};

// generic request fn
const request = (
  endpoint: string,
  method: string = "GET",
  body?: unknown,
): Promise<Response> => {
  return fetch(`${API_URL}${endpoint}`, getOptions(method, body));
};

// --- API SERVICE ---

export const api = {
  auth: {
    login: (credentials: LoginRequest) =>
      request("/users/login", "POST", credentials),
    register: (data: RegisterRequest) =>
      request("/users/register", "POST", data),
    logout: () => request("/users/logout", "POST"),
  },

  users: {
    update: (id: string, data: Partial<User>) =>
      request(`/users/${id}`, "PUT", data),
    delete: (id: string) => request(`/users/${id}`, "DELETE"),
  },

  devices: {
    getAll: () => request("/devices"),
    add: (data: AddDeviceRequest) => request(`/devices`, "POST", data),
    delete: (id: string) => request(`/devices/${id}`, "DELETE"),
    toggle: (id: string, action: "turn-on" | "turn-off") =>
      request(`/devices/${id}/${action}`, "PUT"),
    rename: (id: string, newName: string) =>
      request(`/devices/${id}/name`, "PATCH", newName),
  },

  rooms: {
    getAll: () => request("/rooms"),
    add: (name: string) => request("/rooms", "POST", name),
    delete: (id: string) => request(`/rooms/${id}`, "DELETE"),
    rename: (id: string, newName: string) =>
      request(`/rooms/${id}`, "PUT", newName),
  },

  logs: {
    getByDevice: (deviceId: string) => request(`/logs/${deviceId}`),
    add: (data: { deviceId: string; title: string; description: string }) =>
      request("/logs", "POST", data),
    update: (id: string, data: { title: string; description: string }) =>
      request(`/logs/${id}`, "PUT", data),
    delete: (id: string) => request(`/logs/${id}`, "DELETE"),
  },
};
