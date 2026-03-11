import type { ReactNode } from "react";
import { useRecoilValue } from "recoil";
import { sessionAtom } from "~/state/appState";
import type { AppRole } from "~/auth/roles";
import { hasAllRoles, hasAnyRole } from "~/auth/roles";

type RoleGuardProps = {
  roles: readonly AppRole[];
  match?: "any" | "all";
  fallback?: ReactNode;
  children: ReactNode;
};

export default function RoleGuard({
  roles,
  match = "any",
  fallback = null,
  children,
}: RoleGuardProps) {
  const session = useRecoilValue(sessionAtom);

  const isAllowed = match === "all"
    ? hasAllRoles(session, roles)
    : hasAnyRole(session, roles);

  return isAllowed ? <>{children}</> : <>{fallback}</>;
}
