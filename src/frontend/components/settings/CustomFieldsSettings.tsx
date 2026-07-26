'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Label } from '@/components/ui/Label';
import { Trash2, Plus, Loader2, Settings2 } from 'lucide-react';
import { apiClient } from '@/lib/api';
import { cn } from '@/lib/utils';

interface CustomField {
    id: string;
    name: string;
    type: string;
    targetEntity: string;
    isRequired: boolean;
    options?: string[]; // For select/multiselect
    sortOrder: number;
}

export function CustomFieldsSettings() {
    const [fields, setFields] = useState<CustomField[]>([]);
    const [loading, setLoading] = useState(true);
    const [newField, setNewField] = useState<Partial<CustomField>>({
        name: '',
        type: 'text',
        targetEntity: 'client',
        isRequired: false
    });

    useEffect(() => {
        loadFields();
    }, []);

    const loadFields = async () => {
        try {
            const res = await apiClient.get('/api/settings/custom-fields');
            setFields(res.data);
        } catch (error) {
            console.error('Failed to load custom fields', error);
        } finally {
            setLoading(false);
        }
    };

    const handleCreate = async () => {
        if (!newField.name) {
            alert('Name is required');
            return;
        }

        try {
            const res = await apiClient.post('/api/settings/custom-fields', newField);
            setFields([...fields, res.data]);
            setNewField({
                name: '',
                type: 'text',
                targetEntity: 'client',
                isRequired: false
            });
            alert('Field created successfully');
        } catch (error) {
            console.error(error);
            alert('Failed to create field');
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Are you sure you want to delete this field?')) return;

        try {
            await apiClient.delete(`/api/settings/custom-fields/${id}`);
            setFields(fields.filter(f => f.id !== id));
            alert('Field deleted');
        } catch (error) {
            console.error(error);
            alert('Failed to delete field');
        }
    };

    if (loading) return (
        <div className="py-24 flex flex-col items-center gap-6 text-slate-400">
            <Loader2 className="h-12 w-12 animate-spin text-primary-500" />
            <span className="text-[10px] font-black uppercase tracking-[0.4em]">Syncing Schema Matrix...</span>
        </div>
    );

    return (
        <div className="space-y-10 animate-fade-in">
            <div className="flex items-center justify-between">
                <div>
                    <h2 className="text-2xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Data Schematization</h2>
                    <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Configure extended entity metadata</p>
                </div>
                <div className="p-4 bg-primary-50 dark:bg-primary-900/30 rounded-2xl border border-primary-100 dark:border-primary-400/20 shadow-sm transition-all hover:scale-110">
                    <Settings2 className="h-8 w-8 text-primary-600 dark:text-primary-400" />
                </div>
            </div>

            <Card className="border-none bg-transparent shadow-none">
                <CardContent className="p-0 space-y-10">
                    {/* Create New Field */}
                    <div className="p-8 bg-white dark:bg-slate-900/50 border border-slate-100 dark:border-slate-800 rounded-[32px] shadow-2xl shadow-slate-200/40 dark:shadow-none backdrop-blur-xl">
                        <h4 className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.4em] mb-6 flex items-center gap-2">
                            <Plus className="h-4 w-4 text-primary-500" />
                            Provision New Metadata Node
                        </h4>
                        
                        <div className="grid gap-6 md:grid-cols-5 items-end">
                            <div className="space-y-2 col-span-1">
                                <Label className="text-[9px] font-black uppercase tracking-widest text-slate-400 pl-1">Target Entity</Label>
                                <select
                                    className="w-full h-12 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 px-4 text-xs font-bold uppercase tracking-widest text-slate-900 dark:text-white focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 outline-none transition-all appearance-none cursor-pointer"
                                    value={newField.targetEntity}
                                    onChange={e => setNewField({ ...newField, targetEntity: e.target.value })}
                                >
                                    <option value="client">Client Node</option>
                                    <option value="booking">Booking Event</option>
                                    <option value="location">Spatial Unit</option>
                                </select>
                            </div>
                            <div className="space-y-2 col-span-2">
                                <Label className="text-[9px] font-black uppercase tracking-widest text-slate-400 pl-1">Descriptor Name</Label>
                                <Input
                                    placeholder="e.g. REFERRAL_DATA"
                                    value={newField.name}
                                    onChange={e => setNewField({ ...newField, name: e.target.value })}
                                    className="h-12 border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 px-4 text-xs font-bold uppercase tracking-widest text-slate-900 dark:text-white focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 rounded-xl"
                                />
                            </div>
                            <div className="space-y-2 col-span-1">
                                <Label className="text-[9px] font-black uppercase tracking-widest text-slate-400 pl-1">Field Type</Label>
                                <select
                                    className="w-full h-12 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 px-4 text-xs font-bold uppercase tracking-widest text-slate-900 dark:text-white focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 outline-none transition-all appearance-none cursor-pointer"
                                    value={newField.type}
                                    onChange={e => setNewField({ ...newField, type: e.target.value })}
                                >
                                    <option value="text">String (Text)</option>
                                    <option value="number">Numeric (Float)</option>
                                    <option value="date">Temporal (Date)</option>
                                    <option value="boolean">Logical (Check)</option>
                                    <option value="select">Categorical (Select)</option>
                                </select>
                            </div>
                            <div className="col-span-1">
                                <Button onClick={handleCreate} className="w-full h-12 rounded-xl font-black uppercase tracking-widest text-[10px] shadow-xl shadow-primary-500/20 active:scale-95 transition-all">
                                    <Plus className="h-5 w-5 mr-2" />
                                    Deploy Node
                                </Button>
                            </div>
                        </div>
                    </div>

                    {/* List Fields */}
                    <div className="bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[32px] overflow-hidden shadow-2xl shadow-slate-200/40 dark:shadow-none">
                        <div className="overflow-x-auto">
                            <table className="w-full text-left">
                                <thead>
                                    <tr className="border-b border-slate-50 dark:border-slate-850 bg-slate-50/50 dark:bg-slate-950/50">
                                        <th className="h-14 px-8 text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Entity Domain</th>
                                        <th className="h-14 px-8 text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Descriptor</th>
                                        <th className="h-14 px-8 text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Telemetry Type</th>
                                        <th className="h-14 px-8 text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-[0.3em] text-right">Operations</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-50 dark:divide-slate-850">
                                    {fields.length === 0 ? (
                                        <tr>
                                            <td colSpan={4} className="py-24 text-center">
                                                <div className="flex flex-col items-center gap-4 opacity-30 grayscale">
                                                    <Settings2 className="h-12 w-12 text-slate-400" />
                                                    <p className="text-[10px] font-black uppercase tracking-[0.4em] text-slate-500">
                                                        Zero Extended Nodes Allocated
                                                    </p>
                                                </div>
                                            </td>
                                        </tr>
                                    ) : (
                                        fields.map(field => (
                                            <tr key={field.id} className="group hover:bg-slate-50/50 dark:hover:bg-slate-950/50 transition-colors">
                                                <td className="px-8 py-6">
                                                    <span className="px-3 py-1 bg-primary-50 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 text-[9px] font-black rounded-lg uppercase tracking-widest border border-primary-100 dark:border-primary-500/20">
                                                        {field.targetEntity}
                                                    </span>
                                                </td>
                                                <td className="px-8 py-6 align-middle font-black text-slate-900 dark:text-white text-xs uppercase tracking-widest">{field.name}</td>
                                                <td className="px-8 py-6">
                                                    <div className="flex items-center gap-2">
                                                        <div className="w-1.5 h-1.5 rounded-full bg-slate-300 dark:bg-slate-700" />
                                                        <span className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest">{field.type}</span>
                                                    </div>
                                                </td>
                                                <td className="px-8 py-6 text-right">
                                                    <button 
                                                        onClick={() => handleDelete(field.id)}
                                                        className="h-10 w-10 inline-flex items-center justify-center text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-400/10 rounded-xl transition-all border border-transparent hover:border-rose-100 dark:hover:border-rose-400/20"
                                                    >
                                                        <Trash2 className="h-4.5 w-4.5" />
                                                    </button>
                                                </td>
                                            </tr>
                                        ))
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
