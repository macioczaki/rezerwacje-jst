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