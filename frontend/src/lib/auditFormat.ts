import type { AuditLogEntry } from "../types";

export function formatAction(action: string): string {
  switch (action) {
    case "Created":
      return "Utworzono";
    case "Updated":
      return "Zaktualizowano";
    case "Deleted":
      return "Usunięto";
    default:
      return action;
  }
}

export function formatEntityType(entityType: string): string {
  switch (entityType) {
    case "Room":
      return "Sala";
    case "Reservation":
      return "Rezerwacja";
    case "User":
      return "Użytkownik";
    default:
      return entityType;
  }
}

/**
 * Zmienia surowy JSON z pola `changes` w czytelną listę zmian.
 */
export function formatChanges(entry: AuditLogEntry): string {
  try {
    const data = JSON.parse(entry.changes) as Record<string, unknown>;

    if (entry.action === "Updated" && "fields" in data) {
      const fields = data.fields as Record<
        string,
        { before: unknown; after: unknown }
      >;
      return Object.entries(fields)
        .map(
          ([field, change]) =>
            `${field}: "${String(change.before)}" → "${String(change.after)}"`
        )
        .join(", ");
    }

    // Created lub Deleted — pokazujemy wybrane pola
    const summary = Object.entries(data)
      .filter(([key]) => !["Id", "CreatedAt", "PasswordHash"].includes(key))
      .slice(0, 4)
      .map(([key, value]) => `${key}: ${String(value)}`)
      .join(", ");
    return summary || "—";
  } catch {
    return entry.changes;
  }
}