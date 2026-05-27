import { Link, useLocation, useNavigate } from "react-router-dom";
import { useRecoilState } from "recoil";
import { APP_ROLES, hasAnyRole } from "~/auth/roles";
import { sessionAtom } from "~/state/appState";
import { useLanguage, useT } from "~/i18n";

export default function Nav() {
  const location = useLocation();
  const navigate = useNavigate();
  const t = useT();
  const { language, setLanguage } = useLanguage();
  const [session, setSession] = useRecoilState(sessionAtom);

  const isFeedbackPage = location.pathname === "/feedback";

  const signOut = () => {
    setSession(null);
    localStorage.removeItem("session-username");
    localStorage.removeItem("session-display-name");
    localStorage.removeItem("session-preferred-language");
    localStorage.removeItem("session-roles");
    localStorage.removeItem("session-token");
    navigate("/login");
  };

  const linkClass = (path: string) =>
    `px-3 py-2 text-[#F5D1A9] uppercase transition-colors duration-200 border-b-2 ${location.pathname === path
      ? "border-[#E3A977] text-[#FFF7EE]"
      : "border-transparent hover:text-[#FFF7EE]"
    }`;

  return (
    <nav className={`fixed top-0 left-0 z-50 w-full shadow-sm ${isFeedbackPage ? "bg-[#2D6A4F]" : "bg-[#70421E]"}`}>
      <div className="flex flex-col sm:flex-row sm:items-center px-4 py-2 text-sm font-medium">
        {/* Links — hidden on feedback page */}
        {!isFeedbackPage && (
          <div className="order-2 sm:order-1 flex items-center justify-center sm:justify-start">
            <Link to="/" className={linkClass("/")}>{t("nav.home")}</Link>
            <Link to="/about" className={linkClass("/about")}>{t("nav.about")}</Link>
            {hasAnyRole(session, [APP_ROLES.admin]) ? (
              <Link to="/management" className={linkClass("/management")}>{t("nav.management")}</Link>
            ) : null}
          </div>
        )}

        {/* Centered title — shown only on feedback page */}
        {isFeedbackPage && (
          <div className="order-2 sm:order-1 flex-1 flex items-center justify-center sm:absolute sm:left-1/2 sm:-translate-x-1/2">
            <span className="text-[#D8F3DC] font-semibold tracking-wide">{t("nav.feedbackTitle")}</span>
          </div>
        )}

        {/* Controls — top on mobile (order-1), right on desktop (sm:order-2 sm:ml-auto) */}
        <div className="order-1 sm:order-2 sm:ml-auto flex items-center justify-center gap-2">
          {!isFeedbackPage && (
            <b className="hidden sm:block text-[#F5D1A9]">{session?.displayName ?? ""}</b>
          )}
          <div className={`flex items-center gap-1 rounded-md border p-1 ${isFeedbackPage ? "border-[#52B788] bg-[#52B788]/30" : "border-[#8E4F24] bg-[#8E4F24]/45"}`}>
            <button
              type="button"
              onClick={() => setLanguage("fi")}
              className={`rounded px-2 py-1 text-xs ${
                language === "fi"
                  ? isFeedbackPage ? "bg-[#74C69D] text-[#1B4332]" : "bg-[#E3A977] text-[#4C250E]"
                  : isFeedbackPage ? "text-[#D8F3DC] hover:text-white" : "text-[#F5D1A9] hover:text-[#FFF7EE]"
              }`}
            >
              {t("nav.language.fi")}
            </button>
            <button
              type="button"
              onClick={() => setLanguage("en")}
              className={`rounded px-2 py-1 text-xs ${
                language === "en"
                  ? isFeedbackPage ? "bg-[#74C69D] text-[#1B4332]" : "bg-[#E3A977] text-[#4C250E]"
                  : isFeedbackPage ? "text-[#D8F3DC] hover:text-white" : "text-[#F5D1A9] hover:text-[#FFF7EE]"
              }`}
            >
              {t("nav.language.en")}
            </button>
            <button
              type="button"
              onClick={() => setLanguage("sk")}
              className={`rounded px-2 py-1 text-xs ${
                language === "sk"
                  ? isFeedbackPage ? "bg-[#74C69D] text-[#1B4332]" : "bg-[#E3A977] text-[#4C250E]"
                  : isFeedbackPage ? "text-[#D8F3DC] hover:text-white" : "text-[#F5D1A9] hover:text-[#FFF7EE]"
              }`}
            >
              {t("nav.language.sk")}
            </button>
          </div>

          {!isFeedbackPage && (
            session ? (
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
            )
          )}
        </div>
      </div>
    </nav>
  );
}
