import { useAuth } from "./auth/AuthContext";

function App() {
  const { isAuthenticated, user, logout } = useAuth();

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg shadow-md p-8 max-w-md w-full">
        <h1 className="text-3xl font-bold text-blue-700 mb-4">Rezerwacje JST</h1>
        {isAuthenticated ? (
          <>
            <p className="text-gray-700 mb-2">
              Zalogowany jako <strong>{user?.email}</strong>
            </p>
            <p className="text-sm text-gray-500 mb-4">Rola: {user?.role}</p>
            <button
              onClick={logout}
              className="w-full bg-red-600 hover:bg-red-700 text-white font-medium py-2 px-4 rounded transition"
            >
              Wyloguj
            </button>
          </>
        ) : (
          <p className="text-gray-500">Nie jesteś zalogowany.</p>
        )}
      </div>
    </div>
  );
}

export default App;