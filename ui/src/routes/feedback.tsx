import { FormEvent, useEffect, useState } from "react";
import { submitFeedback } from "~/api";
import { useT } from "~/i18n";

export default function Feedback() {
  const t = useT();
  const [message, setMessage] = useState("");
  const [status, setStatus] = useState<"idle" | "sending" | "success" | "error">("idle");
  const [validationError, setValidationError] = useState("");

  useEffect(() => {
    document.title = t("feedback.title");
  }, [t]);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setValidationError("");

    if (!message.trim()) {
      setValidationError(t("feedback.required"));
      return;
    }

    if (message.trim().length > 5000) {
      setValidationError(t("feedback.tooLong"));
      return;
    }

    setStatus("sending");

    try {
      await submitFeedback(message.trim());
      setStatus("success");
      setMessage("");
    } catch {
      setStatus("error");
    }
  };

  return (
    <main className="!bg-[#D8EDDA] !text-[#1A3A2A] px-4 py-8">
      <div className="mx-auto max-w-lg space-y-6">
        <div className="space-y-2 text-center">
          <h1 className="!text-[#1B4332] text-2xl font-bold">{t("feedback.heading")}</h1>
          <p className="text-sm text-[#2D6A4F]/80">{t("feedback.description")}</p>
        </div>

        {status === "success" ? (
          <div className="rounded-md border border-[#74C69D] bg-[#EBF7EF] p-4 text-center text-sm font-medium text-[#1B4332]">
            {t("feedback.success")}
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1">
              <label
                htmlFor="feedback-message"
                className="block text-sm font-medium text-[#1B4332]"
              >
                {t("feedback.label")}
              </label>
              <textarea
                id="feedback-message"
                autoFocus
                value={message}
                onChange={(e) => {
                  setMessage(e.target.value);
                  setValidationError("");
                  if (status === "error") setStatus("idle");
                }}
                placeholder={t("feedback.placeholder")}
                rows={6}
                maxLength={5000}
                className="w-full rounded-md border border-[#74C69D] bg-[#EBF7EF] px-3 py-2 text-sm text-[#1A3A2A] placeholder:text-[#74C69D] focus:border-[#2D6A4F] focus:outline-none focus:ring-1 focus:ring-[#2D6A4F] disabled:opacity-60"
                disabled={status === "sending"}
              />
              {validationError && (
                <p className="text-xs text-red-600">{validationError}</p>
              )}
              {status === "error" && (
                <p className="text-xs text-red-600">{t("feedback.error")}</p>
              )}
              <p className="text-right text-xs text-[#2D6A4F]/50">{message.length} / 5000</p>
            </div>

            <button
              type="submit"
              disabled={status === "sending"}
              className="w-full rounded-md bg-[#2D6A4F] px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-[#1B4332] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2D6A4F] focus-visible:ring-offset-2 disabled:opacity-60"
            >
              {status === "sending" ? t("feedback.sending") : t("feedback.submit")}
            </button>
          </form>
        )}
      </div>
    </main>
  );
}
