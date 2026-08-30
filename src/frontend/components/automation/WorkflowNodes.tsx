"use client";

import React, { memo } from 'react';
import { Handle, Position, NodeProps } from 'reactflow';
import { 
  Zap, User, CreditCard, Users, Star, PenTool, Play, 
  Mail, MessageSquare, Bell, CheckSquare, RefreshCcw, 
  Tag, FileText, Clock, Calendar, Globe, RotateCcw, 
  UserPlus, UserMinus, DollarSign, GitBranch, Split, Square,
  MoreHorizontal, ChevronRight
} from 'lucide-react';

const iconMap: Record<string, any> = {
  // Triggers
  BookingCreated: Zap, LoadingCancelled: Zap, BookingCompleted: Zap, BookingNoShow: Zap, BookingRescheduled: Zap,
  ClientCreated: User, ClientUpdated: User, ClientTagAdded: Tag,
  PaymentReceived: CreditCard, PaymentFailed: CreditCard, RefundIssued: RotateCcw,
  StaffCreated: Users, StaffScheduleChanged: Calendar,
  ReviewSubmitted: Star, FormSubmitted: PenTool, ManualTrigger: Play,
  // Actions
  SendEmail: Mail, SendSms: MessageSquare, SendPushNotification: Bell,
  CreateTask: CheckSquare, UpdateBookingStatus: RefreshCcw, UpdateClientTags: Tag, AddClientNote: FileText,
  Delay: Clock, WaitUntil: Calendar, CallWebhook: Globe, RequestReview: Star, IssueRefund: RotateCcw,
  AssignStaff: UserPlus, RemoveStaff: UserMinus, ChargeCreditCard: DollarSign, SubWorkflow: GitBranch,
  ConditionBranch: Split, EndWorkflow: Square
};

export const TriggerNode = memo(({ data, isConnectable }: NodeProps) => {
  const Icon = iconMap[data.type] || Zap;
  
  return (
    <div className="px-4 py-3 shadow-lg rounded-xl bg-card border-2 border-amber-500 min-w-[200px] border-l-[6px]">
      <div className="flex items-center">
        <div className="rounded-lg p-2 bg-warning-surface text-warning-fg">
          <Icon size={18} />
        </div>
        <div className="ml-3">
          <div className="text-xs font-bold text-warning-fg uppercase tracking-wider">Trigger</div>
          <div className="text-sm font-bold text-foreground">{data.label}</div>
        </div>
      </div>
      <Handle
        type="source"
        position={Position.Bottom}
        style={{ background: '#f59e0b', width: 10, height: 10 }}
        isConnectable={isConnectable}
      />
    </div>
  );
});

export const ActionNode = memo(({ data, isConnectable }: NodeProps) => {
  const Icon = iconMap[data.type] || Mail;
  const isConfigured = !!data.config;

  return (
    <div className={`px-4 py-3 shadow-md rounded-xl bg-card border-2 ${isConfigured ? 'border-blue-500' : 'border-border-strong'} min-w-[200px] border-l-[6px]`}>
      <Handle
        type="target"
        position={Position.Top}
        style={{ background: isConfigured ? '#3b82f6' : '#d1d5db', width: 8, height: 8 }}
        isConnectable={isConnectable}
      />
      <div className="flex items-center">
        <div className={`rounded-lg p-2 ${isConfigured ? 'bg-info-surface text-info-fg' : 'bg-muted text-foreground-secondary'}`}>
          <Icon size={18} />
        </div>
        <div className="ml-3 flex-1">
          <div className={`text-xs font-bold ${isConfigured ? 'text-info-fg' : 'text-foreground-muted'} uppercase tracking-wider`}>Action</div>
          <div className="text-sm font-bold text-foreground">{data.label}</div>
        </div>
        <ChevronRight className="w-4 h-4 text-gray-300" />
      </div>
      <Handle
        type="source"
        position={Position.Bottom}
        style={{ background: isConfigured ? '#3b82f6' : '#d1d5db', width: 8, height: 8 }}
        isConnectable={isConnectable}
      />
    </div>
  );
});

export const LogicNode = memo(({ data, isConnectable }: NodeProps) => {
  const Icon = iconMap[data.type] || Split;
  const isCondition = data.type === 'ConditionBranch';

  return (
    <div className={`px-4 py-3 shadow-md rounded-xl bg-card border-2 border-primary-500 min-w-[200px] border-l-[6px]`}>
      <Handle
        type="target"
        position={Position.Top}
        style={{ background: '#a855f7', width: 8, height: 8 }}
        isConnectable={isConnectable}
      />
      <div className="flex items-center">
        <div className="rounded-lg p-2 bg-brand-subtle text-primary">
          <Icon size={18} />
        </div>
        <div className="ml-3">
          <div className="text-xs font-bold text-primary uppercase tracking-wider">Logic</div>
          <div className="text-sm font-bold text-foreground">{data.label}</div>
        </div>
      </div>
      
      {isCondition ? (
        <div className="flex justify-between mt-2 pt-2 border-t border-border-subtle">
          <div className="relative h-4 flex-1">
            <span className="text-[10px] font-bold text-success-fg">YES</span>
            <Handle
              type="source"
              position={Position.Bottom}
              id="yes"
              style={{ left: '25%', background: '#22c55e', width: 10, height: 10 }}
              isConnectable={isConnectable}
            />
          </div>
          <div className="relative h-4 flex-1 text-right">
            <span className="text-[10px] font-bold text-danger-fg">NO</span>
            <Handle
              type="source"
              position={Position.Bottom}
              id="no"
              style={{ left: '75%', background: '#ef4444', width: 10, height: 10 }}
              isConnectable={isConnectable}
            />
          </div>
        </div>
      ) : (
        <Handle
          type="source"
          position={Position.Bottom}
          style={{ background: '#a855f7', width: 10, height: 10 }}
          isConnectable={isConnectable}
        />
      )}
    </div>
  );
});

TriggerNode.displayName = 'TriggerNode';
ActionNode.displayName = 'ActionNode';
LogicNode.displayName = 'LogicNode';
