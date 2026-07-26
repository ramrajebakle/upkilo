'use client';

import React from 'react';
import { AlertTriangle, RefreshCw, Save, X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { IBooking } from '@/types';

interface ConflictResolutionDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onResolve: (strategy: 'overwrite' | 'refresh') => void;
  currentData: Partial<IBooking>;
  serverData: Partial<IBooking>;
  entityName: string;
}

export function ConflictResolutionDialog({
  isOpen,
  onClose,
  onResolve,
  currentData,
  serverData,
  entityName
}: ConflictResolutionDialogProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-2xl overflow-hidden animate-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="bg-amber-50 px-6 py-4 border-b border-amber-100 flex items-center justify-between">
          <div className="flex items-center gap-3 text-amber-800">
            <div className="p-2 bg-amber-100 rounded-lg">
              <AlertTriangle className="h-5 w-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold">Conflict Detected</h2>
              <p className="text-sm opacity-90">This {entityName} was modified by someone else while you were editing.</p>
            </div>
          </div>
          <button onClick={onClose} className="p-1 hover:bg-amber-100 rounded-full transition-colors">
            <X className="h-5 w-5 text-amber-600" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6">
          <div className="grid grid-cols-2 gap-6">
            {/* Your Changes */}
            <div className="space-y-4">
              <h3 className="text-sm font-bold text-gray-500 uppercase tracking-wider">Your Changes (Pending)</h3>
              <div className="p-4 bg-primary-50 border border-primary-100 rounded-xl space-y-3">
                <div className="text-sm">
                  <span className="text-gray-500 block mb-1">Status</span>
                  <span className="font-semibold text-primary-900">{currentData.status || 'N/A'}</span>
                </div>
                <div className="text-sm">
                  <span className="text-gray-500 block mb-1">Notes</span>
                  <span className="font-semibold text-primary-900 line-clamp-3">{currentData.notes || 'No notes'}</span>
                </div>
              </div>
            </div>

            {/* Server Version */}
            <div className="space-y-4">
              <h3 className="text-sm font-bold text-gray-500 uppercase tracking-wider">Server Version (Current)</h3>
              <div className="p-4 bg-gray-50 border border-gray-100 rounded-xl space-y-3">
                <div className="text-sm">
                  <span className="text-gray-500 block mb-1">Status</span>
                  <span className="font-semibold text-gray-900">{serverData.status || 'N/A'}</span>
                </div>
                <div className="text-sm">
                  <span className="text-gray-500 block mb-1">Notes</span>
                  <span className="font-semibold text-gray-900 line-clamp-3">{serverData.notes || 'No notes'}</span>
                </div>
              </div>
            </div>
          </div>

          <div className="mt-8 p-4 bg-blue-50 border border-blue-100 rounded-xl flex items-start gap-3">
            <RefreshCw className="h-5 w-5 text-blue-600 mt-0.5" />
            <div className="text-sm text-blue-800">
              <p className="font-bold mb-1">How would you like to proceed?</p>
              <p>You can overwrite the server with your changes, or refresh to see the latest version and start over.</p>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-4 bg-gray-50 border-t border-gray-100 flex justify-end gap-3">
          <button
            onClick={() => onResolve('refresh')}
            className="px-4 py-2 text-sm font-bold text-gray-600 hover:bg-gray-200 transition-colors rounded-lg flex items-center gap-2"
          >
            <RefreshCw className="h-4 w-4" />
            Discard & Refresh
          </button>
          <button
            onClick={() => onResolve('overwrite')}
            className="px-6 py-2 text-sm font-bold text-white bg-primary-600 hover:bg-primary-700 shadow-md shadow-primary-200 transition-colors rounded-lg flex items-center gap-2"
          >
            <Save className="h-4 w-4" />
            Overwrite Server
          </button>
        </div>
      </div>
    </div>
  );
}
