import { api } from "./client";
import type { CreateRoomRequest, Room, UpdateRoomRequest } from "../types";

export const roomsApi = {
  getAll: async (activeOnly = true): Promise<Room[]> => {
    const response = await api.get<Room[]>("/api/Rooms", {
      params: { activeOnly },
    });
    return response.data;
  },

  getById: async (id: string): Promise<Room> => {
    const response = await api.get<Room>(`/api/Rooms/${id}`);
    return response.data;
  },

  create: async (data: CreateRoomRequest): Promise<Room> => {
    const response = await api.post<Room>("/api/Rooms", data);
    return response.data;
  },

  update: async (id: string, data: UpdateRoomRequest): Promise<Room> => {
    const response = await api.put<Room>(`/api/Rooms/${id}`, data);
    return response.data;
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/api/Rooms/${id}`);
  },
};