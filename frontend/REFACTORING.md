# Frontend Refactoring Summary

## Overview

The frontend was refactored from a single "god component" (`App.tsx`) architecture into a modular structure with custom hooks, reusable components, layout elements, and page-level organization.

---

## New Directory Structure

```
src/
├── App.tsx                          # Slim entry (23 lines)
├── hooks/
│   ├── useAuth.ts                   # Authentication state & session management
│   ├── useDevices.ts                # Device CRUD, search, temps, derived lists
│   ├── useRooms.ts                  # Room CRUD operations
│   ├── useSignalR.ts                # Real-time SignalR connection
│   └── useToast.ts                  # Timed toast notification state
├── pages/
│   └── DashboardPage.tsx            # Main dashboard orchestrating hooks & UI
├── components/
│   ├── layout/
│   │   ├── AppLayout.tsx            # Authenticated app shell (header + toast + views)
│   │   └── Header.tsx               # Top bar with navigation & user info
│   ├── common/
│   │   ├── Toast.tsx                # Dismissible error toast notification
│   │   ├── ErrorBanner.tsx          # Persistent system warning banner
│   │   ├── LoadingSpinner.tsx       # Full-page loading spinner
│   │   ├── SearchBar.tsx            # Reusable search input
│   │   └── EmptyState.tsx           # Empty device list placeholder
│   ├── devices/
│   │   ├── DeviceCard.tsx           # Individual device card (updated)
│   │   ├── DeviceForm.tsx           # Add device form
│   │   └── DeviceSection.tsx        # Groups device cards by type (new)
│   ├── auth/
│   │   └── AuthForm.tsx             # Login/register form (updated)
│   ├── modals/
│   │   └── MaintenanceModal.tsx     # Service logs modal
│   ├── rooms/
│   │   └── RoomManager.tsx          # Room management panel
│   └── user/
│       └── UserProfile.tsx          # User profile settings
```

---

## Custom Hooks

### `useAuth`
- Manages `user` state, session loading flag
- Handles session restoration on mount via cookie check (`/users/me`)
- Exposes `login`, `logout`, `updateUser` callbacks

### `useDevices`
- Manages `devices`, `searchTerm`, `temps`, `globalError`
- Provides `fetchDevices`, `addDevice`, `toggleDevice`, `deleteDevice`
- Derives `lightbulbs` and `sensors` arrays via `useMemo`
- Uses a `ref` for `searchTerm` to avoid stale closures in stable callbacks

### `useRooms`
- Manages `rooms` state
- Provides `fetchRooms`, `addRoom`, `renameRoom`, `deleteRoom`
- Returns promises from mutation functions for cross-hook coordination

### `useSignalR`
- Establishes a SignalR hub connection when a user is authenticated
- Uses callback refs (synced via `useEffect`) so the connection only reconnects when `user` changes — not on every callback identity change
- Cleans up connection on unmount

### `useToast`
- Manages a timed toast message with auto-dismiss
- Properly cleans up timers on unmount
- Exposes `show`, `dismiss`, and `message`

---

## Quality Fixes

### 1. Direct Prop Mutation in `DeviceCard`
**Before:** `device.name = editedName.trim()` — mutated the prop object directly, breaking React's immutability model.

**After:** Added `onRename` callback prop. After a successful API rename, the parent is notified to refresh device state properly.

### 2. Redundant Ternary in `AuthForm`
**Before:** `disabled={isLoading ? true : false}`

**After:** `disabled={isLoading}`

### 3. SignalR Reconnecting on Every Search Keystroke
**Before:** `searchTerm` was in the SignalR `useEffect` dependency array, causing the WebSocket connection to tear down and reconnect on every keystroke.

**After:** Callbacks are stored in refs (synced via `useEffect` to satisfy lint rules). The SignalR connection only depends on `user`, remaining stable across search changes.

### 4. Missing `useMemo` for Derived Arrays
**Before:** `lightbulbs` and `sensors` were re-computed on every render via inline `.filter()`.

**After:** Both are wrapped in `useMemo` keyed on `devices`.

### 5. `App.tsx` Reduced from ~300 to 23 Lines
All state management, side effects, and UI composition were extracted into hooks, pages, and layout components. `App.tsx` now only handles auth state and top-level conditional rendering.

---

## Type Updates

- Added `onRename: (id: string, newName: string) => void` to `DeviceCardProps` interface in `types.ts`
