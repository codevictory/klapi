import { FormEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useRecoilState } from "recoil";
import { requestAuthToken } from "~/api";
import { useT } from "~/i18n";
import { sessionAtom } from "~/state/appState";

export default function Login() {
  const t = useT();
  const navigate = useNavigate();
  const [session, setSession] = useRecoilState(sessionAtom);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    document.title = t("login.title");
  }, [t]);

  useEffect(() => {
    if (!session) return;
    navigate("/");
  }, [session, navigate]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();

    if (!username.trim() || !password.trim()) {
      setError(t("errors.requiredUsernamePassword"));
      return;
    }

    const normalizedUsername = username.trim().toLowerCase();

    try {
      const auth = await requestAuthToken(normalizedUsername, password);

      setSession({
        username: auth.username,
        displayName: auth.displayName,
        roles: auth.roles,
        token: auth.accessToken,
      });

      localStorage.setItem("session-username", auth.username);
      localStorage.setItem("session-display-name", auth.displayName);
      localStorage.setItem("session-roles", auth.roles.join(","));
      localStorage.setItem("session-token", auth.accessToken);
      setError("");
      navigate("/");
    } catch {
      setError(t("errors.invalidUsernameOrPassword"));
    }
  };

  return (
    <main>
      <h1>{t("login.heading")}</h1>
      <h2>{t("login.subheading")}</h2>
      <form onSubmit={submit} className="w-full max-w-md space-y-4 px-4">
        <label htmlFor="username" className="block w-full text-left">
          {t("login.username")}
          <input
            id="username"
            name="username"
            type="text"
            autoComplete="username"
            placeholder={t("login.username")}
            required
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            className="mt-1 block w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-4 py-2 focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
        </label>

        <label htmlFor="password" className="block w-full text-left">
          {t("login.password")}
          <input
            id="password"
            name="password"
            type="password"
            autoComplete="current-password"
            placeholder={t("login.password")}
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            className="mt-1 block w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-4 py-2 focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
        </label>

        <button
          type="submit"
          className="w-full rounded-lg bg-gradient-to-r from-[#A56C38] to-[#70421E] px-4 py-2 text-[#FFF7EE] shadow-lg shadow-[#70421E]/30 transition-colors duration-300 hover:from-[#8E4F24] hover:to-[#4C250E]"
        >
          {t("login.submit")}
        </button>

        {error && <p className="mt-2 text-center text-xs text-[#8E4F24]">{error}</p>}
      </form>
    </main>
  );
}
