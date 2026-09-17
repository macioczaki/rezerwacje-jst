import { formatTimeRange, isFuture } from "../lib/dates";
import type { Reservation } from "../types";

interface ReservationCardProps {
  reservation: Reservation;
  canCancel: boolean;
  onCancel: (id: string) => void;
}

export function ReservationCard({
  reservation,
  canCancel,
  onCancel,
}: ReservationCardProps) {
  const isCancelled = reservation.status === "Cancelled";
  const future = isFuture(reservation.startTime);
  const showCancel = canCancel && !isCancelled && future;

  return (
    <div
      className={`bg-white rounded-lg shadow-sm border border-gray-200 p-4 ${
        isCancelled ? "opacity-60" : ""
      }`}
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1">
          <h3 className="font-semibold text-gray-900">{reservation.title}</h3>
          <p className="text-sm text-gray-600 mt-1">
            📍 {reservation.roomName}
          </p>
          <p className="text-sm text-gray-600">
            🕒 {formatTimeRange(reservation.startTime, reservation.endTime)}
          </p>
          <p className="text-xs text-gray-500 mt-2">
            Zgłosił(a): {reservation.userEmail}
          </p>
        </div>
        <div className="flex flex-col items-end gap-2">
          {isCancelled ? (
            <span className="inline-block px-2 py-1 text-xs font-medium bg-red-100 text-red-700 rounded">
              Anulowana
            </span>
          ) : (
            <span className="inline-block px-2 py-1 text-xs font-medium bg-green-100 text-green-700 rounded">
              Aktywna
            </span>
          )}
          {showCancel && (
            <button
              onClick={() => onCancel(reservation.id)}
              className="text-sm text-red-600 hover:underline"
            >
              Anuluj
            </button>
          )}
        </div>
      </div>
    </div>
  );
}