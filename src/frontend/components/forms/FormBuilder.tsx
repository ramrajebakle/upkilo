"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Card, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { GripVertical, Plus, Trash2, Undo2, Redo2 } from "lucide-react";
import { useHistoryState } from "@/hooks/useHistoryState";

interface FormField {
  id: string;
  type: string;
  label: string;
  required: boolean;
}

export const FormBuilder = () => {
  const [fields, setFields, { undo, redo, canUndo, canRedo }] = useHistoryState<FormField[]>([
    { id: "1", type: "text", label: "Full Name", required: true },
    { id: "2", type: "email", label: "Email Address", required: true },
  ]);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === "z") {
        if (e.shiftKey) {
          redo();
        } else {
          undo();
        }
      } else if ((e.ctrlKey || e.metaKey) && e.key === "y") {
        redo();
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [undo, redo]);

  const addField = () => {
    setFields([...fields, { id: Math.random().toString(), type: "text", label: "New Field", required: false }]);
  };

  const removeField = (id: string) => {
    setFields(fields.filter(f => f.id !== id));
  };

  const updateField = (id: string, updates: Partial<FormField>) => {
    const nextFields = fields.map(f => f.id === id ? { ...f, ...updates } : f);
    setFields(nextFields);
  };

  return (
    <div className="flex flex-col gap-6 h-[calc(100vh-14rem)]">
      {/* Toolbar */}
      <div className="flex items-center justify-between pb-4 border-b">
        <div className="flex items-center gap-2">
          <Button 
            variant="outline" 
            size="sm" 
            onClick={undo} 
            disabled={!canUndo}
            title="Undo (Ctrl+Z)"
          >
            <Undo2 className="w-4 h-4 mr-2" /> Undo
          </Button>
          <Button 
            variant="outline" 
            size="sm" 
            onClick={redo} 
            disabled={!canRedo}
            title="Redo (Ctrl+Y)"
          >
            <Redo2 className="w-4 h-4 mr-2" /> Redo
          </Button>
        </div>
        <div className="text-sm text-muted-foreground">
          {fields.length} Fields
        </div>
      </div>

      <div className="flex gap-6 overflow-hidden">
        <div className="flex-1 overflow-y-auto space-y-4 pr-2">
          {fields.map((field) => (
            <Card key={field.id} className="relative group border-border shadow-sm hover:border-primary/50">
              <CardContent className="p-4 flex gap-4 items-start">
                <div className="pt-2 cursor-grab text-foreground-muted">
                  <GripVertical className="w-5 h-5" />
                </div>
                <div className="flex-1 space-y-3">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="text-xs font-medium text-foreground-secondary mb-1 block">Field Label</label>
                      <Input 
                        value={field.label} 
                        onChange={(e) => updateField(field.id, { label: e.target.value })}
                      />
                    </div>
                    <div>
                      <label className="text-xs font-medium text-foreground-secondary mb-1 block">Input Type</label>
                      <select 
                        className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                        value={field.type}
                        onChange={(e) => updateField(field.id, { type: e.target.value })}
                      >
                        <option value="text">Short Text</option>
                        <option value="textarea">Long Text</option>
                        <option value="email">Email</option>
                        <option value="phone">Phone</option>
                        <option value="select">Dropdown</option>
                        <option value="checkbox">Checkbox</option>
                      </select>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <input 
                      type="checkbox" 
                      id={`req-${field.id}`} 
                      checked={field.required}
                      onChange={(e) => updateField(field.id, { required: e.target.checked })}
                      className="rounded border-border-strong text-primary"
                    />
                    <label htmlFor={`req-${field.id}`} className="text-sm text-foreground-secondary">Required field</label>
                  </div>
                </div>
                <Button 
                  variant="ghost" 
                  size="icon" 
                  className="text-foreground-muted hover:text-red-500"
                  onClick={() => removeField(field.id)}
                >
                  <Trash2 className="w-4 h-4" />
                </Button>
              </CardContent>
            </Card>
          ))}
          
          <Button 
            variant="outline" 
            className="w-full border-dashed border-2 py-8 text-muted-foreground hover:border-primary hover:text-primary transition-colors"
            onClick={addField}
          >
            <Plus className="w-5 h-5 mr-2" /> Add Form Field
          </Button>
        </div>

        <div className="w-80 border-l pl-6 space-y-6 hidden lg:block overflow-y-auto">
          <div>
            <h3 className="font-semibold text-lg mb-2">Form Settings</h3>
            <p className="text-sm text-foreground-secondary mb-4">Configure form behavior after submission.</p>
            <div className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-1 block">Success Message</label>
                <Input defaultValue="Thank you for your submission!" />
              </div>
              <div>
                <label className="text-sm font-medium mb-1 block">Redirect URL</label>
                <Input placeholder="https://..." />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

