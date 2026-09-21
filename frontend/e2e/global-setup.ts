import { request } from "@playwright/test";

const API_URL = process.env.API_URL ?? "http://localhost:5045";

/**
 * Global setup — czyści bazę z rezerwacji przed uruchomieniem testów.
 * Bez tego testy z drugiego uruchomienia kolidują z danymi z pierwszego.
 */
async function globalSetup() {
  const context = await request.newContext({ baseURL: API_URL });

  // 1. Zaloguj się jako admin.
  const loginRes = await context.post("/api/Auth/login", {
    data: { email: "admin@example.com", password: "Admin123!" },
  });

  if (!loginRes.ok()) {
    throw new Error(
      `Global setup: nie udało się zalogować (${loginRes.status()}). Sprawdź, czy backend działa na ${API_URL}.`
    );
  }

  const { accessToken } = (await loginRes.json()) as { accessToken: string };
  const headers = { Authorization: `Bearer ${accessToken}` };

  // 2. Pobierz wszystkie rezerwacje (także anulowane).
  const listRes = await context.get("/api/Reservations?onlyActive=false", { headers });
  const reservations = (await listRes.json()) as Array<{ id: string; title: string; status: string }>;

  // 3. Anuluj każdą aktywną.
  let cancelled = 0;
  for (const r of reservations) {
    if (r.status === "Cancelled") continue;
    const del = await context.delete(`/api/Reservations/${r.id}`, { headers });
    if (del.ok()) cancelled++;
  }

  console.log(`Global setup: anulowano ${cancelled} rezerwacji (z ${reservations.length} znalezionych).`);

  await context.dispose();
}

export default globalSetup;