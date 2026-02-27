import type { User } from "../../types";

type ViewType = "dashboard" | "profile";

const Header = ({
  user,
  view,
  onViewChange,
  onLogout,
}: {
  user: User;
  view: ViewType;
  onViewChange: (view: ViewType) => void;
  onLogout: () => void;
}) => (
  <div className="flex flex-col sm:flex-row justify-between items-center mb-8 gap-4 sm:gap-0 bg-white p-4 rounded-xl shadow-sm border border-gray-100">
    <h1 className="text-3xl font-bold text-blue-600 flex items-center gap-2 flex-col sm:flex-row">
      Smart Home{" "}
      <span className="text-gray-400 text-lg font-normal hidden sm:block mt-1.5">
        | {user.username}
      </span>
    </h1>

    <div className="flex gap-2 w-full sm:w-auto justify-center">
      <button
        onClick={() =>
          onViewChange(view === "dashboard" ? "profile" : "dashboard")
        }
        className="px-4 py-2 bg-blue-50 text-blue-700 hover:bg-blue-100 rounded-lg font-medium transition cursor-pointer"
      >
        {view === "dashboard" ? "Profile" : "Dashboard"}
      </button>
      <button
        onClick={onLogout}
        className="px-4 py-2 bg-gray-200 hover:bg-gray-300 text-gray-700 rounded-lg font-medium transition cursor-pointer"
      >
        Logout
      </button>
    </div>
  </div>
);

export default Header;
