import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { roomsApi } from "../api/rooms";
import { localInputToUtcIso } from "../lib/dates";
import type { CreateReservationRequest } from "../types";
import { Button } from "./Button";
import { Input } from "./Input";

interface ReservationFormProps {
  onSubmit: (data: CreateReservationRequest) => Promise<void>;
  onCancel: () => void;
}

export function ReservationForm({ onSubmit, onCancel }: ReservationFormProps) {
  const { data: rooms } = useQuery({
    queryKey: ["rooms", { activeOnly: true }],
    queryFn: () => roomsApi.getAll(true),
  });

  const [roomId, setRoomId] = useState("");
  const [title, setTitle] = useState("");
  const [start, setStart] = useState("");
  const [end, setEnd] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!roomId) {
      setError("Wybierz salę.");
      return;
    }
    if (!start || !end) {
      setError("Podaj czas rozpoczęcia i zakończenia.");
      return;
    }

    const startIso = localInputToUtcIso(start);
    const endIso = localInputToUtcIso(end);

    if (new Date(endIso) <= new Date(startIso)) {
      setError("Data zakończenia musi być późniejsza niż data rozpoczęcia.");
      return;
    }

    setLoading(true);
    try {
      await onSubmit({ roomId, title, startTime: startIso, endTime: endIso });
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { error?: string } } })?.response?.data
          ?.error ?? "Nie udało się utworzyć rezerwacji.";
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div className="mb-4">
        <label htmlFor="room-select" className="block text-sm font-medium text-gray-700 mb-1">
          Sala
        </label>
        <select
          id="room-select"
          value={roomId}
          onChange={(e) => setRoomId(e.target.value)}
          required
          className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        >
          <option value="">— wybierz salę —</option>
          {rooms?.map((room) => (
            <option key={room.id} value={room.id}>
              {room.name} ({room.location}, {room.capacity} osób)
            </option>
          ))}
        </select>
      </div>

      <Input
        label="Tytuł"
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        required
        placeholder="np. Spotkanie zespołu"
      />
      <Input
        label="Początek"
        type="datetime-local"
        value={start}
        onChange={(e) => setStart(e.target.value)}
        required
      />
      <Input
        label="Koniec"
        type="datetime-local"
        value={end}
        onChange={(e) => setEnd(e.target.value)}
        required
      />

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 text-red-700 text-sm rounded">
          {error}
        </div>
      )}

      <div className="flex justify-end gap-2 mt-6">
        <Button type="button" variant="secondary" onClick={onCancel}>
          Anuluj
        </Button>
        <Button type="submit" loading={loading}>
          Zarezerwuj
        </Button>
      </div>
    </form>
  );
}