import { useQuery } from "@tanstack/react-query";
import { roomsApi } from "../api/rooms";
import type { Room } from "../types";

function RoomCard({ room }: { room: Room }) {
  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 hover:shadow-md transition">
      <h3 className="text-lg font-semibold text-gray-900">{room.name}</h3>
      <p className="text-sm text-gray-600 mt-1">{room.location}</p>
      <div className="flex items-center gap-4 mt-3 text-sm text-gray-500">
        <span>👥 {room.capacity} osób</span>
      </div>
      {room.equipment && (
        <p className="text-xs text-gray-500 mt-2">🛠 {room.equipment}</p>
      )}
      {room.description && (
        <p className="text-sm text-gray-600 mt-2">{room.description}</p>
      )}
    </div>
  );
}

export function RoomsPage() {
  const { data: rooms, isLoading, error } = useQuery({
    queryKey: ["rooms", { activeOnly: true }],
    queryFn: () => roomsApi.getAll(true),
  });

  if (isLoading) {
    return <p className="text-gray-500">Ładowanie sal...</p>;
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
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Dostępne sale</h1>
      {rooms && rooms.length === 0 ? (
        <p className="text-gray-500">Brak aktywnych sal.</p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {rooms?.map((room) => (
            <RoomCard key={room.id} room={room} />
          ))}
        </div>
      )}
    </div>
  );
}