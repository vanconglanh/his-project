import { test as base } from "@playwright/test";

export const TEST_USER = {
  email: "admin@phongkham.vn",
  password: "password123",
};

export const test = base.extend({
  // Future: add authenticated page fixture here
});

export { expect } from "@playwright/test";
