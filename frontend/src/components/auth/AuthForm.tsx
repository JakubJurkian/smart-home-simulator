import { useState } from "react";
import { api } from "../../services/api";
import type { User } from "../../types";

const AuthForm = ({
  onLoginSuccess,
}: {
  onLoginSuccess: (user: User) => void;
}) => {
  const [isLoginMode, setIsLoginMode] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [username, setUsername] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);

    try {
      let response;

      if (isLoginMode) {
        response = await api.auth.login({ email, password });
      } else {
        response = await api.auth.register({ username, email, password });
      }

      if (response.status >= 500) {
        throw new Error("Server is currently unavailable.");
      }

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.message || "Invalid credentials.");
      }

      if (isLoginMode) {
        onLoginSuccess(data);
      } else {
        alert("Registration successful! Please log in.");
        setIsLoginMode(true);
      }
    } catch (err: unknown) {
      if (err instanceof Error) {
        if (err.message === "Failed to fetch") {
          setError("Unable to connect to the server. Try again later.");
        } else {
          setError(err.message);
        }
      } else {
        setError("Something went wrong! Try again later.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-100 p-4">
      <div className="bg-white p-6 sm:p-8 rounded-xl shadow-lg w-full max-w-md border border-gray-200">
        <h2 className="text-2xl font-bold text-center mb-6 text-blue-600">
          {isLoginMode ? "🔐 Log In" : "📝 Register"}
        </h2>
        {error && (
          <div className="bg-red-100 text-red-700 p-3 rounded-lg mb-4 text-sm text-center font-medium border border-red-200">
            {error}
          </div>
        )}
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          {!isLoginMode && (
            <input
              type="text"
              placeholder="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="p-3 border rounded-lg w-full"
              required
            />
          )}
          <input
            type="email"
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="p-3 border rounded-lg w-full"
            required
          />
          <input
            type="password"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="p-3 border rounded-lg w-full"
            required
          />
          <button
            type="submit"
            disabled={isLoading}
            className={`py-3 rounded-lg font-bold text-white transition-colors w-full ${
              isLoading
                ? "cursor-not-allowed bg-blue-400"
                : "cursor-pointer bg-blue-600 hover:bg-blue-700"
            }`}
          >
            {isLoading
              ? "Connecting..."
              : isLoginMode
              ? "Log In"
              : "Create Account"}
          </button>
        </form>
        <p className="text-center mt-6 text-sm text-gray-600">
          <button
            onClick={() => setIsLoginMode(!isLoginMode)}
            className={`${
              isLoading
                ? "cursor-not-allowed"
                : "cursor-pointer hover:underline"
            }  text-blue-600 font-semibold `}
            disabled={isLoading ? true : false}
          >
            {isLoginMode ? "Register here" : "Log in here"}
          </button>
        </p>
      </div>
    </div>
  );
};

export default AuthForm;
