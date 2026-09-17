import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { roomsApi } from "../api/rooms";
import { reservationsApi } from "../api/reservations";
import { formatTimeRange } from "../lib/dates";

const HOURS = Array.from({ length: 14 }, (_, i) => 7 + i); // 7:00 – 20:00

function todayIso(): string {
  const d = new Date();
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

export function AvailabilityPage() {
  const [roomId, setRoomId] = useState("");
  const [date, setDate] = useState(todayIso());

  const { data: rooms } = useQuery({
    queryKey: ["rooms", { activeOnly: true }],
    queryFn: () => roomsApi.getAll(true),
  });

  const { data: availability, isLoading } = useQuery({
    queryKey: ["availability", { roomId, date }],
    queryFn: () => reservationsApi.getAvailability(roomId, date),
    enabled: !!roomId && !!date,
  });

  // Buduje mapę: godzina -> zajęta?
  const busyHours = new Set<number>();
  availability?.busySlots.forEach((slot) => {
    const start = new Date(slot.startTime);
    const end = new Date(slot.endTime);
    for (let h = start.getHours(); h < end.getHours() + (end.getMinutes() > 0 ? 1 : 0); h++) {
      if (HOURS.includes(h)) busyHours.add(h);
    }
  });

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Dostępność sali</h1>

      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 mb-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label htmlFor="room-select" className="block text-sm font-medium text-gray-700 mb-1">
              Sala
            </label>
            <select
              id="room-select"
              value={roomId}
              onChange={(e) => setRoomId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">— wybierz salę —</option>
              {rooms?.map((room) => (
                <option key={room.id} value={room.id}>
                  {room.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label htmlFor="date-input" className="block text-sm font-medium text-gray-700 mb-1">
              Data
            </label>
            <input
              id="date-input"
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </div>
      </div>

      {!roomId && (
        <p className="text-gray-500">Wybierz salę i datę, żeby zobaczyć zajętość.</p>
      )}

      {roomId && isLoading && <p className="text-gray-500">Ładowanie...</p>}

      {roomId && availability && (
        <>
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden mb-6">
            <div className="grid grid-cols-7 sm:grid-cols-14 border-b border-gray-200">
              {HOURS.map((h) => (
                <div
                  key={h}
                  className={`px-1 py-2 text-center text-xs font-medium border-r border-gray-100 last:border-r-0 ${
                    busyHours.has(h)
                      ? "bg-red-100 text-red-700"
                      : "bg-green-50 text-green-700"
                  }`}
                >
                  {h}:00
                </div>
              ))}
            </div>
            <div className="px-4 py-3 text-sm text-gray-600 flex gap-4 flex-wrap">
              <span className="flex items-center gap-1">
                <span className="inline-block w-3 h-3 bg-green-50 border border-green-200 rounded"></span>
                Wolne
              </span>
              <span className="flex items-center gap-1">
                <span className="inline-block w-3 h-3 bg-red-100 border border-red-200 rounded"></span>
                Zajęte
              </span>
            </div>
          </div>

          <h2 className="text-lg font-semibold text-gray-900 mb-3">
            Zajęte przedziały ({availability.busySlots.length})
          </h2>

          {availability.busySlots.length === 0 ? (
            <p className="text-gray-500">Cały dzień wolny.</p>
          ) : (
            <div className="space-y-2">
              {availability.busySlots.map((slot, i) => (
                <div
                  key={i}
                  className="bg-white rounded-lg shadow-sm border border-gray-200 p-3 flex items-center justify-between"
                >
                  <div>
                    <p className="font-medium text-gray-900">{slot.title}</p>
                    <p className="text-sm text-gray-600">
                      {formatTimeRange(slot.startTime, slot.endTime)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </>
      )}
    </div>
  );
}