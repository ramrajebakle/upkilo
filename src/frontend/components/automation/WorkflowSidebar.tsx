"use client";

import React from 'react';
import { 
  Zap, User, CreditCard, Users, Star, PenTool, Play, 
  Mail, MessageSquare, Bell, CheckSquare, RefreshCcw, 
  Tag, FileText, Clock, Calendar, Globe, RotateCcw, 
  UserPlus, UserMinus, DollarSign, GitBranch, Split, Square,
  Search
} from 'lucide-react';

const triggers = [
  { type: 'BookingCreated', label: 'Booking Created', icon: Zap, category: 'Booking' },
  { type: 'BookingCancelled', label: 'Booking Cancelled', icon: Zap, category: 'Booking' },
  { type: 'BookingCompleted', label: 'Booking Completed', icon: Zap, category: 'Booking' },
  { type: 'BookingNoShow', label: 'Booking No-Show', icon: Zap, category: 'Booking' },
  { type: 'BookingRescheduled', label: 'Booking Rescheduled', icon: Zap, category: 'Booking' },
  { type: 'ClientCreated', label: 'Client Created', icon: User, category: 'Client' },
  { type: 'ClientUpdated', label: 'Client Updated', icon: User, category: 'Client' },
  { type: 'ClientTagAdded', label: 'Tag Added', icon: Tag, category: 'Client' },
  { type: 'PaymentReceived', label: 'Payment Received', icon: CreditCard, category: 'Payment' },
  { type: 'PaymentFailed', label: 'Payment Failed', icon: CreditCard, category: 'Payment' },
  { type: 'RefundIssued', label: 'Refund Issued', icon: RotateCcw, category: 'Payment' },
  { type: 'StaffCreated', label: 'Staff Created', icon: Users, category: 'Staff' },
  { type: 'StaffScheduleChanged', label: 'Schedule Changed', icon: Calendar, category: 'Staff' },
  { type: 'ReviewSubmitted', label: 'Review Submitted', icon: Star, category: 'Marketing' },
  { type: 'FormSubmitted', label: 'Form Submitted', icon: PenTool, category: 'Marketing' },
  { type: 'ManualTrigger', label: 'Manual Trigger', icon: Play, category: 'Manual' },
];

const actions = [
  { type: 'SendEmail', label: 'Send Email', icon: Mail, category: 'Communication' },
  { type: 'SendSms', label: 'Send SMS', icon: MessageSquare, category: 'Communication' },
  { type: 'SendPushNotification', label: 'Push Notification', icon: Bell, category: 'Communication' },
  { type: 'CreateTask', label: 'Create Task', icon: CheckSquare, category: 'Operations' },
  { type: 'UpdateBookingStatus', label: 'Update Booking Status', icon: RefreshCcw, category: 'Operations' },
  { type: 'UpdateClientTags', label: 'Update Tags', icon: Tag, category: 'Contact' },
  { type: 'AddClientNote', label: 'Add Note', icon: FileText, category: 'Contact' },
  { type: 'Delay', label: 'Delay (Wait)', icon: Clock, category: 'Logic' },
  { type: 'WaitUntil', label: 'Wait Until Date', icon: Calendar, category: 'Logic' },
  { type: 'ConditionBranch', label: 'If / Else', icon: Split, category: 'Logic' },
  { type: 'CallWebhook', label: 'Call Webhook', icon: Globe, category: 'Integration' },
  { type: 'RequestReview', label: 'Request Review', icon: Star, category: 'Marketing' },
  { type: 'IssueRefund', label: 'Issue Refund', icon: RotateCcw, category: 'Payment' },
  { type: 'AssignStaff', label: 'Assign Staff', icon: UserPlus, category: 'Staff' },
  { type: 'RemoveStaff', label: 'Remove Staff', icon: UserMinus, category: 'Staff' },
  { type: 'ChargeCreditCard', label: 'Charge Card', icon: DollarSign, category: 'Payment' },
  { type: 'SubWorkflow', label: 'Run Sub-Workflow', icon: GitBranch, category: 'Logic' },
  { type: 'EndWorkflow', label: 'End Execution', icon: Square, category: 'Logic' },
];

export const WorkflowSidebar = () => {
  const [searchTerm, setSearchTerm] = React.useState('');

  const onDragStart = (event: React.DragEvent, nodeType: string, data: any) => {
    event.dataTransfer.setData('application/reactflow', nodeType);
    event.dataTransfer.setData('application/reactflow-data', JSON.stringify(data));
    event.dataTransfer.effectAllowed = 'move';
  };

  const filteredTriggers = triggers.filter(t => t.label.toLowerCase().includes(searchTerm.toLowerCase()));
  const filteredActions = actions.filter(a => a.label.toLowerCase().includes(searchTerm.toLowerCase()));

  return (
    <aside className="w-72 bg-white border-r border-gray-200 flex flex-col h-full shadow-sm">
      <div className="p-4 border-b border-gray-100">
        <h3 className="text-sm font-semibold uppercase tracking-wider text-gray-500 mb-4">Node Library</h3>
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input 
            type="text" 
            placeholder="Search triggers/actions..." 
            className="w-full pl-9 pr-4 py-2 bg-gray-50 border-none rounded-lg text-sm focus:ring-2 focus:ring-primary/20 outline-none transition-all"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-8 scrollbar-thin scrollbar-thumb-gray-200">
        {/* Triggers Section */}
        <section>
          <div className="flex items-center justify-between mb-3">
            <h4 className="text-xs font-bold text-gray-400 uppercase tracking-widest">Triggers</h4>
            <span className="bg-amber-100 text-amber-700 text-[10px] font-bold px-2 py-0.5 rounded-full">YELLOW</span>
          </div>
          <div className="grid grid-cols-1 gap-2">
            {filteredTriggers.map((trigger) => (
              <div
                key={trigger.type}
                className="group flex items-center p-3 bg-white border border-gray-100 rounded-xl hover:border-amber-200 hover:shadow-md hover:shadow-amber-500/5 cursor-grab active:cursor-grabbing transition-all duration-200"
                onDragStart={(event) => onDragStart(event, 'trigger', trigger)}
                draggable
              >
                <div className="w-8 h-8 flex items-center justify-center bg-amber-50 text-amber-600 rounded-lg group-hover:bg-amber-100 transition-colors">
                  <trigger.icon className="w-4 h-4" />
                </div>
                <div className="ml-3">
                  <p className="text-sm font-medium text-gray-700">{trigger.label}</p>
                  <p className="text-[10px] text-gray-400">{trigger.category}</p>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Actions Section */}
        <section>
          <div className="flex items-center justify-between mb-3">
            <h4 className="text-xs font-bold text-gray-400 uppercase tracking-widest">Actions</h4>
            <span className="bg-blue-100 text-blue-700 text-[10px] font-bold px-2 py-0.5 rounded-full">BLUE</span>
          </div>
          <div className="grid grid-cols-1 gap-2">
            {filteredActions.map((action) => (
              <div
                key={action.type}
                className="group flex items-center p-3 bg-white border border-gray-100 rounded-xl hover:border-blue-200 hover:shadow-md hover:shadow-blue-500/5 cursor-grab active:cursor-grabbing transition-all duration-200"
                onDragStart={(event) => onDragStart(event, 'action', action)}
                draggable
              >
                <div className="w-8 h-8 flex items-center justify-center bg-blue-50 text-blue-600 rounded-lg group-hover:bg-blue-100 transition-colors">
                  <action.icon className="w-4 h-4" />
                </div>
                <div className="ml-3">
                  <p className="text-sm font-medium text-gray-700">{action.label}</p>
                  <p className="text-[10px] text-gray-400">{action.category}</p>
                </div>
              </div>
            ))}
          </div>
        </section>
      </div>

      <div className="p-4 bg-gray-50 border-t border-gray-100">
        <div className="flex items-center text-xs text-gray-400">
          <Play className="w-3 h-3 mr-2" />
          <span>Drag nodes to valid locations</span>
        </div>
      </div>
    </aside>
  );
};
