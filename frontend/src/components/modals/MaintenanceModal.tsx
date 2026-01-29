import { useState, useEffect } from "react";
import { api } from "../../services/api";
import type { MaintenanceLog } from "../../types";

const MaintenanceModal = ({
  deviceId,
  deviceName,
  onClose,
}: {
  deviceId: string;
  deviceName: string;
  onClose: () => void;
}) => {
  const [logs, setLogs] = useState<MaintenanceLog[]>([]);
  const [title, setTitle] = useState("");
  const [desc, setDesc] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [editingId, setEditingId] = useState<string | null>(null);

  useEffect(() => {
    api.logs
      .getByDevice(deviceId)
      .then((res) => res.json())
      .then((data) => {
        setLogs(Array.isArray(data) ? data : []);
        setIsLoading(false);
      })
      .catch((err) => console.error("Error loading logs:", err));
  }, [deviceId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title || !desc) return;

    try {
      if (editingId) {
        const res = await api.logs.update(editingId, {
          title,
          description: desc,
        });

        if (!res.ok) throw new Error("Failed to update");

        setLogs(
          logs.map((log) =>
            log.id === editingId ? { ...log, title, description: desc } : log
          )
        );
        setEditingId(null);
      } else {
        const res = await api.logs.add({ deviceId, title, description: desc });

        if (!res.ok) throw new Error("Failed to add");

        const responseData = await res.json();

        const newLog: MaintenanceLog = {
          id: responseData.id,
          deviceId,
          title,
          description: desc,
          createdAt: new Date().toISOString(),
        };
        setLogs([newLog, ...logs]);
      }
      setTitle("");
      setDesc("");
    } catch (err) {
      console.log(err);
      alert("Operation failed. Try again.");
    }
  };

  const startEdit = (log: MaintenanceLog) => {
    setEditingId(log.id);
    setTitle(log.title);
    setDesc(log.description);
  };

  const cancelEdit = () => {
    setEditingId(null);
    setTitle("");
    setDesc("");
  };

  const handleDeleteLog = async (id: string) => {
    if (!confirm("Remove this entry?")) return;
    try {
      await api.logs.delete(id);
      setLogs(logs.filter((l) => l.id !== id));

      if (editingId === id) cancelEdit();
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4 backdrop-blur-sm">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-lg overflow-hidden flex flex-col max-h-[80vh]">
        <div className="bg-gray-50 p-4 border-b flex justify-between items-center">
          <h3 className="font-bold text-lg text-gray-700 flex items-center gap-2">
            🛠️ Service History{" "}
            <span className="text-sm font-normal text-gray-500">
              for {deviceName}
            </span>
          </h3>
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-700 text-2xl leading-none cursor-pointer"
          >
            &times;
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-4 space-y-4 bg-gray-50/50">
          {isLoading ? (
            <p className="text-center text-gray-500">Loading history...</p>
          ) : logs.length === 0 ? (
            <p className="text-center text-gray-400 italic">
              No service logs yet.
            </p>
          ) : (
            logs.map((log) => (
              <div
                key={log.id}
                className={`p-3 rounded-lg border shadow-sm relative group transition-colors ${
                  editingId === log.id
                    ? "bg-blue-50 border-blue-300 ring-1 ring-blue-300"
                    : "bg-white border-gray-200"
                }`}
              >
                <div className="flex justify-between items-start pr-16">
                  <h4 className="font-bold text-gray-800 text-sm">
                    {log.title}
                  </h4>
                  <span className="text-xs text-gray-400">
                    {new Date(log.createdAt).toLocaleDateString()}
                  </span>
                </div>
                <p className="text-gray-600 text-sm mt-1 whitespace-pre-wrap">
                  {log.description}
                </p>

                <div className="absolute top-2 right-2 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  <button
                    onClick={() => startEdit(log)}
                    className="p-1 text-blue-400 hover:text-blue-600 hover:bg-blue-50 rounded cursor-pointer"
                    title="Edit"
                  >
                    ✏️
                  </button>
                  <button
                    onClick={() => handleDeleteLog(log.id)}
                    className="p-1 text-red-400 hover:text-red-600 hover:bg-red-50 rounded cursor-pointer"
                    title="Delete"
                  >
                    🗑️
                  </button>
                </div>
              </div>
            ))
          )}
        </div>

        <form
          onSubmit={handleSubmit}
          className={`p-4 border-t ${editingId ? "bg-blue-50" : "bg-white"}`}
        >
          {editingId && (
            <div className="flex justify-between items-center mb-2 text-xs font-bold text-blue-600 uppercase tracking-wide">
              <span>Editing Entry</span>
              <button
                type="button"
                onClick={cancelEdit}
                className="text-gray-500 hover:text-gray-800 underline cursor-pointer"
              >
                Cancel
              </button>
            </div>
          )}
          <div className="mb-2">
            <input
              placeholder="Log Title (e.g. Battery Change)"
              className="w-full p-2 border rounded text-sm mb-2"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
            />
            <textarea
              placeholder="Description..."
              className="w-full p-2 border rounded text-sm"
              rows={2}
              value={desc}
              onChange={(e) => setDesc(e.target.value)}
              required
            />
          </div>
          <button
            type="submit"
            className={`w-full py-2 rounded font-medium text-sm transition text-white cursor-pointer ${
              editingId
                ? "bg-green-600 hover:bg-green-700"
                : "bg-blue-600 hover:bg-blue-700"
            }`}
          >
            {editingId ? "💾 Save Changes" : "➕ Add Entry"}
          </button>
        </form>
      </div>
    </div>
  );
};

export default MaintenanceModal;
