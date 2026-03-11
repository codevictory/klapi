import { useEffect } from "react";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  description?: string;
  contextText?: string;
  confirmLabel: string;
  cancelLabel: string;
  busy?: boolean;
  busyLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
};

export default function ConfirmDialog({
  open,
  title,
  description,
  contextText,
  confirmLabel,
  cancelLabel,
  busy = false,
  busyLabel,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  useEffect(() => {
    if (!open) return;

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !busy) {
        onCancel();
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => {
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [open, busy, onCancel]);

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-[#2B170A]/60 px-4"
      onClick={() => {
        if (!busy) onCancel();
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        className="w-full max-w-md rounded-xl border border-[#C99763] bg-[#F5D1A9] p-5 shadow-xl"
        onClick={(event) => event.stopPropagation()}
      >
        <h3 className="text-lg font-semibold text-[#4C250E]">{title}</h3>
        {description && <p className="mt-2 text-sm text-[#70421E]">{description}</p>}
        {contextText && <p className="mt-1 text-sm font-medium text-[#4C250E]">{contextText}</p>}

        <div className="mt-4 flex flex-wrap gap-2">
          <button
            type="button"
            onClick={onCancel}
            disabled={busy}
            className="rounded-md border border-[#A56C38] bg-[#EED5B8] px-3 py-1.5 text-sm text-[#70421E] hover:bg-[#E3A977] disabled:cursor-not-allowed disabled:opacity-50"
          >
            {cancelLabel}
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={busy}
            className="rounded-md border border-[#70421E] bg-[#8E4F24] px-3 py-1.5 text-sm text-[#FFF7EE] hover:bg-[#70421E] disabled:cursor-not-allowed disabled:opacity-50"
          >
            {busy ? busyLabel ?? confirmLabel : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}