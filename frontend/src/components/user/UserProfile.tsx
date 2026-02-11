import { useState } from "react";
import { api } from "../../services/api";
import type { User } from "../../types";

const UserProfile = ({
  user,
  onBack,
  onUpdateUser,
  onDeleteAccount,
}: {
  user: User;
  onBack: () => void;
  onUpdateUser: (updatedUser: User) => void;
  onDeleteAccount: () => void;
}) => {
  const [username, setUsername] = useState(user.username);
  const [isChangingPass, setIsChangingPass] = useState(false);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [passError, setPassError] = useState<string | null>(null);
  const [confirmError, setConfirmError] = useState<string | null>(null);
  const [usernameError, setUsernameError] = useState<string | null>(null);

  const [msg, setMsg] = useState<{
    text: string;
    type: "success" | "error";
  } | null>(null);

  const isUsernameChanged = username.trim() !== user.username;
  const isUsernameValid = username.trim().length > 0;

  const isPasswordValid =
    !isChangingPass ||
    (newPassword.length >= 8 && newPassword === confirmPassword);

  const hasAnyChanges =
    isUsernameChanged || (isChangingPass && newPassword.length > 0);

  const canSave = hasAnyChanges && isUsernameValid && isPasswordValid;

  const handleUsernameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setUsername(e.target.value);
    if (usernameError) setUsernameError(null);
  };
  const handleUsernameBlur = () => {
    if (username.length === 0) {
      setUsernameError("Username cannot be empty");
    }
  };

  const handlePasswordChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setNewPassword(e.target.value);
    if (passError) setPassError(null);
  };

  const handlePasswordBlur = () => {
    if (newPassword.length > 0 && newPassword.length < 8) {
      setPassError("Password must be at least 8 characters long");
    }
  };

  const handleConfirmChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setConfirmPassword(e.target.value);
    if (confirmError) setConfirmError(null);
  };

  const handleConfirmBlur = () => {
    if (confirmPassword.length > 0 && newPassword !== confirmPassword) {
      setConfirmError("Passwords do not match");
    }
  };

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg(null);

    if (!canSave) return;

    try {
      const body: { username: string; password?: string } = { username };

      if (isChangingPass && newPassword) {
        body.password = newPassword;
      }

      const res = await api.users.update(user.id, body);

      if (!res.ok) throw new Error("Failed to update profile.");

      setMsg({ text: "Profile updated successfully!", type: "success" });
      onUpdateUser({ ...user, username });

      handleCancelPassChange();
    } catch (err) {
      console.log(err);
      setMsg({ text: "Error updating profile.", type: "error" });
    }
  };

  const handleCancelPassChange = () => {
    setIsChangingPass(false);
    setNewPassword("");
    setConfirmPassword("");
    setPassError(null);
    setConfirmError(null);
  };

  const handleDeleteAccount = async () => {
    if (
      !window.confirm(
        "ARE YOU SURE? This action cannot be undone. All your devices and data will be lost permanently.",
      )
    ) {
      return;
    }

    try {
      const res = await api.users.delete(user.id);
      if (!res.ok) throw new Error("Failed to delete account.");
      alert("Account deleted. Goodbye!");
      onDeleteAccount();
    } catch (err) {
      console.log(err);
      alert("Error deleting account.");
    }
  };

  return (
    <div className="max-w-2xl mx-auto bg-white p-6 sm:p-8 rounded-xl shadow-md border border-gray-200 mt-8">
      <div className="flex items-center justify-between mb-6 border-b pb-4">
        <h2 className="text-2xl font-bold text-gray-800">User Profile</h2>
        <button
          onClick={onBack}
          className="cursor-pointer text-gray-500 hover:text-gray-800 px-3 py-1 rounded border border-gray-300 hover:bg-gray-100 transition text-sm"
        >
          ← Back to Dashboard
        </button>
      </div>

      {msg && (
        <div
          className={`p-3 rounded mb-4 text-center ${
            msg.type === "success"
              ? "bg-green-100 text-green-700"
              : "bg-red-100 text-red-700"
          }`}
        >
          {msg.text}
        </div>
      )}

      <form onSubmit={handleUpdateProfile} className="space-y-6">
        <div>
          <label className="block text-sm font-medium text-gray-500 mb-1">
            Email (read-only)
          </label>
          <input
            type="email"
            value={user.email}
            disabled
            className="w-full p-3 bg-gray-100 border border-gray-300 rounded-lg text-gray-500 cursor-not-allowed"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Username
          </label>
          <input
            type="text"
            value={username}
            onChange={handleUsernameChange}
            className={`${
              usernameError
                ? "border-red-500 focus:ring-red-200"
                : "border-gray-300"
            } w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none`}
            minLength={1}
            onBlur={handleUsernameBlur}
            autoComplete="username"
          />
          {usernameError && (
            <p className="text-xs text-red-500 mt-1">{usernameError}</p>
          )}
        </div>

        <div className="pt-4 border-t border-gray-100">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Password
          </label>
          {!isChangingPass ? (
            <div className="flex gap-4 items-center">
              <input
                type="password"
                value="********"
                disabled
                className="w-full p-3 bg-gray-50 border border-gray-200 rounded-lg text-gray-400"
                autoComplete="current-password"
              />
              <button
                type="button"
                onClick={() => setIsChangingPass(true)}
                className="whitespace-nowrap px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-lg font-medium transition cursor-pointer"
              >
                Change Password
              </button>
            </div>
          ) : (
            <div className="bg-gray-50 p-4 rounded-lg border border-gray-200 space-y-3">
              <div>
                <input
                  type="password"
                  placeholder="New Password (min 8 chars)"
                  value={newPassword}
                  onChange={handlePasswordChange}
                  onBlur={handlePasswordBlur}
                  className={`w-full p-3 border rounded-lg bg-white ${
                    passError
                      ? "border-red-500 focus:ring-red-200"
                      : "border-gray-300"
                  }`}
                  minLength={8}
                  maxLength={128}
                  autoComplete="new-password"
                />
                {passError && (
                  <p className="text-xs text-red-500 mt-1">{passError}</p>
                )}
              </div>

              <div>
                <input
                  type="password"
                  placeholder="Confirm New Password"
                  value={confirmPassword}
                  onChange={handleConfirmChange}
                  onBlur={handleConfirmBlur}
                  className={`w-full p-3 border rounded-lg bg-white ${
                    confirmError
                      ? "border-red-500 focus:ring-red-200"
                      : "border-gray-300"
                  }`}
                  autoComplete="new-password"
                />
                {confirmError && (
                  <p className="text-xs text-red-500 mt-1">{confirmError}</p>
                )}
              </div>

              <button
                type="button"
                onClick={handleCancelPassChange}
                className="text-sm text-red-500 hover:underline cursor-pointer"
              >
                Cancel Password Change
              </button>
            </div>
          )}
        </div>

        <div className="pt-4">
          <button
            type="submit"
            className={`w-full py-3 ${
              canSave
                ? "bg-blue-600 hover:bg-blue-700 cursor-pointer shadow-md active:scale-[0.99]"
                : "bg-gray-300 text-gray-500 cursor-not-allowed"
            } text-white font-bold rounded-lg transition-transform`}
            disabled={!canSave}
          >
            Save Changes
          </button>
        </div>
      </form>

      <div className="mt-12 pt-6 border-t-2 border-red-100">
        <h3 className="text-lg font-bold text-red-600 mb-2">Danger Zone</h3>
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex flex-col sm:flex-row items-center justify-between gap-4">
          <p className="text-sm text-red-800">
            Once you delete your account, there is no going back. Please be
            certain.
          </p>
          <button
            onClick={handleDeleteAccount}
            className="whitespace-nowrap px-4 py-2 bg-white border border-red-300 text-red-600 hover:bg-red-600 hover:text-white rounded-lg font-bold transition shadow-sm cursor-pointer"
          >
            Delete Account
          </button>
        </div>
      </div>
    </div>
  );
};

export default UserProfile;
