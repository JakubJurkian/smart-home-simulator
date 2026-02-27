import { useAuth } from "./hooks/useAuth";

import LoadingSpinner from "./components/common/LoadingSpinner";
import AuthForm from "./components/auth/AuthForm";
import AppLayout from "./components/layout/AppLayout";

function App() {
  const { user, isSessionLoading, login, logout, updateUser } = useAuth();

  if (isSessionLoading) {
    return <LoadingSpinner label="Loading Smart Home..." />;
  }

  if (!user) {
    return <AuthForm onLoginSuccess={login} />;
  }

  return (
    <AppLayout user={user} onLogout={logout} onUpdateUser={updateUser} />
  );
}

export default App;
