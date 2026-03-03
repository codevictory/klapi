import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { useEffect } from "react";
import { useRecoilValue, useSetRecoilState } from "recoil";
import Nav from "~/components/Nav";
import Home from "~/routes/index";
import About from "~/routes/about";
import Login from "~/routes/login";
import Management from "~/routes/management";
import NotFound from "~/routes/[...404]";
import Toasts from "~/components/Toasts";
import { initializeLanguage, useLanguage } from "~/i18n";
import { sessionAtom } from "~/state/appState";
import "./app.css";

function AppShell() {
  const { language, setLanguage } = useLanguage();
  const session = useRecoilValue(sessionAtom);
  const setSession = useSetRecoilState(sessionAtom);

  useEffect(() => {
    initializeLanguage(setLanguage);
  }, [setLanguage]);

  useEffect(() => {
    const storedUsername = localStorage.getItem("session-username");
    const storedDisplayName = localStorage.getItem("session-display-name");
    const storedIsAdmin = localStorage.getItem("session-is-admin");
    const storedToken = localStorage.getItem("session-token");
    if (!storedUsername || !storedDisplayName || !storedToken || !storedIsAdmin) {
      setSession(null);
      return;
    }

    setSession({
      username: storedUsername,
      displayName: storedDisplayName,
      isAdmin: storedIsAdmin === "true",
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
        <Route path="/login" element={<Login />} />
        <Route
          path="/management"
          element={session?.isAdmin ? <Management /> : <Navigate to="/" replace />}
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
    <BrowserRouter>
      <AppShell />
    </BrowserRouter>
  );
}
