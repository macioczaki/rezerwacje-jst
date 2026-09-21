import { api } from "./client";
import type { AuditLogEntry } from "../types";

export interface AuditLogFilters {
  entityType?: string;
  entityId?: string;
  userId?: string;
  from?: string;
  to?: string;
  limit?: number;
}

export const auditLogApi = {
  getAll: async (filters: AuditLogFilters = {}): Promise<AuditLogEntry[]> => {
    const response = await api.get<AuditLogEntry[]>("/api/AuditLog", {
      params: filters,
    });
    return response.data;
  },
};