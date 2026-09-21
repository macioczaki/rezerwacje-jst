import { Link, NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { Button } from "./Button";

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `px-3 py-2 rounded-md text-sm font-medium transition ${
    isActive
      ? "bg-blue-100 text-blue-700"
      : "text-gray-700 hover:bg-gray-100"
  }`;

export function Layout() {
  const { user, logout, isAdmin } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate("/login");
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200 shadow-sm">
        <div className="max-w-6xl mx-auto px-4 py-3 flex items-center justify-between">
          <div className="flex items-center gap-6">
            <Link to="/" className="text-xl font-bold text-blue-700">
              Rezerwacje JST
            </Link>
            <nav className="hidden md:flex items-center gap-1">
              <NavLink to="/" end className={navLinkClass}>
                Sale
              </NavLink>
              <NavLink to="/reservations" className={navLinkClass}>
                Rezerwacje
              </NavLink>
              <NavLink to="/availability" className={navLinkClass}>
                Dostępność
              </NavLink>
              {isAdmin && (
                <>
                  <NavLink to="/admin/rooms" className={navLinkClass}>
                    Panel admina
                  </NavLink>
                  <NavLink to="/admin/audit-log" className={navLinkClass}>
                    Audit log
                  </NavLink>
                </>
              )}
            </nav>
          </div>

          <div className="flex items-center gap-3">
            <div className="text-right hidden sm:block">
              <p className="text-sm font-medium text-gray-900">{user?.email}</p>
              <p className="text-xs text-gray-500">{user?.role}</p>
            </div>
            <Button variant="secondary" onClick={handleLogout}>
              Wyloguj
            </Button>
          </div>
        </div>
      </header>

      <main className="max-w-6xl mx-auto px-4 py-6">
        <Outlet />
      </main>
    </div>
  );
}