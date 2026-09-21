import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { authApi } from "../api/auth";
import { Button } from "../components/Button";
import { Input } from "../components/Input";

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token") ?? "";

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (password.length < 6) {
      setError("Hasło musi mieć co najmniej 6 znaków.");
      return;
    }
    if (password !== confirm) {
      setError("Hasła nie są identyczne.");
      return;
    }

    setLoading(true);

    try {
      await authApi.resetPassword({ token, newPassword: password });
      navigate("/login?reset=success");
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        "Nie udało się zresetować hasła.";
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  if (!token) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-lg shadow-md p-8 w-full max-w-md text-center">
          <h1 className="text-xl font-bold text-gray-900 mb-3">Nieprawidłowy link</h1>
          <p className="text-sm text-gray-600 mb-6">
            Link jest niekompletny. Poproś o nowy link do resetu hasła.
          </p>
          <Link
            to="/forgot-password"
            className="text-blue-600 hover:underline font-medium"
          >
            Poproś o nowy link
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-md p-8 w-full max-w-md">
        <h1 className="text-2xl font-bold text-gray-900 mb-6">Ustaw nowe hasło</h1>

        <form onSubmit={handleSubmit}>
          <Input
            label="Nowe hasło"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={6}
            autoComplete="new-password"
          />
          <Input
            label="Powtórz nowe hasło"
            type="password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            required
            minLength={6}
            autoComplete="new-password"
          />

          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 text-sm rounded">
              {error}
            </div>
          )}

          <Button type="submit" loading={loading} className="w-full">
            Ustaw hasło
          </Button>
        </form>

        <p className="mt-6 text-sm text-gray-600 text-center">
          <Link to="/login" className="text-blue-600 hover:underline font-medium">
            Powrót do logowania
          </Link>
        </p>
      </div>
    </div>
  );
}