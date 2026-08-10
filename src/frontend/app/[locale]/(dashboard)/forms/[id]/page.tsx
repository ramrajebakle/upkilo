'use client';

import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { GripVertical, Plus, Trash2, Undo2, Redo2, ArrowLeft, FileText, PlusCircle, Tag, Settings, Layout, Save, CheckCircle2, Info, ChevronUp, ChevronDown, Type, Copy, List, CheckSquare, Eye } from 'lucide-react';
import { useHistoryState } from '@/hooks/useHistoryState';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import { ConfirmModal } from '@/components/ui/Modal';

const fieldSchema = z.object({
    id: z.string(),
    label: z.string().min(1, 'Field label is required'),
    type: z.string().min(1, 'Field type is required'),
    required: z.boolean(),
    options: z.array(z.string()).optional(),
});

const formSchema = z.object({
    name: z.string().min(2, 'Form name must be at least 2 characters'),
    type: z.string().min(1, 'Please select a form type'),
    status: z.string().min(1, 'Please select a status'),
    isRequired: z.boolean(),
    fields: z.array(fieldSchema).min(1, 'Please add at least one field'),
});

type FormBuilderData = z.infer<typeof formSchema>;

export default function EditFormPage() {
    const router = useRouter();
    const params = useParams();
    const id = params.id as string;
    const { success: toastSuccess, error: toastError } = useToast();
    const [loading, setLoading] = useState(false);
    const [fetching, setFetching] = useState(true);
    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);
    const [responses, setResponses] = useState(0);

    const {
        register,
        control,
        handleSubmit,
        formState: { errors },
        setValue,
        reset,
        watch,
    } = useForm<FormBuilderData>({
        resolver: zodResolver(formSchema),
    });

    const { fields, append, remove, move, replace } = useFieldArray({
        control,
        name: "fields"
    });

    const watchedFields = watch('fields');
    const [historyFields, setHistoryFields, { undo, redo, canUndo, canRedo }] = useHistoryState<any[]>(watchedFields || []);

    // Sync form -> history
    useEffect(() => {
        if (watchedFields) {
            setHistoryFields(watchedFields);
        }
    }, [watchedFields, setHistoryFields]);

    // Sync history -> form (Internal effect to handle undo/redo)
    const handleUndo = () => {
        const prevState = undo();
        if (prevState) replace(prevState);
    };

    const handleRedo = () => {
        const nextState = redo();
        if (nextState) replace(nextState);
    };

    // Keyboard shortcuts
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'z') {
                if (e.shiftKey) {
                    handleRedo();
                } else {
                    handleUndo();
                }
            } else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'y') {
                handleRedo();
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [historyFields]); // Re-bind with latest state

    const formName = watch('name');

    useEffect(() => {
        const fetchForm = async () => {
            if (!id) return;
            setFetching(true);
            try {
                const res = await api.forms.get(id);
                const data = res.data;
                setResponses(data.responses || 0);
                reset(data);
            } catch (error) {
                console.error('Failed to fetch form', error);
                toastError('Failed to load form details');
                router.push('/forms');
            } finally {
                setFetching(false);
            }
        };
        fetchForm();
    }, [id, router, toastError, reset]);

    const addField = () => {
        append({
            id: Math.random().toString(36).substr(2, 9),
            label: 'New Field',
            type: 'text',
            required: false,
        });
    };

    const onSubmit = async (data: FormBuilderData) => {
        setLoading(true);
        try {
            const submissionData = {
                ...data,
                fieldCount: data.fields.length,
            };
            await api.forms.update(id, submissionData);
            toastSuccess('Form updated successfully');
            router.push('/forms?updated=true');
        } catch (error) {
            console.error('Failed to update form', error);
            toastError('Failed to update form');
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async () => {
        setIsDeleting(true);
        try {
            await api.forms.delete(id);
            toastSuccess('Form deleted successfully');
            router.push('/forms?deleted=true');
        } catch (error) {
            console.error('Failed to delete form', error);
            toastError('Failed to delete form');
        } finally {
            setIsDeleting(false);
            setIsDeleteModalOpen(false);
        }
    };

    if (fetching) {
        return (
            <div className="max-w-5xl mx-auto animate-pulse space-y-8">
                <div className="h-20 bg-slate-100 rounded-2xl w-full" />
                <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
                    <div className="lg:col-span-1 h-96 bg-slate-50 rounded-2xl" />
                    <div className="lg:col-span-3 h-96 bg-slate-50 rounded-2xl" />
                </div>
            </div>
        );
    }

    return (
        <div className="max-w-5xl mx-auto">
            {/* Header */}
            <div className="flex items-center gap-4 mb-8 animate-fade-in-up">
                <Link
                    href="/forms"
                    className="p-2 hover:bg-slate-100 rounded-xl transition-colors"
                >
                    <ArrowLeft className="h-5 w-5 text-slate-600" />
                </Link>
                <div className="flex-1">
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-xl shadow-lg shadow-blue-500/25">
                            <FileText className="h-5 w-5 text-white" />
                        </div>
                        <h1
                            className="text-2xl font-bold text-slate-900"
                            style={{ fontFamily: 'var(--font-display)' }}
                        >
                            Edit Form: {formName}
                        </h1>
                    </div>
                    <p className="text-slate-500 ml-12">Update your custom intake or consent form</p>
                </div>
                <div className="flex gap-2">
                    <div className="flex items-center gap-1 mr-4 pr-4 border-r border-slate-200">
                        <button
                            type="button"
                            onClick={handleUndo}
                            disabled={!canUndo}
                            className="p-2 hover:bg-slate-100 text-slate-500 disabled:opacity-30 rounded-xl transition-colors"
                            title="Undo (Ctrl+Z)"
                        >
                            <Undo2 className="h-5 w-5" />
                        </button>
                        <button
                            type="button"
                            onClick={handleRedo}
                            disabled={!canRedo}
                            className="p-2 hover:bg-slate-100 text-slate-500 disabled:opacity-30 rounded-xl transition-colors"
                            title="Redo (Ctrl+Y)"
                        >
                            <Redo2 className="h-5 w-5" />
                        </button>
                    </div>
                    <button 
                        type="button"
                        onClick={() => setIsDeleteModalOpen(true)}
                        className="p-2 hover:bg-red-50 text-red-400 hover:text-red-600 rounded-xl transition-colors"
                    >
                        <Trash2 className="h-5 w-5" />
                    </button>
                    <button type="button" className="p-2 hover:bg-slate-100 text-slate-400 hover:text-indigo-600 rounded-xl transition-colors">
                        <Eye className="h-5 w-5" />
                    </button>
                </div>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="space-y-6 pb-20">
                <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
                    {/* Sidebar Configuration */}
                    <div className="lg:col-span-1 space-y-6">
                        <div className="card-elevated p-5 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                            <h2 className="text-sm font-semibold text-slate-900 mb-4 flex items-center gap-2">
                                <Settings className="h-4 w-4 text-slate-400" />
                                Form Settings
                            </h2>
                            <div className="space-y-4">
                                <div>
                                    <label className="block text-xs font-medium text-slate-500 mb-1.5 uppercase tracking-wider">
                                        Form Name <span className="text-red-500">*</span>
                                    </label>
                                    <input
                                        {...register('name')}
                                        type="text"
                                        className={cn("input text-sm", errors.name && "border-red-500")}
                                    />
                                    {errors.name && <p className="text-xs text-red-500 mt-1">{errors.name.message}</p>}
                                </div>
                                <div>
                                    <label className="block text-xs font-medium text-slate-500 mb-1.5 uppercase tracking-wider">
                                        Type <span className="text-red-500">*</span>
                                    </label>
                                    <select
                                        {...register('type')}
                                        className={cn("input text-sm", errors.type && "border-red-500")}
                                    >
                                        <option value="intake">Intake Form</option>
                                        <option value="consent">Consent Waiver</option>
                                        <option value="feedback">Feedback Form</option>
                                        <option value="custom">Custom Form</option>
                                    </select>
                                    {errors.type && <p className="text-xs text-red-500 mt-1">{errors.type.message}</p>}
                                </div>
                                <div>
                                    <label className="block text-xs font-medium text-slate-500 mb-1.5 uppercase tracking-wider">
                                        Status <span className="text-red-500">*</span>
                                    </label>
                                    <select
                                        {...register('status')}
                                        className={cn("input text-sm", errors.status && "border-red-500")}
                                    >
                                        <option value="active">Active</option>
                                        <option value="draft">Draft</option>
                                        <option value="disabled">Disabled</option>
                                    </select>
                                    {errors.status && <p className="text-xs text-red-500 mt-1">{errors.status.message}</p>}
                                </div>
                                <label className="flex items-center gap-3 p-3 bg-slate-50 rounded-xl cursor-pointer hover:bg-slate-100 transition-colors group">
                                    <input
                                        {...register('isRequired')}
                                        type="checkbox"
                                        className="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-500"
                                    />
                                    <span className="text-xs font-medium text-slate-700">Require before booking</span>
                                </label>
                            </div>
                        </div>

                        <div className="card-elevated p-5 animate-fade-in-up" style={{ animationDelay: '200ms' }}>
                            <h2 className="text-sm font-semibold text-slate-900 mb-4 flex items-center gap-2">
                                <Info className="h-4 w-4 text-blue-500" />
                                Usage Stats
                            </h2>
                            <div className="space-y-4">
                                <div className="text-center p-3 bg-slate-50 rounded-xl border border-slate-100">
                                    <p className="text-xl font-bold text-slate-900">{responses}</p>
                                    <p className="text-[10px] text-slate-400 uppercase tracking-widest font-bold">Total Responses</p>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Main Builder */}
                    <div className="lg:col-span-3 space-y-6">
                        <div className="card-elevated p-6 animate-fade-in-up shadow-indigo-500/5 border-indigo-100" style={{ animationDelay: '150ms' }}>
                            <div className="flex items-center justify-between mb-8">
                                <h2 className="text-lg font-bold text-slate-900 flex items-center gap-2">
                                    <Layout className="h-5 w-5 text-indigo-500" />
                                    Form Layout
                                </h2>
                                <button
                                    type="button"
                                    onClick={addField}
                                    className="btn btn-secondary text-xs h-9 flex items-center gap-2"
                                >
                                    <PlusCircle className="h-4 w-4" />
                                    Add Field
                                </button>
                            </div>

                            <div className="space-y-4">
                                {fields.map((field, index) => (
                                    <div 
                                        key={field.id}
                                        className="group p-5 bg-white border border-slate-200 rounded-2xl hover:border-indigo-300 hover:shadow-md transition-all relative animate-fade-in-up"
                                        style={{ animationDelay: `${index * 50}ms` }}
                                    >
                                        <div className="flex flex-col md:flex-row gap-4 items-start">
                                            <div className="flex-1 space-y-4 w-full">
                                                <div className="flex gap-4">
                                                    <div className="flex-1">
                                                        <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1.5 ml-1">Field Label <span className="text-red-500">*</span></label>
                                                        <input
                                                            {...register(`fields.${index}.label`)}
                                                            type="text"
                                                            className={cn("w-full bg-slate-50 border-transparent focus:bg-white focus:border-indigo-500 rounded-xl px-4 py-2 text-sm transition-all", errors.fields?.[index]?.label && "border-red-500")}
                                                        />
                                                        {errors.fields?.[index]?.label && <p className="text-[10px] text-red-500 mt-1">{errors.fields[index]?.label?.message}</p>}
                                                    </div>
                                                    <div className="w-40">
                                                        <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1.5 ml-1">Field Type <span className="text-red-500">*</span></label>
                                                        <select
                                                            {...register(`fields.${index}.type`)}
                                                            className={cn("w-full bg-slate-50 border-transparent focus:bg-white focus:border-indigo-500 rounded-xl px-4 py-2 text-sm transition-all appearance-none", errors.fields?.[index]?.type && "border-red-500")}
                                                        >
                                                            <option value="text">Text Input</option>
                                                            <option value="textarea">Text Area</option>
                                                            <option value="select">Dropdown</option>
                                                            <option value="checkbox">Checkbox</option>
                                                            <option value="date">Date Picker</option>
                                                        </select>
                                                        {errors.fields?.[index]?.type && <p className="text-[10px] text-red-500 mt-1">{errors.fields[index]?.type?.message}</p>}
                                                    </div>
                                                </div>
                                                
                                                {watch(`fields.${index}.type`) === 'select' && (
                                                    <div className="animate-in slide-in-from-top-2 duration-200">
                                                        <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-widest mb-1.5 ml-1">Dropdown Options (Comma separated)</label>
                                                        <input
                                                            type="text"
                                                            placeholder="Red, Green, Blue"
                                                            defaultValue={field.options?.join(', ') || ''}
                                                            onChange={(e) => setValue(`fields.${index}.options`, e.target.value.split(',').map(s => s.trim()))}
                                                            className="w-full bg-slate-50 border-transparent focus:bg-white focus:border-indigo-500 rounded-xl px-4 py-2 text-sm transition-all"
                                                        />
                                                    </div>
                                                )}
                                            </div>

                                            <div className="flex md:flex-col gap-2 h-full justify-between items-center md:items-end">
                                                <div className="flex gap-1">
                                                    <button 
                                                        type="button"
                                                        disabled={index === 0}
                                                        onClick={() => move(index, index - 1)}
                                                        className="p-1.5 hover:bg-slate-100 rounded-lg disabled:opacity-30"
                                                    >
                                                        <ChevronUp className="h-4 w-4 text-slate-400" />
                                                    </button>
                                                    <button 
                                                        type="button"
                                                        disabled={index === fields.length - 1}
                                                        onClick={() => move(index, index + 1)}
                                                        className="p-1.5 hover:bg-slate-100 rounded-lg disabled:opacity-30"
                                                    >
                                                        <ChevronDown className="h-4 w-4 text-slate-400" />
                                                    </button>
                                                </div>
                                                
                                                <div className="flex items-center gap-4">
                                                    <label className="flex items-center gap-2 cursor-pointer group/req">
                                                        <input 
                                                            {...register(`fields.${index}.required`)}
                                                            type="checkbox"
                                                            className="w-3.5 h-3.5 rounded text-indigo-600 focus:ring-indigo-500" 
                                                        />
                                                        <span className="text-[10px] font-bold text-slate-400 group-hover/req:text-indigo-400 transition-colors uppercase tracking-widest">Required</span>
                                                    </label>
                                                    <button 
                                                        type="button"
                                                        onClick={() => remove(index)}
                                                        className="p-2 hover:bg-red-50 text-red-300 hover:text-red-500 rounded-xl transition-colors"
                                                    >
                                                        <Trash2 className="h-4 w-4" />
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))}

                                {errors.fields && !Array.isArray(errors.fields) && (
                                    <p className="text-xs text-red-500 text-center">{errors.fields.message as string}</p>
                                )}
                                
                                <button
                                    type="button"
                                    onClick={addField}
                                    className="w-full py-4 border-2 border-dashed border-slate-200 rounded-2xl text-slate-400 hover:text-indigo-500 hover:border-indigo-200 hover:bg-indigo-50/30 transition-all flex items-center justify-center gap-2 group"
                                >
                                    <PlusCircle className="h-5 w-5 group-hover:scale-110 transition-transform" />
                                    <span className="font-semibold uppercase tracking-widest text-xs">Add New Form Field</span>
                                </button>
                            </div>
                        </div>

                        <div className="sticky bottom-0 pt-4 bg-gradient-to-t from-slate-50 to-transparent">
                            <div className="flex gap-4">
                                <button
                                    type="submit"
                                    disabled={loading}
                                    className="flex-1 btn btn-primary py-4 shadow-xl shadow-primary-500/25"
                                >
                                    {loading ? (
                                        <>
                                            <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                                            Saving Changes...
                                        </>
                                    ) : (
                                        <>
                                            <Save className="h-5 w-5" />
                                            Save Changes
                                        </>
                                    )}
                                </button>
                                <Link
                                    href="/forms"
                                    className="px-8 btn btn-secondary flex items-center justify-center py-4"
                                >
                                    Cancel
                                </Link>
                            </div>
                        </div>
                    </div>
                </div>
            </form>

            <ConfirmModal
                isOpen={isDeleteModalOpen}
                onClose={() => setIsDeleteModalOpen(false)}
                onConfirm={handleDelete}
                title="Delete Form"
                description={`Are you sure you want to delete "${formName}"? All related data will be lost.`}
                confirmText="Delete"
                variant="danger"
                loading={isDeleting}
            />
        </div>
    );
}
