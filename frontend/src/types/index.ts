export type UserRole = "Employee" | "Admin";

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  email: string;
  role: UserRole;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface Room {
  id: string;
  name: string;
  location: string;
  capacity: number;
  description?: string | null;
  equipment?: string | null;
  isActive: boolean;
}

export interface CreateRoomRequest {
  name: string;
  location: string;
  capacity: number;
  description?: string | null;
  equipment?: string | null;
}

export interface UpdateRoomRequest extends CreateRoomRequest {
  isActive: boolean;
}

export type ReservationStatus = "Active" | "Cancelled";

export interface Reservation {
  id: string;
  roomId: string;
  roomName: string;
  userId: string;
  userEmail: string;
  title: string;
  startTime: string;
  endTime: string;
  status: ReservationStatus;
  createdAt: string;
}

export interface CreateReservationRequest {
  roomId: string;
  title: string;
  startTime: string;
  endTime: string;
}

export interface UpdateReservationRequest {
  title: string;
  startTime: string;
  endTime: string;
}

export interface BusySlot {
  startTime: string;
  endTime: string;
  title: string;
}

export interface AvailabilitySlot {
  roomId: string;
  date: string;
  busySlots: BusySlot[];
}

export type AuditAction = "Created" | "Updated" | "Deleted";

export interface AuditLogEntry {
  id: string;
  userId: string | null;
  userEmail: string | null;
  entityType: string;
  entityId: string;
  action: AuditAction;
  changes: string;
  timestamp: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}