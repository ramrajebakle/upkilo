'use client';

import React from 'react';
import { X, CheckSquare, Trash2, ChevronDown } from 'lucide-react';

interface BulkAction {
  label: string;
  icon?: React.ReactNode;
  onClick: () => void;
  destructive?: boolean;
  disabled?: boolean;
}

interface BulkActionsBarProps {
  selectedCount: number;
  totalCount: number;
  actions: BulkAction[];
  onSelectAll: () => void;
  onClearSelection: () => void;
  isAllSelected: boolean;
}

export function BulkActionsBar({
  selectedCount,
  totalCount,
  actions,
  onSelectAll,
  onClearSelection,
  isAllSelected,
}: BulkActionsBarProps) {
  if (selectedCount === 0) return null;

  return (
    <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 animate-in slide-in-from-bottom-4 duration-200">
      <div className="flex items-center gap-3 bg-slate-900 text-white rounded-2xl shadow-2xl px-4 py-3 border border-slate-700">
        {/* Selection info */}
        <div className="flex items-center gap-2">
          <button
            onClick={isAllSelected ? onClearSelection : onSelectAll}
            className="flex items-center gap-1.5 text-slate-300 hover:text-white transition-colors"
          >
            <CheckSquare className="h-4 w-4" />
            <span className="text-sm font-semibold">{selectedCount}</span>
          </button>
          <span className="text-slate-300 text-sm">of {totalCount} selected</span>
        </div>

        <div className="h-5 w-px bg-slate-700" />

        {/* Actions */}
        <div className="flex items-center gap-1">
          {actions.map((action, idx) => (
            <button
              key={idx}
              onClick={action.onClick}
              disabled={action.disabled}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium transition-colors disabled:opacity-50 ${
                action.destructive
                  ? 'text-red-400 hover:bg-red-500/20 hover:text-red-300'
                  : 'text-slate-200 hover:bg-slate-700'
              }`}
            >
              {action.icon}
              {action.label}
            </button>
          ))}
        </div>

        <div className="h-5 w-px bg-slate-700" />

        {/* Clear */}
        <button
          onClick={onClearSelection}
          className="p-1.5 text-slate-400 hover:text-white rounded-lg hover:bg-slate-700 transition-colors"
          title="Clear selection"
        >
          <X className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}
