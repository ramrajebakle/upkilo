"use client";

import React, { useState } from "react";
import { Eye, EyeOff, Loader2, ShieldCheck } from "lucide-react";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import type { IntegrationItem } from "./IntegrationCard";

interface Props {
  integration: IntegrationItem | null;
  isOpen: boolean;
  onClose: () => void;
  onSave: (id: string, credentials: Record<string, string>) => Promise<void>;
}

export function IntegrationCredentialModal({ integration, isOpen, onClose, onSave }: Props) {
  const [values, setValues] = useState<Record<string, string>>({});
  const [revealed, setRevealed] = useState<Record<string, boolean>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Reset state when a different integration is opened
  React.useEffect(() => {
    if (isOpen) {
      setValues({});
      setRevealed({});
      setError(null);
    }
  }, [isOpen, integration?.id]);

  if (!integration) return null;

  const handleChange = (key: string, value: string) => {
    setValues((prev) => ({ ...prev, [key]: value }));
    setError(null);
  };

  const toggleReveal = (key: string) => {
    setRevealed((prev) => ({ ...prev, [key]: !prev[key] }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    // Validate all required fields
    for (const field of integration.fields) {
      if (!values[field.key]?.trim()) {
        setError(`${field.label} is required.`);
        return;
      }
    }

    setSaving(true);
    try {
      await onSave(integration.id, values);
      onClose();
    } catch (err: any) {
      setError(err?.response?.data?.message ?? "Failed to save credentials. Please try again.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={`Connect ${integration.name}`}
      description="Your credentials are encrypted with AES-256-GCM before being stored."
      size="md"
    >
      <form onSubmit={handleSubmit} className="space-y-4 mt-2">
        {integration.authType === "oauth" && (
          <div className="flex items-start gap-2 p-3 bg-blue-50 rounded-lg text-sm text-blue-700">
            <ShieldCheck className="w-4 h-4 mt-0.5 shrink-0" />
            <span>This integration uses OAuth. You&apos;ll be redirected to authorize access on the provider&apos;s site.</span>
          </div>
        )}

        {integration.fields.map((field) => (
          <div key={field.key} className="space-y-1.5">
            <Label htmlFor={field.key}>{field.label}</Label>
            <div className="relative">
              <Input
                id={field.key}
                type={field.secret && !revealed[field.key] ? "password" : "text"}
                placeholder={field.placeholder}
                value={values[field.key] ?? ""}
                onChange={(e) => handleChange(field.key, e.target.value)}
                className={field.secret ? "pr-10" : ""}
                autoComplete="off"
              />
              {field.secret && (
                <button
                  type="button"
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-foreground-muted hover:text-foreground-secondary"
                  onClick={() => toggleReveal(field.key)}
                  tabIndex={-1}
                >
                  {revealed[field.key] ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              )}
            </div>
          </div>
        ))}

        {error && (
          <p className="text-sm text-red-600 bg-red-50 rounded p-2">{error}</p>
        )}

        <div className="flex gap-3 pt-2">
          <Button type="button" variant="outline" className="flex-1" onClick={onClose} disabled={saving}>
            Cancel
          </Button>
          <Button type="submit" className="flex-1" disabled={saving}>
            {saving ? <Loader2 className="w-4 h-4 animate-spin mr-2" /> : null}
            {saving ? "Saving…" : "Save & Connect"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
