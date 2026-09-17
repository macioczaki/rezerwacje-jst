import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { reservationsApi } from "../api/reservations";
import { useAuth } from "../auth/AuthContext";
import { Button } from "../components/Button";
import { Modal } from "../components/Modal";
import { ReservationCard } from "../components/ReservationCard";
import { ReservationForm } from "../components/ReservationForm";
import type { CreateReservationRequest } from "../types";

type Filter = "all" | "mine";

export function ReservationsPage() {
  const { user, isAdmin } = useAuth();
  const queryClient = useQueryClient();
  const [modalOpen, setModalOpen] = useState(false);
  const [filter, setFilter] = useState<Filter>("all");
  const [showCancelled, setShowCancelled] = useState(false);

  const { data: reservations, isLoading, error } = useQuery({
    queryKey: ["reservations", { filter, showCancelled, email: user?.email }],
    queryFn: () => {
      const params: { onlyActive?: boolean } = {
        onlyActive: !showCancelled,
      };
      if (filter === "mine") {
        // Backend filtruje po userId; my znamy tylko email, więc
        // filtrujemy po stronie klienta. W produkcji użylibyśmy userId z tokenu.
      }
      return reservationsApi.getAll(params);
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateReservationRequest) =>
      reservationsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reservations"] });
      setModalOpen(false);
    },
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => reservationsApi.cancel(id),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["reservations"] }),
  });

  const handleCancel = (id: string) => {
    if (confirm("Anulować tę rezerwację?")) {
      cancelMutation.mutate(id);
    }
  };

  const visible = reservations?.filter((r) => {
    if (filter === "mine" && r.userEmail !== user?.email) return false;
    return true;
  });

  return (
    <div>
      <div className="flex items-center justify-between mb-6 flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Rezerwacje</h1>
        <Button onClick={() => setModalOpen(true)}>+ Nowa rezerwacja</Button>
      </div>

      <div className="flex items-center gap-4 mb-4 text-sm flex-wrap">
        <div className="flex items-center gap-2">
          <span className="text-gray-600">Widok:</span>
          <button
            onClick={() => setFilter("all")}
            className={
              filter === "all"
                ? "font-medium text-blue-600"
                : "text-gray-600 hover:underline"
            }
          >
            Wszystkie
          </button>
          <span className="text-gray-300">|</span>
          <button
            onClick={() => setFilter("mine")}
            className={
              filter === "mine"
                ? "font-medium text-blue-600"
                : "text-gray-600 hover:underline"
            }
          >
            Moje
          </button>
        </div>

        <label className="flex items-center gap-2 text-gray-600 cursor-pointer">
          <input
            type="checkbox"
            checked={showCancelled}
            onChange={(e) => setShowCancelled(e.target.checked)}
            className="w-4 h-4"
          />
          Pokaż anulowane
        </label>
      </div>

      {isLoading && <p className="text-gray-500">Ładowanie...</p>}

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 p-4 rounded">
          Nie udało się pobrać listy rezerwacji.
        </div>
      )}

      {!isLoading && !error && visible && visible.length === 0 && (
        <p className="text-gray-500">Brak rezerwacji.</p>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {visible?.map((r) => (
          <ReservationCard
            key={r.id}
            reservation={r}
            canCancel={isAdmin || r.userEmail === user?.email}
            onCancel={handleCancel}
          />
        ))}
      </div>

      <Modal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        title="Nowa rezerwacja"
      >
        <ReservationForm
          onSubmit={async (data) => {
            await createMutation.mutateAsync(data);
          }}
          onCancel={() => setModalOpen(false)}
        />
      </Modal>
    </div>
  );
}