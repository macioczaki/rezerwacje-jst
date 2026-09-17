export function formatDateTime(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString("pl-PL", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatDate(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleDateString("pl-PL", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  });
}

export function formatTimeRange(startIso: string, endIso: string): string {
  const start = new Date(startIso);
  const end = new Date(endIso);
  const sameDay =
    start.getFullYear() === end.getFullYear() &&
    start.getMonth() === end.getMonth() &&
    start.getDate() === end.getDate();

  if (sameDay) {
    return `${formatDate(startIso)}, ${start.toLocaleTimeString("pl-PL", {
      hour: "2-digit",
      minute: "2-digit",
    })}–${end.toLocaleTimeString("pl-PL", {
      hour: "2-digit",
      minute: "2-digit",
    })}`;
  }

  return `${formatDateTime(startIso)} – ${formatDateTime(endIso)}`;
}

/**
 * Konwertuje wartość z <input type="datetime-local"> (czas lokalny) na ISO UTC.
 */
export function localInputToUtcIso(localValue: string): string {
  const d = new Date(localValue);
  return d.toISOString();
}

/**
 * Konwertuje ISO UTC na format akceptowany przez <input type="datetime-local">
 * (yyyy-MM-ddTHH:mm w czasie lokalnym).
 */
export function utcIsoToLocalInput(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(
    d.getHours()
  )}:${pad(d.getMinutes())}`;
}

export function isFuture(iso: string): boolean {
  return new Date(iso).getTime() > Date.now();
}