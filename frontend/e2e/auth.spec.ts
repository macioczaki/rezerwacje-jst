import { expect, test } from "@playwright/test";
import { ADMIN_EMAIL, loginAsAdmin } from "./helpers";

test.describe("Logowanie", () => {
  test("logowanie z poprawnymi danymi przekierowuje na listę sal", async ({ page }) => {
    await loginAsAdmin(page);

    // Powinien być nagłówek z emailem admina i lista sal.
    await expect(page.getByText(ADMIN_EMAIL)).toBeVisible();
    await expect(page.getByRole("heading", { name: "Dostępne sale" })).toBeVisible();
  });

  test("logowanie z błędnym hasłem pokazuje komunikat", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Email").fill(ADMIN_EMAIL);
    await page.getByLabel("Hasło").fill("ZleHaslo!");
    await page.getByRole("button", { name: "Zaloguj" }).click();

    await expect(page.getByText(/Nie udało się zalogować|Nieprawidłowe dane/)).toBeVisible();
    // Zostaje na stronie logowania.
    await expect(page).toHaveURL(/\/login/);
  });

  test("niezalogowany użytkownik jest przekierowany na /login", async ({ page }) => {
    await page.goto("/reservations");
    await expect(page).toHaveURL(/\/login/);
  });
});