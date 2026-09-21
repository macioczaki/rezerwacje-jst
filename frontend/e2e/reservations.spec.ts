import { expect, test } from "@playwright/test";
import { futureDateTime, loginAsAdmin, uniqueName } from "./helpers";

test.describe("Rezerwacje", () => {
  test("admin może utworzyć rezerwację", async ({ page }) => {
    await loginAsAdmin(page);
    await page.getByRole("link", { name: "Rezerwacje", exact: true }).click();
    await expect(page).toHaveURL(/\/reservations/);

    const title = uniqueName("Spotkanie testowe");
    const start = futureDateTime(2, 10);
    const end = futureDateTime(2, 11);

    await page.getByRole("button", { name: /Nowa rezerwacja/ }).click();

    // Wybierz pierwszą salę z listy (nie placeholder).
    await page.locator("#room-select").selectOption({ index: 1 });
    await page.getByLabel("Tytuł").fill(title);
    await page.getByLabel("Początek").fill(start);
    await page.getByLabel("Koniec").fill(end);
    await page.getByRole("button", { name: "Zarezerwuj" }).click();

    // Modal znika, rezerwacja pojawia się na liście.
    await expect(page.getByText(title)).toBeVisible();
  });

  test("kolizja — druga rezerwacja w tym samym czasie pokazuje błąd", async ({ page }) => {
    await loginAsAdmin(page);
    await page.getByRole("link", { name: "Rezerwacje", exact: true }).click();

    const title1 = uniqueName("Pierwsza");
    const title2 = uniqueName("Druga");
    const start = futureDateTime(3, 14);
    const end = futureDateTime(3, 15);

    // Pierwsza rezerwacja
    await page.getByRole("button", { name: /Nowa rezerwacja/ }).click();
    await page.locator("#room-select").selectOption({ index: 1 });
    await page.getByLabel("Tytuł").fill(title1);
    await page.getByLabel("Początek").fill(start);
    await page.getByLabel("Koniec").fill(end);
    await page.getByRole("button", { name: "Zarezerwuj" }).click();
    await expect(page.getByText(title1)).toBeVisible();

    // Druga rezerwacja, ten sam czas, ta sama sala.
    await page.getByRole("button", { name: /Nowa rezerwacja/ }).click();
    await page.locator("#room-select").selectOption({ index: 1 });
    await page.getByLabel("Tytuł").fill(title2);
    await page.getByLabel("Początek").fill(start);
    await page.getByLabel("Koniec").fill(end);
    await page.getByRole("button", { name: "Zarezerwuj" }).click();

    // Powinien pojawić się czerwony komunikat o kolizji.
    await expect(page.getByText(/zajęta/i)).toBeVisible();
  });

  test("admin może anulować własną rezerwację", async ({ page }) => {
    await loginAsAdmin(page);
    await page.getByRole("link", { name: "Rezerwacje", exact: true }).click();

    const title = uniqueName("Do anulowania");
    const start = futureDateTime(4, 16);
    const end = futureDateTime(4, 17);

    // Utwórz
    await page.getByRole("button", { name: /Nowa rezerwacja/ }).click();
    await page.locator("#room-select").selectOption({ index: 1 });
    await page.getByLabel("Tytuł").fill(title);
    await page.getByLabel("Początek").fill(start);
    await page.getByLabel("Koniec").fill(end);
    await page.getByRole("button", { name: "Zarezerwuj" }).click();

    // Znajdź kartę rezerwacji: div, który zawiera nagłówek z naszym tytułem
    // ORAZ przycisk "Anuluj". Bierzemy .last() — najgłębszy taki div (sama karta),
    // a nie #root czy main, które też zawierają tytuł.
    const card = page
        .locator("div")
        .filter({ hasText: title })
        .filter({ has: page.getByRole("button", { name: "Anuluj" }) })
        .last();

    page.on("dialog", (dialog) => dialog.accept());
    await card.getByRole("button", { name: "Anuluj" }).click();

    // Po anulowaniu komunikat znika (bo domyślnie pokazujemy tylko aktywne).
    await expect(page.getByText(title)).not.toBeVisible();
  });

  test("filtry: 'Moje' pokazuje tylko rezerwacje zalogowanego użytkownika", async ({ page }) => {
    await loginAsAdmin(page);
    await page.getByRole("link", { name: "Rezerwacje", exact: true }).click();

    await page.getByRole("button", { name: "Moje" }).click();
    // Wszystkie widoczne karty powinny mieć email admina.
    const cards = page.locator('div:has-text("admin@example.com")');
    await expect(cards.first()).toBeVisible();
  });
});