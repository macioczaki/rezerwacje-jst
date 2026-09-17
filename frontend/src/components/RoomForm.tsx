import { useState } from "react";
import type { CreateRoomRequest, Room, UpdateRoomRequest } from "../types";
import { Button } from "./Button";
import { Input } from "./Input";

interface RoomFormProps {
  initial?: Room;
  onSubmit: (data: CreateRoomRequest | UpdateRoomRequest) => Promise<void>;
  onCancel: () => void;
}

export function RoomForm({ initial, onSubmit, onCancel }: RoomFormProps) {
  const isEdit = !!initial;
  const [name, setName] = useState(initial?.name ?? "");
  const [location, setLocation] = useState(initial?.location ?? "");
  const [capacity, setCapacity] = useState(initial?.capacity ?? 10);
  const [description, setDescription] = useState(initial?.description ?? "");
  const [equipment, setEquipment] = useState(initial?.equipment ?? "");
  const [isActive, setIsActive] = useState(initial?.isActive ?? true);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const base: CreateRoomRequest = {
        name,
        location,
        capacity,
        description: description || null,
        equipment: equipment || null,
      };

      if (isEdit) {
        await onSubmit({ ...base, isActive } as UpdateRoomRequest);
      } else {
        await onSubmit(base);
      }
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        "Nie udało się zapisać sali.";
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <Input
        label="Nazwa"
        value={name}
        onChange={(e) => setName(e.target.value)}
        required
      />
      <Input
        label="Lokalizacja"
        value={location}
        onChange={(e) => setLocation(e.target.value)}
        required
      />
      <Input
        label="Pojemność"
        type="number"
        min={1}
        value={capacity}
        onChange={(e) => setCapacity(Number(e.target.value))}
        required
      />
      <Input
        label="Opis (opcjonalnie)"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
      />
      <Input
        label="Wyposażenie (opcjonalnie)"
        value={equipment}
        onChange={(e) => setEquipment(e.target.value)}
      />

      {isEdit && (
        <div className="mb-4 flex items-center gap-2">
          <input
            id="is-active"
            type="checkbox"
            checked={isActive}
            onChange={(e) => setIsActive(e.target.checked)}
            className="w-4 h-4"
          />
          <label htmlFor="is-active" className="text-sm text-gray-700">
            Sala aktywna
          </label>
        </div>
      )}

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
          {isEdit ? "Zapisz" : "Dodaj"}
        </Button>
      </div>
    </form>
  );
}