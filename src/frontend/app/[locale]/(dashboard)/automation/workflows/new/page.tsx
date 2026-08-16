"use client";

import React, { useState, useCallback } from 'react';
import { ReactFlowProvider, Node, Edge, useNodesState, useEdgesState, Connection, addEdge } from 'reactflow';
import 'reactflow/dist/style.css';
import { WorkflowBuilder } from '@/components/automation/WorkflowBuilder';
import { useRouter } from 'next/navigation';
import { apiClient } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { ChevronLeft, Save, Play, Settings, CheckCircle, XCircle, Loader2 } from 'lucide-react';
import { toast } from 'sonner';

const initialNodes: Node[] = [
    {
        id: 'trigger-1',
        type: 'trigger',
        data: { label: 'Manual Trigger', type: 'ManualTrigger' },
        position: { x: 400, y: 50 },
    },
];

const initialEdges: Edge[] = [];

export default function WorkflowPage() {
    const router = useRouter();
    const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
    const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);
    const [isSaving, setIsSaving] = useState(false);
    const [isTesting, setIsTesting] = useState(false);
    const [savedWorkflowId, setSavedWorkflowId] = useState<string | null>(null);
    const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null);
    const [workflowName, setWorkflowName] = useState('New Automation');

    const handleTestRun = async () => {
        // Save first if not saved
        let workflowId = savedWorkflowId;
        if (!workflowId) {
            toast.info('Saving workflow before test run...');
            workflowId = await handleSave(true);
            if (!workflowId) return;
        }
        setIsTesting(true);
        setTestResult(null);
        try {
            const res = await apiClient.post(`/api/v1/workflows/${workflowId}/test`, {});
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

    const handleSave = async (returnId = false): Promise<string | null> => {
        if (!workflowName.trim()) {
            toast.error("Please enter a workflow name");
            return null;
        }

        setIsSaving(true);
        try {
            const triggerNode = nodes.find(n => n.type === 'trigger');
            const triggerType = triggerNode?.data?.type || 'ManualTrigger';

            const payload = {
                Name: workflowName,
                Description: `Automated workflow for ${triggerType}`,
                TriggerType: triggerType,
                Steps: { nodes, edges }
            };

            const res = savedWorkflowId
                ? await apiClient.put(`/api/v1/workflows/${savedWorkflowId}`, payload)
                : await apiClient.post('/api/v1/workflows', payload);

            const id = res.data?.id || res.data?.data?.id;
            if (id) setSavedWorkflowId(id);

            toast.success('Workflow saved successfully!');
            if (!returnId) router.push('/automation/workflows');
            return id || null;
        } catch (error) {
            console.error('Error saving workflow:', error);
            toast.error('Failed to save workflow. Please try again.');
            return null;
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <ReactFlowProvider>
            <div className="h-screen flex flex-col bg-white overflow-hidden">
                {/* Header */}
                <header className="h-16 border-b border-gray-200 px-6 flex items-center justify-between bg-white z-10 shrink-0">
                    <div className="flex items-center space-x-4">
                        <Button 
                            variant="ghost" 
                            size="icon" 
                            onClick={() => router.back()}
                            className="rounded-full"
                        >
                            <ChevronLeft className="w-5 h-5" />
                        </Button>
                        <div className="h-6 w-[1px] bg-gray-200" />
                        <div className="flex flex-col">
                            <Input
                                value={workflowName}
                                onChange={(e) => setWorkflowName(e.target.value)}
                                className="h-8 py-0 px-2 text-lg font-bold border-transparent hover:border-gray-200 focus:border-primary bg-transparent transition-all w-64"
                                placeholder="Enter workflow name..."
                            />
                            <p className="text-[10px] text-gray-400 px-2 uppercase font-bold tracking-widest">Visual Builder Mode</p>
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
                            onClick={handleTestRun}
                            disabled={isTesting || isSaving}
                            className="rounded-xl font-bold text-primary-600 border-primary-200 bg-primary-50 hover:bg-primary-100"
                        >
                            {isTesting ? <Loader2 className="w-4 h-4 mr-2 animate-spin" /> : <Play className="w-4 h-4 mr-2" />}
                            {isTesting ? 'Testing...' : 'Test Run'}
                        </Button>
                        <Button
                            onClick={() => handleSave(false)}
                            disabled={isSaving}
                            className="rounded-xl font-bold shadow-lg shadow-primary/20 px-6"
                        >
                            {isSaving ? <Loader2 className="w-4 h-4 mr-2 animate-spin" /> : <Save className="w-4 h-4 mr-2" />}
                            {isSaving ? 'Saving...' : 'Publish'}
                        </Button>
                    </div>
                </header>

                {/* Builder Area */}
                <main className="flex-1 min-h-0 bg-gray-50/50">
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
