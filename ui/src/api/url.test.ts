import { describe, expect, it } from "vitest";
import { buildApiUrl } from "./url";

describe("buildApiUrl", () => {
  it("joins base url and relative path without duplicate slashes", () => {
    expect(buildApiUrl("/lok/open-hours")).toBe(
      "http://localhost:5013/lok/open-hours",
    );
  });

  it("accepts path without leading slash", () => {
    expect(buildApiUrl("lok/open-hours/1")).toBe(
      "http://localhost:5013/lok/open-hours/1",
    );
  });
});
