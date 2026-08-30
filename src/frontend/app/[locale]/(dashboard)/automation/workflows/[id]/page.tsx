"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { ReactFlowProvider, Node, Edge, useNodesState, useEdgesState } from 'reactflow';
import 'reactflow/dist/style.css';
import { WorkflowBuilder } from '@/components/automation/WorkflowBuilder';
import { useRouter, useParams } from 'next/navigation';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { ChevronLeft, Save, Play, History, CheckCircle, XCircle, Loader2, BarChart2 } from 'lucide-react';
import { toast } from 'sonner';

const initialNodes: Node[] = [
    { id: 'trigger-1', type: 'trigger', data: { label: 'Manual Trigger', type: 'ManualTrigger' }, position: { x: 400, y: 50 } },
];

export default function WorkflowEditPage() {
    const router = useRouter();
    const params = useParams();
    const id = params?.id as string;

    const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
    const [edges, setEdges, onEdgesChange] = useEdgesState([]);
    const [isSaving, setIsSaving] = useState(false);
    const [isTesting, setIsTesting] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [workflowName, setWorkflowName] = useState('');
    const [workflowStatus, setWorkflowStatus] = useState(false);
    const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null);

    useEffect(() => {
        const load = async () => {
            try {
                const res = await apiClient.get(`/api/v1/workflows/${id}`);
                const wf = res.data?.data || res.data;
                setWorkflowName(wf.name || '');
                setWorkflowStatus(wf.isActive ?? false);
                // Parse steps (ReactFlow graph)
                if (wf.steps) {
                    const parsed = typeof wf.steps === 'string' ? JSON.parse(wf.steps) : wf.steps;
                    if (parsed?.nodes) setNodes(parsed.nodes);
                    if (parsed?.edges) setEdges(parsed.edges);
                }
            } catch {
                toast.error('Failed to load workflow');
            } finally {
                setIsLoading(false);
            }
        };
        if (id) load();
    }, [id]);

    const handleTestRun = async () => {
        setIsTesting(true);
        setTestResult(null);
        try {
            const res = await apiClient.post(`/api/v1/workflows/${id}/test`, {});
            const data = res.data;
            setTestResult({ success: data.success, message: data.message });
            if (data.success) toast.success('Test run completed successfully');
            else toast.error(`Test run failed: ${data.error || data.message}`);
        } catch {
            setTestResult({ success: false, message: 'Test run failed' });
            toast.error('Failed to execute test run');
        } finally {
            setIsTesting(false);
        }
    };

    const handleSave = async () => {
        if (!workflowName.trim()) { toast.error('Please enter a workflow name'); return; }
        setIsSaving(true);
        try {
            const triggerNode = nodes.find(n => n.type === 'trigger');
            await apiClient.put(`/api/v1/workflows/${id}`, {
                Name: workflowName,
                TriggerType: triggerNode?.data?.type || 'ManualTrigger',
                Steps: { nodes, edges }
            });
            toast.success('Workflow saved!');
        } catch {
            toast.error('Failed to save workflow');
        } finally {
            setIsSaving(false);
        }
    };

    if (isLoading) return (
        <div className="h-screen flex items-center justify-center bg-muted">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
        </div>
    );

    return (
        <ReactFlowProvider>
            <div className="h-screen flex flex-col bg-card overflow-hidden">
                {/* Header */}
                <header className="h-16 border-b border-border px-6 flex items-center justify-between bg-card z-10 shrink-0">
                    <div className="flex items-center space-x-4">
                        <Button variant="ghost" size="icon" onClick={() => router.back()} className="rounded-full">
                            <ChevronLeft className="w-5 h-5" />
                        </Button>
                        <div className="h-6 w-[1px] bg-gray-200" />
                        <div className="flex flex-col">
                            <Input
                                value={workflowName}
                                onChange={e => setWorkflowName(e.target.value)}
                                className="h-8 py-0 px-2 text-lg font-bold border-transparent hover:border-border focus:border-primary bg-transparent transition-all w-64"
                                placeholder="Workflow name..."
                            />
                            <p className="text-[10px] text-foreground-muted px-2 uppercase font-bold tracking-widest">
                                {workflowStatus ? 'Active' : 'Paused'} · Visual Builder
                            </p>
                        </div>
                    </div>

                    <div className="flex items-center space-x-3">
                        {testResult && (
                            <div className={`flex items-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-lg ${testResult.success ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-600'}`}>
                                {testResult.success ? <CheckCircle className="w-3.5 h-3.5" /> : <XCircle className="w-3.5 h-3.5" />}
                                {testResult.message}
                            </div>
                        )}
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => router.push(`/automation/workflows/${id}/executions`)}
                            className="rounded-xl font-bold text-foreground-secondary"
                        >
                            <History className="w-4 h-4 mr-2" />
                            Execution Logs
                        </Button>
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={handleTestRun}
                            disabled={isTesting || isSaving}
                            className="rounded-xl font-bold text-primary border-primary/25 bg-brand-subtle hover:bg-brand-subtle"
                        >
                            {isTesting ? <Loader2 className="w-4 h-4 mr-2 animate-spin" /> : <Play className="w-4 h-4 mr-2" />}
                            {isTesting ? 'Testing...' : 'Test Run'}
                        </Button>
                        <Button onClick={handleSave} disabled={isSaving} className="rounded-xl font-bold px-6">
                            {isSaving ? <Loader2 className="w-4 h-4 mr-2 animate-spin" /> : <Save className="w-4 h-4 mr-2" />}
                            {isSaving ? 'Saving...' : 'Save'}
                        </Button>
                    </div>
                </header>

                {/* Builder */}
                <main className="flex-1 min-h-0 bg-muted/50">
                    <WorkflowBuilder
                        nodes={nodes}
                        edges={edges}
                        setNodes={setNodes}
                        setEdges={setEdges}
                        onNodesChange={onNodesChange}
                        onEdgesChange={onEdgesChange}
                    />
                </main>
            </div>
        </ReactFlowProvider>
    );
}
