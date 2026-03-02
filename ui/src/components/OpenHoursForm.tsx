import { useEffect, useMemo, useRef, useState } from "react";
import { useRecoilState } from "recoil";
import {
  createLokOpenHours,
  deleteLokOpenHours,
  queryLokOpenHours,
  setActiveLokOpenHours,
  updateLokOpenHours,
  type LokOpenHours,
} from "~/api";
import { useT } from "~/i18n";
import { openHoursAtom, toastsAtom, type Toast } from "~/state/appState";

type FormState = {
  name: string;
  paragraph1: string;
  paragraph2: string;
  paragraph3: string;
  paragraph4: string;
  kitchenNotice: string;
};

const EMPTY_FORM: FormState = {
  name: "",
  paragraph1: "",
  paragraph2: "",
  paragraph3: "",
  paragraph4: "",
  kitchenNotice: "",
};

export default function OpenHoursForm() {
  const t = useT();
  const [versions, setVersions] = useRecoilState(openHoursAtom);
  const [, setToasts] = useRecoilState(toastsAtom);

  const [selectedVersion, setSelectedVersion] = useState("");
  const [isEditing, setIsEditing] = useState(false);
  const [editingVersionId, setEditingVersionId] = useState("");
  const [deletingId, setDeletingId] = useState("");
  const [activatingId, setActivatingId] = useState("");
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<FormState>(EMPTY_FORM);

  const initializedRef = useRef(false);
  const toastGuardRef = useRef<Record<string, number>>({});

  const isUpdateMode = editingVersionId.length > 0;

  const pushToast = (message: string, kind: Toast["kind"], dedupeKey: string) => {
    const now = Date.now();
    const lastShown = toastGuardRef.current[dedupeKey] ?? 0;
    if (now - lastShown < 800) return;

    toastGuardRef.current[dedupeKey] = now;

    setToasts((previous) => [
      ...previous,
      {
        id: now + Math.floor(Math.random() * 1000),
        message,
        kind,
        leaving: false,
      },
    ]);
  };

  const clearForm = () => {
    setForm(EMPTY_FORM);
  };

  const hydrate = async () => {
    const loaded = await queryLokOpenHours().catch(() => []);
    setVersions(loaded);
    initializedRef.current = true;
  };

  useEffect(() => {
    if (initializedRef.current) return;
    void hydrate();
  }, []);

  useEffect(() => {
    if (versions.length === 0) {
      if (selectedVersion) setSelectedVersion("");
      return;
    }

    const activeVersion = versions.find((version) => version.isActive);
    if (activeVersion) {
      const activeId = String(activeVersion.id);
      if (activeId !== selectedVersion) {
        setSelectedVersion(activeId);
      }
      return;
    }

    if (!versions.some((version) => String(version.id) === selectedVersion)) {
      setSelectedVersion(String(versions[0].id));
    }
  }, [versions, selectedVersion]);

  useEffect(() => {
    if (!isEditing || !isUpdateMode) return;
    if (versions.some((version) => String(version.id) === editingVersionId)) return;

    setEditingVersionId("");
    setIsEditing(false);
    clearForm();
  }, [versions, isEditing, isUpdateMode, editingVersionId]);

  useEffect(() => {
    if (!isEditing || isUpdateMode) return;
    clearForm();
  }, [isEditing, isUpdateMode]);

  const populateForm = (versionId: string) => {
    const selected = versions.find((version) => String(version.id) === versionId);
    if (!selected) {
      clearForm();
      return;
    }

    setForm({
      name: selected.name,
      paragraph1: selected.paragraph1,
      paragraph2: selected.paragraph2,
      paragraph3: selected.paragraph3,
      paragraph4: selected.paragraph4,
      kitchenNotice: selected.kitchenNotice,
    });
  };

  const startCreate = () => {
    setEditingVersionId("");
    clearForm();
    setIsEditing(true);
  };

  const startEdit = (versionId: string) => {
    setEditingVersionId(versionId);
    populateForm(versionId);
    setIsEditing(true);
  };

  const cancelEdit = () => {
    setEditingVersionId("");
    clearForm();
    setIsEditing(false);
  };

  const submitForm = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving(true);

    try {
      if (isUpdateMode) {
        const updated = await updateLokOpenHours(Number(editingVersionId), form);
        setVersions((previous) =>
          previous.map((version) =>
            version.id === updated.id ? updated : version,
          ),
        );
        pushToast(
          t("home.openHours.updated"),
          "success",
          `update:${updated.id}:${updated.version}`,
        );
      } else {
        const created = await createLokOpenHours(form);
        setVersions((previous) => {
          const next = created.isActive
            ? [created, ...previous.map((version) => ({ ...version, isActive: false }))]
            : [created, ...previous];
          return next.slice(0, 5);
        });
        setSelectedVersion(String(created.id));
        pushToast(
          t("home.openHours.saved"),
          "success",
          `create:${created.id}:${created.version}`,
        );
      }

      setEditingVersionId("");
      clearForm();
      setIsEditing(false);
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      pushToast(message, "error", `error:save:${message}`);
    } finally {
      setSaving(false);
    }
  };

  const onDelete = async (version: LokOpenHours) => {
    setDeletingId(String(version.id));

    try {
      await deleteLokOpenHours(version.id);
      setVersions((previous) =>
        previous.filter((item) => item.id !== version.id),
      );

      if (editingVersionId === String(version.id)) {
        setEditingVersionId("");
        clearForm();
        setIsEditing(false);
      }

      pushToast(t("home.openHours.deleted"), "success", `delete:${version.id}`);
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      pushToast(message, "error", `error:delete:${message}`);
    } finally {
      setDeletingId("");
    }
  };

  const onSetActive = async (version: LokOpenHours) => {
    if (version.isActive) return;

    setActivatingId(String(version.id));

    try {
      const result = await setActiveLokOpenHours(version.id);
      setVersions((previous) =>
        previous.map((item) => ({
          ...item,
          isActive: item.id === result.id,
        })),
      );
      setSelectedVersion(String(result.id));
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      pushToast(message, "error", `error:active:${message}`);
    } finally {
      setActivatingId("");
    }
  };

  const tooltipText = (version: LokOpenHours) =>
    [
      version.name,
      version.paragraph1,
      version.paragraph2,
      version.paragraph3,
      version.paragraph4,
      version.kitchenNotice,
    ]
      .filter(Boolean)
      .join("\n");

  const nameRequired = useMemo(() => form.name.trim().length === 0, [form.name]);

  return (
    <section className="w-full max-w-3xl rounded-2xl border border-[#C99763] bg-[#F5D1A9] p-4 shadow-md sm:p-6">
      <h2 className="text-2xl font-semibold text-[#4C250E]">{t("home.openHours.heading")}</h2>

      <div className="mt-4">
        <div
          className={`transition-all duration-500 ease-[cubic-bezier(0.22,1,0.36,1)] ${!isEditing
              ? "max-h-[1200px] translate-y-0 opacity-100"
              : "pointer-events-none max-h-0 -translate-y-3 overflow-hidden opacity-0"
            }`}
        >
          <div className="mb-3 flex justify-end">
            <button
              type="button"
              onClick={startCreate}
              className="w-full rounded-md border border-[#70421E] bg-[#8E4F24] px-4 py-2 text-[#FFF7EE] hover:bg-[#70421E] sm:w-auto"
            >
              {t("home.openHours.new")}
            </button>
          </div>

          {versions.length === 0 ? (
            <p className="text-[#70421E]">{t("home.openHours.empty")}</p>
          ) : (
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {versions.map((version) => {
                const versionId = String(version.id);
                const active = version.isActive;
                const deleting = deletingId === versionId;
                const settingActive = activatingId === versionId;

                return (
                  <article
                    key={version.id}
                    onClick={() => {
                      if (active || settingActive) return;
                      void onSetActive(version);
                    }}
                    className={`rounded-xl border p-3 transition-all duration-400 ease-[cubic-bezier(0.22,1,0.36,1)] ${active ? "border-[#8E4F24] bg-[#D6A06B]" : "border-[#C99763] bg-[#EED5B8]"
                      } ${deleting ? "-translate-y-1 scale-90 opacity-0" : ""} ${settingActive && !active ? "opacity-80" : ""
                      }`}
                  >
                    <div className="mb-2 flex items-center justify-between gap-2" title={tooltipText(version)}>
                      <p className="truncate font-medium text-[#4C250E]">
                        {version.name || t("home.openHours.latest")}
                      </p>
                      {active && (
                        <span className="rounded-md bg-[#8E4F24] px-2 py-0.5 text-xs font-medium text-[#FFF7EE]">
                          {t("home.openHours.active")}
                        </span>
                      )}
                    </div>

                    <div className="flex flex-wrap gap-2">
                      <button
                        type="button"
                        onClick={(event) => {
                          event.stopPropagation();
                          startEdit(versionId);
                        }}
                        className="rounded-md border border-[#A56C38] bg-[#FFF7EE] px-3 py-1.5 text-sm text-[#70421E] hover:bg-[#E3A977]"
                      >
                        {t("home.openHours.edit")}
                      </button>

                      <button
                        type="button"
                        disabled={active || deleting}
                        onClick={(event) => {
                          event.stopPropagation();
                          void onDelete(version);
                        }}
                        className={`rounded-md border border-[#8E4F24] bg-[#EED5B8] px-3 py-1.5 text-sm text-[#70421E] disabled:cursor-not-allowed disabled:opacity-50 ${!active ? "hover:bg-[#E3A977]" : ""
                          }`}
                      >
                        {t("home.openHours.delete")}
                      </button>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </div>

        <form
          onSubmit={submitForm}
          className={`space-y-3 transition-all duration-500 ease-[cubic-bezier(0.22,1,0.36,1)] ${isEditing
              ? "max-h-[1600px] translate-y-0 opacity-100"
              : "pointer-events-none max-h-0 translate-y-3 overflow-hidden opacity-0"
            }`}
        >
          <div className="flex flex-wrap items-center justify-between gap-2">
            {isUpdateMode && (
              <p className="text-sm font-medium text-[#4C250E]">{t("home.openHours.editing")}</p>
            )}
            <button
              type="button"
              onClick={cancelEdit}
              className="rounded-md border border-[#A56C38] bg-[#EED5B8] px-3 py-1.5 text-sm text-[#70421E] hover:bg-[#E3A977]"
            >
              {t("home.openHours.cancel")}
            </button>
          </div>

          <div>
            <label htmlFor="name" className="block text-sm font-medium text-[#4C250E]">{t("home.openHours.name")}</label>
            <input
              id="name"
              name="name"
              required
              value={form.name}
              onChange={(event) => setForm((previous) => ({ ...previous, name: event.target.value }))}
              className="mt-1 w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-3 py-2 text-[#4C250E] focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
            />
            {nameRequired && (
              <p className="mt-1 text-xs text-[#8E4F24]">{t("home.openHours.nameRequired")}</p>
            )}
          </div>

          {(["paragraph1", "paragraph2", "paragraph3", "paragraph4", "kitchenNotice"] as const).map((field) => (
            <div key={field}>
              <label htmlFor={field} className="block text-sm font-medium text-[#4C250E]">
                {t(`home.openHours.${field}`)}
              </label>
              <textarea
                id={field}
                name={field}
                rows={2}
                value={form[field]}
                onChange={(event) =>
                  setForm((previous) => ({
                    ...previous,
                    [field]: event.target.value,
                  }))
                }
                className="mt-1 w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-3 py-2 text-[#4C250E] focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
              />
            </div>
          ))}

          <div className="flex flex-wrap gap-3 pt-1">
            <button
              type="submit"
              disabled={saving}
              className="w-full rounded-md border border-[#70421E] bg-[#8E4F24] px-4 py-2 text-[#FFF7EE] hover:bg-[#70421E] disabled:cursor-not-allowed disabled:opacity-50 sm:w-auto"
            >
              {isUpdateMode
                ? saving
                  ? t("home.openHours.updating")
                  : t("home.openHours.update")
                : saving
                  ? t("home.openHours.saving")
                  : t("home.openHours.submit")}
            </button>
          </div>
        </form>
      </div>
    </section>
  );
}
