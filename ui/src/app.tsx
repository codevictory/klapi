import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { useEffect } from "react";
import { useSetRecoilState } from "recoil";
import Nav from "~/components/Nav";
import Home from "~/routes/index";
import About from "~/routes/about";
import Login from "~/routes/login";
import NotFound from "~/routes/[...404]";
import Toasts from "~/components/Toasts";
import { initializeLanguage, useLanguage } from "~/i18n";
import { sessionAtom } from "~/state/appState";
import "./app.css";

function AppShell() {
  const { language, setLanguage } = useLanguage();
  const setSession = useSetRecoilState(sessionAtom);

  useEffect(() => {
    initializeLanguage(setLanguage);
  }, [setLanguage]);

  useEffect(() => {
    const storedEmail = localStorage.getItem("session-email");
    if (!storedEmail) {
      setSession(null);
      return;
    }

    setSession({ email: storedEmail });
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
