import { Page } from "@playwright/test";

export const ADMIN_EMAIL = "admin@example.com";
export const ADMIN_PASSWORD = "Admin123!";
export const EMPLOYEE_EMAIL = "pracownik@example.com";
export const EMPLOYEE_PASSWORD = "Pracownik123!";

export async function loginAs(page: Page, email: string, password: string) {
  await page.goto("/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Hasło").fill(password);
  await page.getByRole("button", { name: "Zaloguj" }).click();
  // Po logowaniu użytkownik ląduje na liście sal.
  await page.waitForURL("**/");
}

export async function loginAsAdmin(page: Page) {
  await loginAs(page, ADMIN_EMAIL, ADMIN_PASSWORD);
}

export async function loginAsEmployee(page: Page) {
  await loginAs(page, EMPLOYEE_EMAIL, EMPLOYEE_PASSWORD);
}

/**
 * Zwraca unikalną nazwę z sufiksem timestamp — żeby testy nie kolidowały ze sobą.
 */
export function uniqueName(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 1000)}`;
}

/**
 * Zwraca datę w formacie akceptowanym przez <input type="datetime-local">,
 * przesuniętą o podaną liczbę dni w przyszłość.
 */
export function futureDateTime(daysAhead: number, hour: number, minute = 0): string {
  const d = new Date();
  d.setDate(d.getDate() + daysAhead);
  d.setHours(hour, minute, 0, 0);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(hour)}:${pad(minute)}`;
}