import { FormEvent, useEffect, useState } from "react";
import { createUser, deleteUser, queryUsers, updateUser, type User } from "~/api";
import { useT } from "~/i18n";

type Mode = "create" | "edit";

export default function Management() {
  const t = useT();
  const [users, setUsers] = useState<User[]>([]);
  const [mode, setMode] = useState<Mode>("create");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [isAdmin, setIsAdmin] = useState(false);
  const [selectedUsername, setSelectedUsername] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    document.title = t("management.title");
  }, [t]);

  useEffect(() => {
    void loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const items = await queryUsers();
      setUsers(items);
      setError("");
    } catch {
      setError(t("management.loadError"));
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setMode("create");
    setUsername("");
    setPassword("");
    setDisplayName("");
    setIsAdmin(false);
    setSelectedUsername("");
  };

  const onEdit = (user: User) => {
    setMode("edit");
    setUsername(user.username);
    setPassword("");
    setDisplayName(user.displayName);
    setIsAdmin(user.isAdmin);
    setSelectedUsername(user.username);
    setError("");
  };

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();

    if (!username.trim() || !displayName.trim() || (mode === "create" && !password.trim())) {
      setError(t("management.requiredFields"));
      return;
    }

    try {
      if (mode === "create") {
        await createUser({
          username: username.trim(),
          password: password,
          displayName: displayName.trim(),
          isAdmin,
        });
      } else {
        await updateUser(selectedUsername, {
          password: password.trim() ? password : undefined,
          displayName: displayName.trim(),
          isAdmin,
        });
      }

      resetForm();
      await loadUsers();
    } catch {
      setError(t("management.saveError"));
    }
  };

  const onDelete = async (targetUsername: string) => {
    try {
      await deleteUser(targetUsername);
      if (selectedUsername === targetUsername) {
        resetForm();
      }
      await loadUsers();
    } catch {
      setError(t("management.deleteError"));
    }
  };

  return (
    <main className="w-full px-4">
      <h1>{t("management.heading")}</h1>

      <form onSubmit={onSubmit} className="mx-auto mt-4 w-full max-w-2xl space-y-3 rounded-md border border-[#C99763] bg-[#FFF7EE] p-4">
        <h2 className="text-lg font-semibold text-[#4C250E]">
          {mode === "create" ? t("management.create") : t("management.edit")}
        </h2>

        <label htmlFor="management-username" className="block text-left">
          {t("management.username")}
          <input
            id="management-username"
            type="text"
            value={username}
            disabled={mode === "edit"}
            onChange={(event) => setUsername(event.target.value)}
            className="mt-1 block w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-4 py-2 focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
        </label>

        <label htmlFor="management-display-name" className="block text-left">
          {t("management.displayName")}
          <input
            id="management-display-name"
            type="text"
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            className="mt-1 block w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-4 py-2 focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
        </label>

        <label htmlFor="management-password" className="block text-left">
          {t("management.password")}
          <input
            id="management-password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            className="mt-1 block w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-4 py-2 focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
        </label>

        <label htmlFor="management-is-admin" className="flex items-center gap-2 text-left">
          <input
            id="management-is-admin"
            type="checkbox"
            checked={isAdmin}
            onChange={(event) => setIsAdmin(event.target.checked)}
          />
          {t("management.isAdmin")}
        </label>

        <div className="flex gap-2">
          <button
            type="submit"
            className="rounded-md border border-[#A56C38] bg-[#8E4F24] px-4 py-2 text-[#F5D1A9] transition-colors duration-200 hover:bg-[#A56C38] hover:text-[#FFF7EE]"
          >
            {mode === "create" ? t("management.create") : t("management.update")}
          </button>

          {mode === "edit" ? (
            <button
              type="button"
              onClick={resetForm}
              className="rounded-md border border-[#A56C38] bg-[#FFF7EE] px-4 py-2 text-[#8E4F24] transition-colors duration-200 hover:bg-[#FCE6CF]"
            >
              {t("management.cancel")}
            </button>
          ) : null}
        </div>
      </form>

      {error ? <p className="mt-3 text-center text-sm text-[#8E4F24]">{error}</p> : null}

      <section className="mx-auto mt-6 w-full max-w-2xl">
        <h2 className="text-lg font-semibold text-[#4C250E]">{t("management.users")}</h2>

        {loading ? (
          <p className="mt-2 text-sm text-[#8E4F24]">{t("management.loading")}</p>
        ) : (
          <ul className="mt-3 space-y-2">
            {users.map((user) => (
              <li key={user.username} className="rounded-md border border-[#C99763] bg-[#FFF7EE] p-3 text-left">
                <div className="flex items-center justify-between gap-2">
                  <div>
                    <strong>{user.displayName}</strong>
                    <div className="text-sm text-[#70421E]">{user.username}</div>
                    <div className="text-xs text-[#8E4F24]">
                      {t("management.added")}: {new Date(user.added).toLocaleString()}
                    </div>
                    <div className="text-xs text-[#8E4F24]">
                      {t("management.updated")}: {new Date(user.lastUpdated).toLocaleString()}
                    </div>
                    <div className="text-xs text-[#8E4F24]">
                      {user.isAdmin ? t("management.admin") : t("management.user")}
                    </div>
                  </div>

                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => onEdit(user)}
                      className="rounded-md border border-[#A56C38] bg-[#8E4F24] px-3 py-1 text-[#F5D1A9] transition-colors duration-200 hover:bg-[#A56C38] hover:text-[#FFF7EE]"
                    >
                      {t("management.edit")}
                    </button>
                    <button
                      type="button"
                      onClick={() => void onDelete(user.username)}
                      className="rounded-md border border-[#A56C38] bg-[#FFF7EE] px-3 py-1 text-[#8E4F24] transition-colors duration-200 hover:bg-[#FCE6CF]"
                    >
                      {t("management.delete")}
                    </button>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </main>
  );
}
