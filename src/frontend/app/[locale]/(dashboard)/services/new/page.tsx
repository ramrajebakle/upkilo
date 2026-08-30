'use client';

import React, { useState } from 'react';
import { useRouter, Link } from '@/navigation';
import { ArrowLeft, Sparkles, Wand2 } from 'lucide-react';
import { api } from '@/lib/api';
import { useToast } from '@/components/ui/Toast';
import ServiceForm from '@/components/forms/ServiceForm';

export default function NewServicePage() {
    const router = useRouter();
    const { success, error } = useToast();
    const [isLoading, setIsLoading] = useState(false);

    const onSubmit = async (data: any) => {
        try {
            setIsLoading(true);
            const response = await api.services.create(data);
            
            if (response.status === 201 || response.status === 200) {
                success('Service created successfully!');
                router.push('/services');
            }
        } catch (err) {
            console.error('Error creating service:', err);
            error('Failed to create service. Please try again.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="max-w-5xl mx-auto space-y-8 animate-fade-in pb-20">
            {/* Navigation & Header */}
            <div className="flex flex-col gap-6">
                <Link 
                    href="/services" 
                    className="flex items-center gap-2 text-foreground-secondary hover:text-primary transition-colors w-fit group"
                >
                    <div className="p-1.5 rounded-lg group-hover:bg-brand-subtle transition-colors">
                        <ArrowLeft className="w-4 h-4" />
                    </div>
                    <span className="text-sm font-medium">Back to Services</span>
                </Link>

                <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 bg-card p-8 rounded-3xl border border-border shadow-sm relative overflow-hidden">
                    {/* Decorative Elements */}
                    <div className="absolute top-0 right-0 w-64 h-64 bg-primary-500/5 rounded-full blur-3xl -translate-y-1/2 translate-x-1/2" />
                    <div className="absolute bottom-0 left-0 w-32 h-32 bg-ai-500/5 rounded-full blur-2xl translate-y-1/2 -translate-x-1/2" />

                    <div className="relative">
                        <div className="flex items-center gap-3 mb-2">
                            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-primary-500 to-primary-600 flex items-center justify-center shadow-lg shadow-primary-500/30">
                                <Sparkles className="w-5 h-5 text-white" />
                            </div>
                            <h1 className="text-3xl font-bold text-foreground tracking-tight">Add New Service</h1>
                        </div>
                        <p className="text-foreground-secondary max-w-md">
                            Create a new service offering for your clients. Define pricing, duration, and scheduling buffers.
                        </p>
                    </div>

                    <div className="flex gap-3 relative">
                        <button 
                            type="button"
                            className="btn btn-secondary bg-card border-border hover:bg-accent gap-2"
                            onClick={() => {}} // Placeholder for "AI Assist" or similar
                        >
                            <Wand2 className="w-4 h-4 text-primary" />
                            <span>AI Assist</span>
                        </button>
                    </div>
                </div>
            </div>

            {/* Form Component */}
            <div className="relative">
                <ServiceForm onSubmit={onSubmit} isLoading={isLoading} />
            </div>
        </div>
    );
}
