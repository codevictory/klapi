import { BrowserRouter, Navigate, Route, Routes, useLocation } from "react-router-dom";
import { useEffect } from "react";
import { useRecoilValue, useSetRecoilState } from "recoil";
import Nav from "~/components/Nav";
import Home from "~/routes/index";
import About from "~/routes/about";
import Feedback from "~/routes/feedback";
import Login from "~/routes/login";
import Management from "~/routes/management";
import NotFound from "~/routes/[...404]";
import Toasts from "~/components/Toasts";
import { APP_ROLES, hasAnyRole } from "~/auth/roles";
import { LANGUAGE_STORAGE_KEY, normalizeLanguage, useLanguage } from "~/i18n";
import { languageAtom, sessionAtom } from "~/state/appState";
import "./app.css";

function AppShell() {
  const location = useLocation();
  const { language, setLanguage } = useLanguage();
  const session = useRecoilValue(sessionAtom);
  const setSession = useSetRecoilState(sessionAtom);
  const setLanguageAtom = useSetRecoilState(languageAtom);

  useEffect(() => {
    // Feedback page manages its own language default
    if (location.pathname === "/feedback") return;
    const storedPreferredLanguage = localStorage.getItem("session-preferred-language");
    const fallback = normalizeLanguage(storedPreferredLanguage === "en" || storedPreferredLanguage === "sk" ? storedPreferredLanguage : "fi");
    const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY);
    if (stored !== null) {
      setLanguage(normalizeLanguage(stored)); // explicit prior choice — keep it saved
    } else {
      setLanguageAtom(fallback); // just set the atom, don't persist the default
    }
  }, [location.pathname, setLanguage, setLanguageAtom]);

  useEffect(() => {
    const storedUsername = localStorage.getItem("session-username");
    const storedDisplayName = localStorage.getItem("session-display-name");
    const storedPreferredLanguage = localStorage.getItem("session-preferred-language");
    const storedRoles = localStorage.getItem("session-roles");
    const storedToken = localStorage.getItem("session-token");
    if (!storedUsername || !storedDisplayName || !storedToken) {
      setSession(null);
      return;
    }

    setSession({
      username: storedUsername,
      displayName: storedDisplayName,
      preferredLanguage: storedPreferredLanguage === "en" || storedPreferredLanguage === "sk" ? storedPreferredLanguage : "fi",
      roles: (storedRoles ?? "").split(",").filter(Boolean),
      token: storedToken,
    });
  }, [setSession]);

  useEffect(() => {
    document.documentElement.lang = language;
  }, [language]);

  return (
    <>
      <Nav />
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/about" element={<About />} />
        <Route path="/feedback" element={<Feedback />} />
        <Route path="/login" element={<Login />} />
        <Route
          path="/management"
          element={hasAnyRole(session, [APP_ROLES.admin]) ? <Management /> : <Navigate to="/" replace />}
        />
        <Route path="*" element={<NotFound />} />
        <Route path="/index.html" element={<Navigate to="/" replace />} />
      </Routes>
      <Toasts />
    </>
  );
}

export default function App() {
  return (
    <BrowserRouter future={{ v7_relativeSplatPath: true }}>
      <AppShell />
    </BrowserRouter>
  );
}
