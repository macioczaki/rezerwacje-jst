import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { roomsApi } from "../api/rooms";
import { Button } from "../components/Button";
import { Modal } from "../components/Modal";
import { RoomForm } from "../components/RoomForm";
import type { CreateRoomRequest, Room, UpdateRoomRequest } from "../types";

export function AdminRoomsPage() {
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Room | null>(null);

  const { data: rooms, isLoading, error } = useQuery({
    queryKey: ["rooms", { activeOnly: false }],
    queryFn: () => roomsApi.getAll(false),
  });

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["rooms"] });

  const createMutation = useMutation({
    mutationFn: (data: CreateRoomRequest) => roomsApi.create(data),
    onSuccess: () => {
      invalidate();
      closeModal();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateRoomRequest }) =>
      roomsApi.update(id, data),
    onSuccess: () => {
      invalidate();
      closeModal();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => roomsApi.delete(id),
    onSuccess: () => invalidate(),
  });

  const openCreate = () => {
    setEditing(null);
    setModalOpen(true);
  };

  const openEdit = (room: Room) => {
    setEditing(room);
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setEditing(null);
  };

  const handleSubmit = async (
    data: CreateRoomRequest | UpdateRoomRequest
  ) => {
    if (editing) {
      await updateMutation.mutateAsync({
        id: editing.id,
        data: data as UpdateRoomRequest,
      });
    } else {
      await createMutation.mutateAsync(data as CreateRoomRequest);
    }
  };

  const handleToggleActive = async (room: Room) => {
    if (room.isActive) {
      if (!confirm(`Dezaktywować salę „${room.name}"?`)) return;
      await deleteMutation.mutateAsync(room.id);
    } else {
      await updateMutation.mutateAsync({
        id: room.id,
        data: {
          name: room.name,
          location: room.location,
          capacity: room.capacity,
          description: room.description,
          equipment: room.equipment,
          isActive: true,
        },
      });
    }
  };

  if (isLoading) {
    return <p className="text-gray-500">Ładowanie...</p>;
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 text-red-700 p-4 rounded">
        Nie udało się pobrać listy sal.
      </div>
    );
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Zarządzanie salami</h1>
        <Button onClick={openCreate}>+ Dodaj salę</Button>
      </div>

      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-sm font-medium text-gray-700">
                Nazwa
              </th>
              <th className="text-left px-4 py-3 text-sm font-medium text-gray-700">
                Lokalizacja
              </th>
              <th className="text-left px-4 py-3 text-sm font-medium text-gray-700">
                Pojemność
              </th>
              <th className="text-left px-4 py-3 text-sm font-medium text-gray-700">
                Status
              </th>
              <th className="text-right px-4 py-3 text-sm font-medium text-gray-700">
                Akcje
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {rooms?.map((room) => (
              <tr key={room.id} className={room.isActive ? "" : "bg-gray-50"}>
                <td className="px-4 py-3 text-sm text-gray-900 font-medium">
                  {room.name}
                </td>
                <td className="px-4 py-3 text-sm text-gray-600">
                  {room.location}
                </td>
                <td className="px-4 py-3 text-sm text-gray-600">
                  {room.capacity}
                </td>
                <td className="px-4 py-3 text-sm">
                  {room.isActive ? (
                    <span className="inline-block px-2 py-1 text-xs font-medium bg-green-100 text-green-700 rounded">
                      Aktywna
                    </span>
                  ) : (
                    <span className="inline-block px-2 py-1 text-xs font-medium bg-gray-200 text-gray-600 rounded">
                      Nieaktywna
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 text-sm text-right space-x-2">
                  <button
                    onClick={() => openEdit(room)}
                    className="text-blue-600 hover:underline"
                  >
                    Edytuj
                  </button>
                  <button
                    onClick={() => handleToggleActive(room)}
                    className={
                      room.isActive
                        ? "text-red-600 hover:underline"
                        : "text-green-600 hover:underline"
                    }
                  >
                    {room.isActive ? "Dezaktywuj" : "Aktywuj"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Modal
        isOpen={modalOpen}
        onClose={closeModal}
        title={editing ? "Edytuj salę" : "Dodaj salę"}
      >
        <RoomForm
          initial={editing ?? undefined}
          onSubmit={handleSubmit}
          onCancel={closeModal}
        />
      </Modal>
    </div>
  );
}