import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { Button } from "../components/Button";
import { Input } from "../components/Input";

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const resetSuccess = searchParams.get("reset") === "success";
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      await login({ email, password });
      navigate("/");
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        "Nie udało się zalogować. Sprawdź dane i spróbuj ponownie.";
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-md p-8 w-full max-w-md">
        <h1 className="text-2xl font-bold text-gray-900 mb-6">Zaloguj się</h1>
        {resetSuccess && (
          <div className="mb-4 p-3 bg-green-50 border border-green-200 text-green-800 text-sm rounded">
            Hasło zostało zmienione. Możesz się zalogować.
          </div>
        )}
        <form onSubmit={handleSubmit}>
          <Input
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoComplete="email"
          />
          <Input
            label="Hasło"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoComplete="current-password"
          />

          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 text-sm rounded">
              {error}
            </div>
          )}

          <Button type="submit" loading={loading} className="w-full">
            Zaloguj
          </Button>
        </form>

        <div className="mt-6 space-y-2 text-sm text-gray-600 text-center">
          <p>
            <Link
              to="/forgot-password"
              className="text-blue-600 hover:underline font-medium"
            >
              Nie pamiętam hasła
            </Link>
          </p>
          <p>
            Nie masz konta?{" "}
            <Link to="/register" className="text-blue-600 hover:underline font-medium">
              Zarejestruj się
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}