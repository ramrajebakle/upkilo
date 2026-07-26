"use client";

import React, { useState, useCallback } from "react";
import ReactFlow, {
  MiniMap,
  Controls,
  Background,
  useNodesState,
  useEdgesState,
  addEdge,
  Connection,
  Edge,
  Handle,
  Position,
  NodeProps
} from "reactflow";
import "reactflow/dist/style.css";
import { Zap, Mail, Shield, MessageSquare, Plus, Save, Play } from "lucide-react";
import { Button } from "@/components/ui/Button";

// Custom Nodes
const TriggerNode = ({ data }: NodeProps) => {
  return (
    <div className="bg-surface-0 border-2 border-warning-400 rounded-xl shadow-lg min-w-[200px] overflow-hidden">
      <div className="bg-warning-50 px-4 py-2 flex items-center gap-2 border-b border-warning-100">
        <Zap size={16} className="text-warning-600" />
        <span className="text-xs font-bold text-warning-700 uppercase tracking-wider">Trigger</span>
      </div>
      <div className="p-4">
        <div className="font-semibold text-text-primary text-sm">{data.label}</div>
        <div className="text-xs text-text-secondary mt-1">{data.description}</div>
      </div>
      <Handle type="source" position={Position.Bottom} className="w-3 h-3 bg-warning-500" />
    </div>
  );
};

const AIActionNode = ({ data }: NodeProps) => {
  return (
    <div className="bg-surface-0 border-2 border-ai-400 rounded-xl shadow-lg min-w-[200px] overflow-hidden">
      <Handle type="target" position={Position.Top} className="w-3 h-3 bg-ai-500" />
      <div className="bg-ai-50 px-4 py-2 flex items-center gap-2 border-b border-ai-100">
        <SparklesIcon />
        <span className="text-xs font-bold text-ai-700 uppercase tracking-wider">AI Action</span>
      </div>
      <div className="p-4">
        <div className="font-semibold text-text-primary text-sm">{data.label}</div>
        <div className="text-xs text-text-secondary mt-1">{data.description}</div>
      </div>
      <Handle type="source" position={Position.Bottom} className="w-3 h-3 bg-ai-500" />
    </div>
  );
};

const OutputNode = ({ data }: NodeProps) => {
  return (
    <div className="bg-surface-0 border-2 border-success-400 rounded-xl shadow-lg min-w-[200px] overflow-hidden">
      <Handle type="target" position={Position.Top} className="w-3 h-3 bg-success-500" />
      <div className="bg-success-50 px-4 py-2 flex items-center gap-2 border-b border-success-100">
        <Mail size={16} className="text-success-600" />
        <span className="text-xs font-bold text-success-700 uppercase tracking-wider">Output</span>
      </div>
      <div className="p-4">
        <div className="font-semibold text-text-primary text-sm">{data.label}</div>
      </div>
    </div>
  );
};

const SparklesIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-ai-600">
    <path d="m12 3-1.912 5.813a2 2 0 0 1-1.275 1.275L3 12l5.813 1.912a2 2 0 0 1 1.275 1.275L12 21l1.912-5.813a2 2 0 0 1 1.275-1.275L21 12l-5.813-1.912a2 2 0 0 1-1.275-1.275L12 3Z"/>
    <path d="M5 3v4"/><path d="M19 17v4"/><path d="M3 5h4"/><path d="M17 19h4"/>
  </svg>
);

const nodeTypes = {
  triggerNode: TriggerNode,
  aiActionNode: AIActionNode,
  outputNode: OutputNode,
};

const initialNodes = [
  {
    id: "1",
    type: "triggerNode",
    position: { x: 250, y: 50 },
    data: { label: "Customer Inactivity", description: "> 14 days no login" },
  },
  {
    id: "2",
    type: "aiActionNode",
    position: { x: 250, y: 200 },
    data: { label: "Analyse Usage Pattern", description: "Evaluate recent feature usage" },
  },
  {
    id: "3",
    type: "outputNode",
    position: { x: 100, y: 350 },
    data: { label: "Send Win-back Sequence", description: "" },
  },
  {
    id: "4",
    type: "outputNode",
    position: { x: 400, y: 350 },
    data: { label: "Draft Downgrade Email", description: "" },
  },
];

const initialEdges = [
  { id: "e1-2", source: "1", target: "2", animated: true, style: { stroke: '#94a3b8', strokeWidth: 2 } },
  { id: "e2-3", source: "2", target: "3", label: "Free plan", style: { stroke: '#94a3b8', strokeWidth: 2 } },
  { id: "e2-4", source: "2", target: "4", label: "Paid plan", style: { stroke: '#94a3b8', strokeWidth: 2 } },
];

export function WorkflowBuilder() {
  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

  const onConnect = useCallback(
    (params: Connection | Edge) => setEdges((eds) => addEdge({ ...params, animated: true }, eds)),
    [setEdges]
  );

  return (
    <div className="h-[700px] w-full border border-surface-200 rounded-2xl overflow-hidden shadow-sm bg-surface-50 relative flex flex-col animate-fade-in">
      <div className="bg-surface-0 border-b border-surface-200 p-4 flex justify-between items-center z-10">
        <div>
          <h2 className="text-lg font-bold text-text-primary">Churn Prevention Automation</h2>
          <p className="text-xs text-text-secondary">Last edited 2 days ago</p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" size="sm" leftIcon={<Plus size={14} />}>Add Node</Button>
          <Button variant="outline" size="sm" leftIcon={<Play size={14} />}>Test Run</Button>
          <Button variant="ai" size="sm" leftIcon={<Save size={14} />}>Publish</Button>
        </div>
      </div>
      
      <div className="flex-1 w-full">
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onConnect={onConnect}
          nodeTypes={nodeTypes}
          fitView
          attributionPosition="bottom-right"
        >
          <Controls />
          <MiniMap 
            nodeColor={(node) => {
              switch (node.type) {
                case 'triggerNode': return '#fbbf24';
                case 'aiActionNode': return '#c084fc';
                case 'outputNode': return '#4ade80';
                default: return '#eee';
              }
            }} 
            maskColor="rgba(0,0,0,0.05)" 
          />
          <Background color="#cbd5e1" gap={16} />
        </ReactFlow>
      </div>
    </div>
  );
}
