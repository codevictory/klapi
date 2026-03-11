import type { Session } from "~/state/appState";

export const APP_ROLES = {
  admin: "admin",
  lok: "lok",
} as const;

export type AppRole = (typeof APP_ROLES)[keyof typeof APP_ROLES];

export const hasAnyRole = (
  session: Session | null,
  roles: readonly AppRole[],
): boolean => {
  if (!session || roles.length === 0) {
    return false;
  }

  return roles.some((role) => session.roles.includes(role));
};

export const hasAllRoles = (
  session: Session | null,
  roles: readonly AppRole[],
): boolean => {
  if (!session || roles.length === 0) {
    return false;
  }

  return roles.every((role) => session.roles.includes(role));
};
