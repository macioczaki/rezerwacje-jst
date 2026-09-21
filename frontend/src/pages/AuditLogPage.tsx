import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { auditLogApi } from "../api/auditLog";
import { formatAction, formatChanges, formatEntityType } from "../lib/auditFormat";
import { formatDateTime } from "../lib/dates";

const ENTITY_TYPES = [
  { value: "", label: "Wszystkie" },
  { value: "Room", label: "Sale" },
  { value: "Reservation", label: "Rezerwacje" },
  { value: "User", label: "Użytkownicy" },
];

export function AuditLogPage() {
  const [entityType, setEntityType] = useState("");

  const { data: entries, isLoading, error } = useQuery({
    queryKey: ["audit-log", { entityType }],
    queryFn: () =>
      auditLogApi.getAll({
        entityType: entityType || undefined,
        limit: 200,
      }),
  });

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Audit log</h1>

      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-6">
        <label
          htmlFor="entity-type"
          className="block text-sm font-medium text-gray-700 mb-1"
        >
          Typ encji
        </label>
        <select
          id="entity-type"
          value={entityType}
          onChange={(e) => setEntityType(e.target.value)}
          className="px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        >
          {ENTITY_TYPES.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>

      {isLoading && <p className="text-gray-500">Ładowanie...</p>}

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 p-4 rounded">
          Nie udało się pobrać logów.
        </div>
      )}

      {entries && entries.length === 0 && (
        <p className="text-gray-500">Brak wpisów.</p>
      )}

      {entries && entries.length > 0 && (
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-sm font-medium text-gray-700">
                  Czas
                </th>
                <th className="text-left px-4 py-3 text-sm font-medium text-gray-700">
                  Użytkownik
                </th>
                <th className="text-left px-4 py-3 text-sm font-medium text-gray-700">
                  Encja
                </th>
                <th className="text-left px-4 py-3 text-sm font-medium text-gray-700">
                  Akcja
                </th>
                <th className="text-left px-4 py-3 text-sm font-medium text-gray-700">
                  Szczegóły
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {entries.map((entry) => (
                <tr key={entry.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-sm text-gray-600 whitespace-nowrap">
                    {formatDateTime(entry.timestamp)}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-900">
                    {entry.userEmail ?? (
                      <span className="text-gray-400 italic">system</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-700">
                    {formatEntityType(entry.entityType)}
                  </td>
                  <td className="px-4 py-3 text-sm">
                    <span
                      className={
                        entry.action === "Created"
                          ? "text-green-700"
                          : entry.action === "Updated"
                          ? "text-blue-700"
                          : "text-red-700"
                      }
                    >
                      {formatAction(entry.action)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-600 max-w-md truncate">
                    {formatChanges(entry)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}