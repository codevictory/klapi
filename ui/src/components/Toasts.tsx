import { useEffect } from "react";
import { useRecoilState } from "recoil";
import { toastsAtom } from "~/state/appState";

export default function Toasts() {
  const [toasts, setToasts] = useRecoilState(toastsAtom);

  useEffect(() => {
    if (toasts.length === 0) return;

    const timers = toasts.map((toast) =>
      window.setTimeout(() => {
        setToasts((previous) =>
          previous.map((item) =>
            item.id === toast.id ? { ...item, leaving: true } : item,
          ),
        );

        window.setTimeout(() => {
          setToasts((previous) => previous.filter((item) => item.id !== toast.id));
        }, 220);
      }, 3200),
    );

    return () => timers.forEach((timer) => window.clearTimeout(timer));
  }, [toasts, setToasts]);

  const dismiss = (id: number) => {
    setToasts((previous) =>
      previous.map((toast) =>
        toast.id === id ? { ...toast, leaving: true } : toast,
      ),
    );
    window.setTimeout(() => {
      setToasts((previous) => previous.filter((toast) => toast.id !== id));
    }, 220);
  };

  return (
    <div className="pointer-events-none fixed bottom-3 left-3 right-3 z-50 flex flex-col gap-2 sm:bottom-5 sm:left-auto sm:right-5 sm:w-full sm:max-w-sm">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`pointer-events-auto rounded-lg border px-4 py-3 text-sm shadow-lg transition-all duration-200 ease-out ${toast.kind === "success"
              ? "border-[#8E4F24] bg-[#F5E2CB] text-[#4C250E]"
              : "border-[#8E4F24] bg-[#F7D3B7] text-[#4C250E]"
            } ${toast.leaving ? "translate-y-2 opacity-0" : "translate-y-0 opacity-100"}`}
        >
          <div className="flex items-start justify-between gap-3">
            <p className="leading-snug">{toast.message}</p>
            <button
              type="button"
              onClick={() => dismiss(toast.id)}
              className="rounded px-1 text-base leading-none text-[#70421E] hover:bg-[#EED5B8]"
              aria-label="Dismiss"
            >
              ×
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}
