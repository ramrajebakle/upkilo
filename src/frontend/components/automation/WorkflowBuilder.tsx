"use client";

import React, { useState, useCallback, useRef } from 'react';
import ReactFlow, {
    addEdge,
    Controls,
    Background,
    Connection,
    Edge,
    Node,
    applyNodeChanges,
    NodeChange,
    OnNodesChange,
    OnEdgesChange,
    ReactFlowInstance,
    MiniMap
} from 'reactflow';
import 'reactflow/dist/style.css';
import { PropertiesPanel } from './PropertiesPanel';
import { WorkflowSidebar } from './WorkflowSidebar';
import { TriggerNode, ActionNode, LogicNode } from './WorkflowNodes';

const nodeTypes = {
    trigger: TriggerNode,
    action: ActionNode,
    logic: LogicNode
};

interface WorkflowBuilderProps {
    nodes: Node[];
    edges: Edge[];
    setNodes: React.Dispatch<React.SetStateAction<Node[]>>;
    setEdges: React.Dispatch<React.SetStateAction<Edge[]>>;
    onNodesChange: OnNodesChange;
    onEdgesChange: OnEdgesChange;
}

let id = 0;
const getId = () => `node_${id++}`;

export const WorkflowBuilder: React.FC<WorkflowBuilderProps> = ({
    nodes, edges, setNodes, setEdges, onNodesChange, onEdgesChange
}) => {
    const reactFlowWrapper = useRef<HTMLDivElement>(null);
    const [reactFlowInstance, setReactFlowInstance] = useState<ReactFlowInstance | null>(null);
    const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);

    const onConnect = useCallback(
        (params: Connection) => setEdges((eds) => addEdge({ ...params, animated: true, style: { strokeWidth: 2 } }, eds)),
        [setEdges],
    );

    const onInit = (instance: ReactFlowInstance) => setReactFlowInstance(instance);

    const onDragOver = useCallback((event: React.DragEvent) => {
        event.preventDefault();
        event.dataTransfer.dropEffect = 'move';
    }, []);

    const onDrop = useCallback(
        (event: React.DragEvent) => {
            event.preventDefault();

            if (!reactFlowWrapper.current || !reactFlowInstance) return;

            const reactFlowBounds = reactFlowWrapper.current.getBoundingClientRect();
            const type = event.dataTransfer.getData('application/reactflow');
            const dataStr = event.dataTransfer.getData('application/reactflow-data');

            if (typeof type === 'undefined' || !type) return;

            const position = reactFlowInstance.project({
                x: event.clientX - reactFlowBounds.left,
                y: event.clientY - reactFlowBounds.top,
            });

            const nodeData = JSON.parse(dataStr);
            
            // Logic nodes are special
            const nodeType = (nodeData.type === 'ConditionBranch' || nodeData.type === 'Delay' || nodeData.type === 'WaitUntil' || nodeData.type === 'SubWorkflow' || nodeData.type === 'EndWorkflow') ? 'logic' : type;

            const newNode: Node = {
                id: getId(),
                type: nodeType,
                position,
                data: { ...nodeData },
            };

            setNodes((nds) => nds.concat(newNode));
        },
        [reactFlowInstance, setNodes],
    );

    const onNodeClick = useCallback((event: React.MouseEvent, node: Node) => {
        setSelectedNodeId(node.id);
    }, []);

    const onPaneClick = useCallback(() => {
        setSelectedNodeId(null);
    }, []);

    const handleNodeUpdate = (id: string, newData: any) => {
        setNodes((nds) =>
            nds.map((node) => (node.id === id ? { ...node, data: { ...newData } } : node))
        );
    };

    const selectedNode = nodes.find((n) => n.id === selectedNodeId) || null;

    return (
        <div className="flex w-full h-[calc(100vh-140px)] border border-border rounded-2xl overflow-hidden bg-muted shadow-inner">
            <WorkflowSidebar />
            
            <div className="flex-1 relative" ref={reactFlowWrapper}>
                <ReactFlow
                    nodes={nodes}
                    edges={edges}
                    onNodesChange={onNodesChange}
                    onEdgesChange={onEdgesChange}
                    onConnect={onConnect}
                    onInit={onInit}
                    onDrop={onDrop}
                    onDragOver={onDragOver}
                    onNodeClick={onNodeClick}
                    onPaneClick={onPaneClick}
                    nodeTypes={nodeTypes}
                    fitView
                    snapToGrid
                    snapGrid={[15, 15]}
                >
                    <Controls />
                    <MiniMap 
                        nodeColor={(n) => {
                            if (n.type === 'trigger') return '#fbbf24';
                            if (n.type === 'action') return '#3b82f6';
                            if (n.type === 'logic') return '#a855f7';
                            return '#eee';
                        }}
                    />
                    <Background gap={20} size={1} color="#e5e7eb" />
                </ReactFlow>

                {selectedNode && (
                    <PropertiesPanel
                        selectedNode={selectedNode}
                        onUpdate={handleNodeUpdate}
                        onClose={() => setSelectedNodeId(null)}
                    />
                )}
            </div>
        </div>
    );
};
