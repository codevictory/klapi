import { Link, useLocation, useNavigate } from "react-router-dom";
import { useRecoilState } from "recoil";
import { sessionAtom } from "~/state/appState";
import { useLanguage, useT } from "~/i18n";

export default function Nav() {
  const location = useLocation();
  const navigate = useNavigate();
  const t = useT();
  const { language, setLanguage } = useLanguage();
  const [session, setSession] = useRecoilState(sessionAtom);

  const signOut = () => {
    setSession(null);
    localStorage.removeItem("session-username");
    localStorage.removeItem("session-display-name");
    localStorage.removeItem("session-is-admin");
    localStorage.removeItem("session-token");
    navigate("/login");
  };

  const linkClass = (path: string) =>
    `px-3 py-2 text-[#F5D1A9] uppercase transition-colors duration-200 border-b-2 ${location.pathname === path
      ? "border-[#E3A977] text-[#FFF7EE]"
      : "border-transparent hover:text-[#FFF7EE]"
    }`;

  return (
    <nav className="fixed top-0 left-0 z-50 flex w-full items-center justify-between bg-[#70421E] px-4 py-3 text-sm font-medium shadow-sm">
      <Link to="/" className={linkClass("/")}>{t("nav.home")}</Link>
      <Link to="/about" className={linkClass("/about")}>{t("nav.about")}</Link>
      {session?.isAdmin ? (
        <Link to="/management" className={linkClass("/management")}>{t("nav.management")}</Link>
      ) : null}

      <div className="ml-auto flex items-center gap-2">
        <b className="text-[#F5D1A9]">{session?.displayName ?? ""}</b>
        <div className="flex items-center gap-1 rounded-md border border-[#8E4F24] bg-[#8E4F24]/45 p-1">
          <button
            type="button"
            onClick={() => setLanguage("fi")}
            className={`rounded px-2 py-1 text-xs ${language === "fi"
              ? "bg-[#E3A977] text-[#4C250E]"
              : "text-[#F5D1A9] hover:text-[#FFF7EE]"
              }`}
          >
            {t("nav.language.fi")}
          </button>
          <button
            type="button"
            onClick={() => setLanguage("en")}
            className={`rounded px-2 py-1 text-xs ${language === "en"
              ? "bg-[#E3A977] text-[#4C250E]"
              : "text-[#F5D1A9] hover:text-[#FFF7EE]"
              }`}
          >
            {t("nav.language.en")}
          </button>
        </div>

        {session ? (
          <button
            type="button"
            onClick={signOut}
            className="rounded-md border border-[#A56C38] bg-[#8E4F24] px-4 py-2 text-[#F5D1A9] transition-colors duration-200 hover:bg-[#A56C38] hover:text-[#FFF7EE]"
          >
            {t("nav.signOut")}
          </button>
        ) : (
          <Link
            to="/login"
            className="rounded-md border border-[#A56C38] bg-[#8E4F24] px-4 py-2 text-[#F5D1A9] transition-colors duration-200 hover:bg-[#A56C38] hover:text-[#FFF7EE]"
          >
            {t("nav.login")}
          </Link>
        )}
      </div>
    </nav>
  );
}
