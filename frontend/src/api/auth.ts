import { api } from "./client";
import type { AuthResponse, LoginRequest, RefreshTokenRequest, RegisterRequest } from "../types";

export const authApi = {
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>("/api/Auth/login", data);
    return response.data;
  },

  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>("/api/Auth/register", data);
    return response.data;
  },

  refresh: async (data: RefreshTokenRequest): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>("/api/Auth/refresh", data);
    return response.data;
  },

  logout: async (data: RefreshTokenRequest): Promise<void> => {
    await api.post("/api/Auth/logout", data);
  },
};