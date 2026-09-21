import { useState } from "react";
import { Link } from "react-router-dom";
import { authApi } from "../api/auth";
import { Button } from "../components/Button";
import { Input } from "../components/Input";

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      await authApi.forgotPassword({ email });
      setSubmitted(true);
    } catch {
      setError("Nie udało się wysłać wiadomości. Spróbuj ponownie.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-md p-8 w-full max-w-md">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">Reset hasła</h1>
        <p className="text-sm text-gray-600 mb-6">
          Podaj email powiązany z kontem. Wyślemy Ci link do ustawienia nowego hasła.
        </p>

        {submitted ? (
          <div className="bg-green-50 border border-green-200 text-green-800 p-4 rounded text-sm">
            <p className="font-medium mb-1">Sprawdź skrzynkę</p>
            <p>
              Jeśli konto z tym adresem istnieje, wysłaliśmy na nie link do
              zresetowania hasła. Link jest ważny przez 30 minut.
            </p>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <Input
              label="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
            />

            {error && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 text-sm rounded">
                {error}
              </div>
            )}

            <Button type="submit" loading={loading} className="w-full">
              Wyślij link
            </Button>
          </form>
        )}

        <p className="mt-6 text-sm text-gray-600 text-center">
          <Link to="/login" className="text-blue-600 hover:underline font-medium">
            Powrót do logowania
          </Link>
        </p>
      </div>
    </div>
  );
}