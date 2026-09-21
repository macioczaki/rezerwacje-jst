import { expect, test } from "@playwright/test";
import { loginAsAdmin, loginAsEmployee, uniqueName } from "./helpers";

test.describe("Panel admina — zarządzanie salami", () => {
  test("admin widzi link do panelu i może utworzyć salę", async ({ page }) => {
    await loginAsAdmin(page);

    await page.getByRole("link", { name: "Panel admina" }).click();
    await expect(page).toHaveURL(/\/admin\/rooms/);
    await expect(
      page.getByRole("heading", { name: "Zarządzanie salami" })
    ).toBeVisible();

    const roomName = uniqueName("Sala testowa");

    await page.getByRole("button", { name: /Dodaj salę/ }).click();
    await page.getByLabel("Nazwa").fill(roomName);
    await page.getByLabel("Lokalizacja").fill("Budynek testowy, I piętro");
    await page.getByLabel("Pojemność").fill("8");
    await page.getByLabel("Opis (opcjonalnie)").fill("Sala do testów E2E");
    await page.getByRole("button", { name: "Dodaj", exact: true }).click();

    // Modal się zamyka, nowa sala pojawia się w tabeli.
    await expect(page.getByRole("cell", { name: roomName })).toBeVisible();
  });

  test("admin może edytować salę", async ({ page }) => {
    await loginAsAdmin(page);

    const original = uniqueName("Sala edycja");
    const renamed = `${original}-v2`;

    // Utwórz salę
    await page.goto("/admin/rooms");
    await page.getByRole("button", { name: /Dodaj salę/ }).click();
    await page.getByLabel("Nazwa").fill(original);
    await page.getByLabel("Lokalizacja").fill("Budynek A");
    await page.getByLabel("Pojemność").fill("5");
    await page.getByRole("button", { name: "Dodaj", exact: true }).click();
    await expect(page.getByRole("cell", { name: original })).toBeVisible();

    // Edytuj — znajdź wiersz i kliknij "Edytuj"
    const row = page.getByRole("row", { name: new RegExp(original) });
    await row.getByRole("button", { name: "Edytuj" }).click();
    await page.getByLabel("Nazwa").fill(renamed);
    await page.getByRole("button", { name: "Zapisz" }).click();

    await expect(page.getByRole("cell", { name: renamed })).toBeVisible();
  });

  test("pracownik nie widzi linku do panelu admina", async ({ page }) => {
    await loginAsEmployee(page);
    await expect(page.getByRole("link", { name: "Panel admina" })).not.toBeVisible();
  });

  test("pracownik wpisujący /admin/rooms jest przekierowany na /", async ({ page }) => {
    await loginAsEmployee(page);
    await page.goto("/admin/rooms");
    await expect(page).toHaveURL(/\/$/);
  });
});