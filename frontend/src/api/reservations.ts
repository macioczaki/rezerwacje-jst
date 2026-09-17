import { api } from "./client";
import type {
  AvailabilitySlot,
  CreateReservationRequest,
  Reservation,
  UpdateReservationRequest,
} from "../types";

export interface ReservationFilters {
  roomId?: string;
  userId?: string;
  from?: string;
  to?: string;
  onlyActive?: boolean;
}

export const reservationsApi = {
  getAll: async (filters: ReservationFilters = {}): Promise<Reservation[]> => {
    const response = await api.get<Reservation[]>("/api/Reservations", {
      params: { onlyActive: true, ...filters },
    });
    return response.data;
  },

  getById: async (id: string): Promise<Reservation> => {
    const response = await api.get<Reservation>(`/api/Reservations/${id}`);
    return response.data;
  },

  create: async (data: CreateReservationRequest): Promise<Reservation> => {
    const response = await api.post<Reservation>("/api/Reservations", data);
    return response.data;
  },

  update: async (
    id: string,
    data: UpdateReservationRequest
  ): Promise<Reservation> => {
    const response = await api.put<Reservation>(
      `/api/Reservations/${id}`,
      data
    );
    return response.data;
  },

  cancel: async (id: string): Promise<void> => {
    await api.delete(`/api/Reservations/${id}`);
  },

  getAvailability: async (
    roomId: string,
    date: string
  ): Promise<AvailabilitySlot> => {
    const response = await api.get<AvailabilitySlot>(
      "/api/Reservations/availability",
      { params: { roomId, date } }
    );
    return response.data;
  },
};