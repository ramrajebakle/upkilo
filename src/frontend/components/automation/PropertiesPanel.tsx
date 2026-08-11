"use client";

import React from 'react';
import { Node } from 'reactflow';
import { 
    X, Info, Settings, Zap, Play, Mail, MessageSquare, 
    Bell, CheckSquare, RefreshCcw, Tag, FileText, Clock, 
    Calendar, Globe, Star, RotateCcw, UserPlus, UserMinus, 
    DollarSign, GitBranch, Split, Square
} from 'lucide-react';

interface PropertiesPanelProps {
    selectedNode: Node | null;
    onUpdate: (id: string, data: any) => void;
    onClose: () => void;
}

const triggerOptions = [
    { value: 'BookingCreated', label: 'Booking Created' },
    { value: 'BookingCancelled', label: 'Booking Cancelled' },
    { value: 'BookingCompleted', label: 'Booking Completed' },
    { value: 'BookingNoShow', label: 'Booking No-Show' },
    { value: 'BookingRescheduled', label: 'Booking Rescheduled' },
    { value: 'ClientCreated', label: 'Client Created' },
    { value: 'ClientUpdated', label: 'Client Updated' },
    { value: 'ClientTagAdded', label: 'Tag Added' },
    { value: 'PaymentReceived', label: 'Payment Received' },
    { value: 'PaymentFailed', label: 'Payment Failed' },
    { value: 'RefundIssued', label: 'Refund Issued' },
    { value: 'StaffCreated', label: 'Staff Created' },
    { value: 'StaffScheduleChanged', label: 'Schedule Changed' },
    { value: 'ReviewSubmitted', label: 'Review Submitted' },
    { value: 'FormSubmitted', label: 'Form Submitted' },
    { value: 'ManualTrigger', label: 'Manual Trigger' },
];

const actionOptions = [
    { value: 'SendEmail', label: 'Send Email' },
    { value: 'SendSms', label: 'Send SMS' },
    { value: 'SendPushNotification', label: 'Push Notification' },
    { value: 'CreateTask', label: 'Create Task' },
    { value: 'UpdateBookingStatus', label: 'Update Booking Status' },
    { value: 'UpdateClientTags', label: 'Update Tags' },
    { value: 'AddClientNote', label: 'Add Note' },
    { value: 'Delay', label: 'Delay (Wait)' },
    { value: 'WaitUntil', label: 'Wait Until Date' },
    { value: 'ConditionBranch', label: 'If / Else' },
    { value: 'CallWebhook', label: 'Call Webhook' },
    { value: 'RequestReview', label: 'Request Review' },
    { value: 'IssueRefund', label: 'Issue Refund' },
    { value: 'AssignStaff', label: 'Assign Staff' },
    { value: 'RemoveStaff', label: 'Remove Staff' },
    { value: 'ChargeCreditCard', label: 'Charge Card' },
    { value: 'SubWorkflow', label: 'Run Sub-Workflow' },
    { value: 'EndWorkflow', label: 'End Execution' },
];

export const PropertiesPanel: React.FC<PropertiesPanelProps> = ({ selectedNode, onUpdate, onClose }) => {
    if (!selectedNode) return null;

    const data = selectedNode.data;
    const isTrigger = selectedNode.type === 'trigger';
    const isAction = selectedNode.type === 'action';
    const isLogic = selectedNode.type === 'logic';

    const handleChange = (field: string, value: any) => {
        onUpdate(selectedNode.id, {
            ...data,
            [field]: value
        });
    };

    const handleConfigChange = (field: string, value: any) => {
        onUpdate(selectedNode.id, {
            ...data,
            config: {
                ...(data.config || {}),
                [field]: value
            }
        });
    };

    return (
        <div className="w-96 border-l border-gray-200 bg-white h-full shadow-2xl fixed right-0 top-0 bottom-0 z-50 flex flex-col animate-in slide-in-from-right duration-300">
            {/* Header */}
            <div className={`p-6 border-b border-gray-100 flex justify-between items-center ${isTrigger ? 'bg-amber-50/50' : isAction ? 'bg-blue-50/50' : 'bg-primary-50/50'}`}>
                <div className="flex items-center">
                    <div className={`p-2 rounded-lg mr-3 ${isTrigger ? 'bg-amber-100 text-amber-600' : isAction ? 'bg-blue-100 text-blue-600' : 'bg-primary-100 text-primary-600'}`}>
                        <Settings size={20} />
                    </div>
                    <div>
                        <h3 className="font-bold text-gray-900 leading-tight">Node Settings</h3>
                        <p className="text-[10px] text-gray-400 font-mono tracking-tighter">{selectedNode.id}</p>
                    </div>
                </div>
                <button 
                  onClick={onClose} 
                  className="p-2 hover:bg-white rounded-full text-gray-400 hover:text-gray-600 transition-colors shadow-sm border border-transparent hover:border-gray-100"
                >
                    <X size={18} />
                </button>
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-6 space-y-6 scrollbar-thin scrollbar-thumb-gray-200">
                {/* Basic Info */}
                <section className="space-y-4">
                    <div>
                        <label className="block text-xs font-bold text-gray-500 uppercase tracking-widest mb-1">Display Name</label>
                        <input
                            type="text"
                            className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none transition-all"
                            value={data.label || ''}
                            onChange={(e) => handleChange('label', e.target.value)}
                        />
                    </div>

                    <div>
                        <label className="block text-xs font-bold text-gray-500 uppercase tracking-widest mb-1">
                            {isTrigger ? 'Trigger Type' : 'Type'}
                        </label>
                        <select
                            className="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:ring-2 focus:ring-primary/20 outline-none transition-all cursor-pointer"
                            value={data.type || ''}
                            onChange={(e) => {
                                const option = [...triggerOptions, ...actionOptions].find(o => o.value === e.target.value);
                                onUpdate(selectedNode.id, {
                                    ...data,
                                    type: e.target.value,
                                    label: option?.label || data.label
                                });
                            }}
                        >
                            <option value="">Select...</option>
                            <optgroup label="Triggers">
                                {triggerOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                            </optgroup>
                            <optgroup label="Actions & Logic">
                                {actionOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                            </optgroup>
                        </select>
                    </div>
                </section>

                {/* Dynamic Configuration */}
                <section className="pt-6 border-t border-gray-100">
                    <div className="flex items-center mb-4 text-primary">
                        <Info size={14} className="mr-2" />
                        <h4 className="text-xs font-bold uppercase tracking-widest">Execution Parameters</h4>
                    </div>

                    <div className="space-y-4">
                        {/* Email config */}
                        {(data.type === 'SendEmail' || data.type === 'SendSms') && (
                            <>
                                <div>
                                    <label className="block text-xs font-medium text-gray-600 mb-1">Subject / Header</label>
                                    <input
                                        type="text"
                                        className="w-full px-4 py-2 border border-gray-200 rounded-lg text-sm outline-none"
                                        value={data.config?.subject || ''}
                                        onChange={(e) => handleConfigChange('subject', e.target.value)}
                                        placeholder="Hello {{client_name}}!"
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs font-medium text-gray-600 mb-1">Message Body</label>
                                    <textarea
                                        className="w-full px-4 py-2 border border-gray-200 rounded-lg text-sm outline-none h-32 resize-none"
                                        value={data.config?.body || ''}
                                        onChange={(e) => handleConfigChange('body', e.target.value)}
                                        placeholder="Write your automated message here..."
                                    />
                                    <p className="text-[10px] text-gray-400 mt-1 italic">Use {"{{variable}}"} for dynamic fields</p>
                                </div>
                            </>
                        )}

                        {/* Delay config */}
                        {data.type === 'Delay' && (
                            <div className="flex space-x-4">
                                <div className="flex-1">
                                    <label className="block text-xs font-medium text-gray-600 mb-1">Value</label>
                                    <input
                                        type="number"
                                        className="w-full px-4 py-2 border border-gray-200 rounded-lg text-sm outline-none"
                                        value={data.config?.delayValue || 0}
                                        onChange={(e) => handleConfigChange('delayValue', parseInt(e.target.value))}
                                    />
                                </div>
                                <div className="flex-1">
                                    <label className="block text-xs font-medium text-gray-600 mb-1">Unit</label>
                                    <select
                                        className="w-full px-4 py-2 border border-gray-200 rounded-lg text-sm outline-none"
                                        value={data.config?.delayUnit || 'minutes'}
                                        onChange={(e) => handleConfigChange('delayUnit', e.target.value)}
                                    >
                                        <option value="minutes">Minutes</option>
                                        <option value="hours">Hours</option>
                                        <option value="days">Days</option>
                                    </select>
                                </div>
                            </div>
                        )}

                        {/* Webhook config */}
                        {data.type === 'CallWebhook' && (
                            <>
                                <div>
                                    <label className="block text-xs font-medium text-gray-600 mb-1">Endpoint URL</label>
                                    <input
                                        type="url"
                                        className="w-full px-4 py-2 border border-gray-200 rounded-lg text-sm outline-none"
                                        value={data.config?.webhookUrl || ''}
                                        onChange={(e) => handleConfigChange('webhookUrl', e.target.value)}
                                        placeholder="https://zapier.com/hooks/..."
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs font-medium text-gray-600 mb-1">Method</label>
                                    <select
                                        className="w-full px-4 py-2 border border-gray-200 rounded-lg text-sm outline-none"
                                        value={data.config?.method || 'POST'}
                                        onChange={(e) => handleConfigChange('method', e.target.value)}
                                    >
                                        <option value="POST">POST</option>
                                        <option value="GET">GET</option>
                                        <option value="PUT">PUT</option>
                                    </select>
                                </div>
                            </>
                        )}

                        {/* Default fallback */}
                        {!['SendEmail', 'SendSms', 'Delay', 'CallWebhook'].includes(data.type) && (
                            <div className="bg-gray-50 border border-dashed border-gray-200 rounded-xl p-6 text-center">
                                <Settings className="w-8 h-8 text-gray-300 mx-auto mb-2" />
                                <p className="text-sm text-gray-500 font-medium">Standard Execution</p>
                                <p className="text-xs text-gray-400">This node uses default platform parameters for {data.label}.</p>
                            </div>
                        )}
                    </div>
                </section>
            </div>

            {/* Footer */}
            <div className="p-6 border-t border-gray-100 bg-gray-50/50">
                <button
                    onClick={onClose}
                    className="w-full py-3 bg-white border border-gray-200 text-gray-700 font-bold rounded-xl text-sm hover:shadow-md transition-all"
                >
                    Update & Close
                </button>
                <div className="mt-4 flex items-center justify-center text-[10px] text-gray-300 uppercase tracking-widest font-bold">
                    <Square size={8} className="mr-2" />
                    Upkilo Automation Engine v1.0
                </div>
            </div>
        </div>
    );
};
