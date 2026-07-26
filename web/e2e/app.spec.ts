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
