'use client';

import React, { useEffect, useState } from 'react';
import { useRouter, Link } from '@/navigation';
import { useParams } from 'next/navigation';
import { ArrowLeft, Loader2, Save, Trash2 } from 'lucide-react';
import { api } from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import ServiceForm from '@/components/forms/ServiceForm';

export default function EditServicePage() {
    const router = useRouter();
    const params = useParams();
    const id = params.id as string;
    const { success, error } = useToast();
    const [service, setService] = useState<any>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);

    useEffect(() => {
        if (id) {
            fetchService();
        }
    }, [id]);

    const fetchService = async () => {
        try {
            setIsLoading(true);
            const response = await api.services.get(id);
            if (response.data) {
                setService(response.data);
            }
        } catch (err) {
            console.error('Error fetching service:', err);
            error('Failed to load service data');
            router.push('/services');
        } finally {
            setIsLoading(false);
        }
    };

    const onSubmit = async (data: any) => {
        try {
            setIsSaving(true);
            const response = await api.services.update(id, data);
            
            if (response.status === 200 || response.status === 204) {
                success('Service updated successfully!');
                router.push('/services');
            }
        } catch (err) {
            console.error('Error updating service:', err);
            error('Failed to update service. Please try again.');
        } finally {
            setIsSaving(false);
        }
    };

    const handleDelete = async () => {
        if (!confirm('Are you sure you want to delete this service? This action cannot be undone.')) {
            return;
        }

        try {
            setIsSaving(true);
            const response = await api.services.delete(id);
            if (response.status === 200 || response.status === 204) {
                success('Service deleted successfully');
                router.push('/services');
            }
        } catch (err) {
            console.error('Error deleting service:', err);
            error('Failed to delete service');
        } finally {
            setIsSaving(false);
        }
    };

    if (isLoading) {
        return (
            <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4">
                <Loader2 className="w-12 h-12 text-primary-500 animate-spin" />
                <p className="text-slate-500 font-medium">Fetching service details...</p>
            </div>
        );
    }

    return (
        <div className="max-w-5xl mx-auto space-y-8 animate-fade-in pb-20">
            {/* Navigation & Header */}
            <div className="flex flex-col gap-6">
                <div className="flex items-center justify-between">
                    <Link 
                        href="/services" 
                        className="flex items-center gap-2 text-slate-500 hover:text-primary-600 transition-colors w-fit group"
                    >
                        <div className="p-1.5 rounded-lg group-hover:bg-primary-50 transition-colors">
                            <ArrowLeft className="w-4 h-4" />
                        </div>
                        <span className="text-sm font-medium">Back to Services</span>
                    </Link>
                    
                    <button 
                        onClick={handleDelete}
                        className="btn btn-secondary border-red-100 text-red-500 hover:bg-red-50 hover:border-red-200 gap-2"
                        disabled={isSaving}
                    >
                        <Trash2 className="w-4 h-4" />
                        <span className="hidden sm:inline">Delete Service</span>
                    </button>
                </div>

                <div className="bg-white p-8 rounded-3xl border border-slate-200 shadow-sm relative overflow-hidden">
                    {/* Decorative Elements */}
                    <div className="absolute top-0 right-0 w-64 h-64 bg-primary-500/5 rounded-full blur-3xl -translate-y-1/2 translate-x-1/2" />
                    
                    <div className="relative">
                        <div className="flex items-center gap-3 mb-2">
                            <div className="w-10 h-10 rounded-xl bg-slate-900 flex items-center justify-center shadow-lg">
                                <Save className="w-5 h-5 text-white" />
                            </div>
                            <h1 className="text-3xl font-bold text-slate-900 tracking-tight">Edit Service</h1>
                        </div>
                        <p className="text-slate-500 max-w-md">
                            Updating <span className="font-semibold text-slate-900">{service?.name}</span>. 
                            Changes will take effect immediately.
                        </p>
                    </div>
                </div>
            </div>

            {/* Form Component */}
            <div className="relative">
                <ServiceForm 
                    initialData={service} 
                    onSubmit={onSubmit} 
                    isLoading={isSaving} 
                />
            </div>
        </div>
    );
}
