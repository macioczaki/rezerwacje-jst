import { createContext, useContext, useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { authApi } from "../api/auth";
import type { AuthResponse, LoginRequest, RegisterRequest, UserRole } from "../types";

interface AuthUser {
  email: string;
  role: UserRole;
  expiresAt: string;
}

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const TOKEN_KEY = "accessToken";
const USER_KEY = "user";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  });

  const applyAuth = (response: AuthResponse) => {
    const authUser: AuthUser = {
      email: response.email,
      role: response.role,
      expiresAt: response.expiresAt,
    };
    localStorage.setItem(TOKEN_KEY, response.accessToken);
    localStorage.setItem(USER_KEY, JSON.stringify(authUser));
    setUser(authUser);
  };

  const login = async (data: LoginRequest) => {
    const response = await authApi.login(data);
    applyAuth(response);
  };

  const register = async (data: RegisterRequest) => {
    const response = await authApi.register(data);
    applyAuth(response);
  };

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    setUser(null);
  };

  // Nasłuchuj na zmiany localStorage z innych kart przeglądarki
  useEffect(() => {
    const handler = (e: StorageEvent) => {
      if (e.key === USER_KEY) {
        setUser(e.newValue ? (JSON.parse(e.newValue) as AuthUser) : null);
      }
    };
    window.addEventListener("storage", handler);
    return () => window.removeEventListener("storage", handler);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: !!user,
      isAdmin: user?.role === "Admin",
      login,
      register,
      logout,
    }),
    [user]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}