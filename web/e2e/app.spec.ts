import { test, expect } from "@playwright/test";

// These tests expect the API to be running on http://localhost:5080
// (dotnet run --project api/SeeSharp.Api, or the compose stack) with
// Postgres up, and the dev bearer token to be "dev-secret-token".

test("dashboard shows the yearly summary", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "This year" })).toBeVisible();
  await expect(page.getByText(/Income: \d/)).toBeVisible();
  await expect(page.getByRole("columnheader", { name: "Income" })).toBeVisible();
});

test("clients page lists clients and adds a new one", async ({ page }) => {
  await page.goto("/clients");
  await expect(page.getByRole("heading", { name: "Clients" })).toBeVisible();
  await expect(page.getByRole("cell", { name: "Acme Co" })).toBeVisible();

  const name = `E2E Client ${Date.now()}`;
  await page.getByPlaceholder("Client name").fill(name);
  await page.getByRole("button", { name: "Add" }).click();
  await expect(page.getByRole("cell", { name })).toBeVisible();
});

test("invoices page lists invoices with status", async ({ page }) => {
  await page.goto("/invoices");
  await expect(page.getByRole("heading", { name: "Invoices" })).toBeVisible();
  await expect(page.getByRole("cell", { name: "INV-1001" })).toBeVisible();
  await expect(page.getByRole("cell", { name: "Paid" }).first()).toBeVisible();
});

test("expenses page lists seeded expenses", async ({ page }) => {
  await page.goto("/expenses");
  await expect(page.getByRole("heading", { name: "Expenses" })).toBeVisible();
  await expect(page.getByRole("cell", { name: "JetBrains license" })).toBeVisible();
  await expect(page.getByRole("cell", { name: "85.50" })).toBeVisible();
});

test("navigation moves between pages", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("link", { name: "Clients" }).click();
  await expect(page).toHaveURL(/\/clients$/);
  await page.getByRole("link", { name: "Dashboard" }).click();
  await expect(page).toHaveURL(/\/$/);
});

async function createClient(page, name: string) {
  await page.goto("/clients");
  await page.getByPlaceholder("Client name").fill(name);
  await page.getByRole("button", { name: "Add" }).click();
  await expect(page.getByRole("cell", { name })).toBeVisible();
}

async function createInvoice(page, clientName: string, number: string) {
  await page.goto("/invoices");
  await page.getByRole("button", { name: "Create invoice" }).click();
  const modal = page.getByRole("dialog", { name: "Create invoice" });
  await modal.getByRole("combobox").selectOption({ label: clientName });
  await modal.getByPlaceholder("Invoice number").fill(number);
  await modal.getByPlaceholder("Description").first().fill("Work");
  await modal.getByPlaceholder("Qty").first().fill("2");
  await modal.getByPlaceholder("Price").first().fill("100");
  await modal.getByRole("button", { name: "Create invoice" }).click();
  await expect(modal).toBeHidden();
  return page.locator("tr", { hasText: number });
}

test("create a draft invoice", async ({ page }) => {
  const client = `Invoice Client ${Date.now()}`;
  const number = `INV-${Date.now()}`;
  await createClient(page, client);
  const row = await createInvoice(page, client, number);
  await expect(row.getByRole("cell", { name: "Draft" })).toBeVisible();
});

test("edit and delete a draft invoice", async ({ page }) => {
  const client = `Edit Client ${Date.now()}`;
  const number = `EDIT-${Date.now()}`;
  await createClient(page, client);
  let row = await createInvoice(page, client, number);

  await row.getByRole("button", { name: "Edit" }).click();
  const modal = page.getByRole("dialog", { name: "Edit draft invoice" });
  await modal.getByPlaceholder("Invoice number").fill(`${number}-updated`);
  await modal.getByRole("button", { name: "Save changes" }).click();
  await expect(modal).toBeHidden();

  row = page.locator("tr", { hasText: `${number}-updated` });
  await expect(row).toBeVisible();

  page.on("dialog", (dialog) => dialog.accept());
  await row.getByRole("button", { name: "Delete" }).click();
  await expect(row).toBeHidden();
});

test("cancel a draft invoice", async ({ page }) => {
  const client = `Cancel Client ${Date.now()}`;
  const number = `CANCEL-${Date.now()}`;
  await createClient(page, client);
  const row = await createInvoice(page, client, number);
  await row.getByRole("button", { name: "Cancel" }).click();
  await expect(row.getByRole("cell", { name: "Cancelled" })).toBeVisible();
});

test("create an expense", async ({ page }) => {
  await page.goto("/expenses");
  await page.getByRole("button", { name: "Create expense" }).click();
  const modal = page.getByRole("dialog", { name: "Create expense" });
  const description = `Hosting ${Date.now()}`;
  await modal.getByPlaceholder("Description").fill(description);
  await modal.getByPlaceholder("Amount").fill("19.99");
  await modal.getByPlaceholder("Vendor").fill("Vercel");
  await modal.getByRole("combobox").selectOption({ label: "Software" });
  await modal.getByRole("button", { name: "Create expense" }).click();
  await expect(modal).toBeHidden();
  await expect(page.getByRole("cell", { name: description })).toBeVisible();
});
