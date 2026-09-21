import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import type { AuthResponse } from "../types";

const baseURL = import.meta.env.VITE_API_URL ?? "http://localhost:5045";

export const api = axios.create({
  baseURL,
  headers: { "Content-Type": "application/json" },
});

// Klucze localStorage — muszą się zgadzać z tymi w AuthContext
const TOKEN_KEY = "accessToken";
const REFRESH_TOKEN_KEY = "refreshToken";
const USER_KEY = "user";

// --- Request interceptor: dodaje Bearer do każdego żądania ---
api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// --- Response interceptor: auto-refresh przy 401 ---
//
// Gdy backend zwróci 401 (access token wygasł), próbujemy raz odświeżyć token
// przez /api/Auth/refresh i powtórzyć oryginalne żądanie. Jeśli refresh też
// się nie uda (refresh token wygasł / został unieważniony), czyścimy stan
// i przekierowujemy na /login.
//
// Wiele równoległych żądań: tylko PIERWSZE wywołuje refresh, reszta czeka
// na ten sam Promise. Bez tego każdy 401 wywołałby osobny refresh.

let refreshPromise: Promise<string> | null = null;

interface RetryableConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

const clearAuthAndRedirect = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  if (window.location.pathname !== "/login") {
    window.location.href = "/login";
  }
};

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableConfig | undefined;
    const status = error.response?.status;

    // Nie próbujemy odświeżać przy 401 z samego /refresh ani gdy już raz próbowaliśmy
    const isRefreshEndpoint = originalRequest?.url?.includes("/api/Auth/refresh");
    const isAuthEndpoint =
      originalRequest?.url?.includes("/api/Auth/login") ||
      originalRequest?.url?.includes("/api/Auth/register");

    if (status !== 401 || !originalRequest || originalRequest._retry || isRefreshEndpoint || isAuthEndpoint) {
      return Promise.reject(error);
    }

    const storedRefreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    if (!storedRefreshToken) {
      clearAuthAndRedirect();
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    try {
      // Równoległe żądania czekają na ten sam refresh
      if (!refreshPromise) {
        refreshPromise = api
          .post<AuthResponse>("/api/Auth/refresh", { refreshToken: storedRefreshToken })
          .then((res) => {
            const { accessToken, refreshToken, accessTokenExpiresAt, email, role } = res.data;
            localStorage.setItem(TOKEN_KEY, accessToken);
            localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
            localStorage.setItem(
              USER_KEY,
              JSON.stringify({ email, role, expiresAt: accessTokenExpiresAt })
            );
            return accessToken;
          })
          .finally(() => {
            refreshPromise = null;
          });
      }

      const newAccessToken = await refreshPromise;
      originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
      return api(originalRequest);
    } catch (refreshError) {
      clearAuthAndRedirect();
      return Promise.reject(refreshError);
    }
  }
);