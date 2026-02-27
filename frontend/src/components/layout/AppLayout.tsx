import { useState } from "react";

import type { User } from "../../types";
import { useToast } from "../../hooks/useToast";

import Header from "./Header";
import Toast from "../common/Toast";
import UserProfile from "../user/UserProfile";
import DashboardPage from "../../pages/DashboardPage";

type ViewType = "dashboard" | "profile";

const AppLayout = ({
  user,
  onLogout,
  onUpdateUser,
}: {
  user: User;
  onLogout: () => void;
  onUpdateUser: (updated: User) => void;
}) => {
  const [view, setView] = useState<ViewType>("dashboard");
  const { message: toastMessage, show: showToast, dismiss: dismissToast } = useToast();

  return (
    <div className="min-h-screen min-w-[320px] bg-gray-50 p-4 sm:p-8 font-sans text-gray-800 relative">
      <Toast message={toastMessage} onDismiss={dismissToast} />

      <div className="max-w-5xl mx-auto">
        <Header
          user={user}
          view={view}
          onViewChange={setView}
          onLogout={onLogout}
        />

        {view === "profile" ? (
          <UserProfile
            user={user}
            onBack={() => setView("dashboard")}
            onUpdateUser={onUpdateUser}
            onDeleteAccount={onLogout}
          />
        ) : (
          <DashboardPage
            user={user}
            onLogout={onLogout}
            showError={showToast}
          />
        )}
      </div>
    </div>
  );
};

export default AppLayout;
