'use client';

import React, { useState } from 'react';
import { 
  BarChart3, 
  FileText, 
  Download, 
  Play, 
  Plus, 
  Settings2, 
  Filter, 
  Calendar as CalendarIcon,
  ChevronDown
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/Card';

interface Metric {
  id: string;
  name: string;
  description: string;
}

interface Dimension {
  id: string;
  name: string;
  description: string;
}

const METRICS: Metric[] = [
  { id: 'revenue', name: 'Total Revenue', description: 'Sum of all completed invoices' },
  { id: 'bookings', name: 'Booking Count', description: 'Total number of bookings' },
  { id: 'clients', name: 'New Clients', description: 'Number of unique clients won' },
  { id: 'noshows', name: 'No-Show Rate', description: 'Percentage of missed appointments' },
];

const DIMENSIONS: Dimension[] = [
  { id: 'service', name: 'Service Name', description: 'Breakdown by service offered' },
  { id: 'staff', name: 'Staff Member', description: 'Analyze performance by staff' },
  { id: 'source', name: 'Booking Source', description: 'Web, Mobile, Walk-in' },
  { id: 'month', name: 'Month', description: 'Monthly trend analysis' },
];

export function ReportBuilder() {
  const [selectedMetrics, setSelectedMetrics] = useState<string[]>(['revenue']);
  const [selectedDimensions, setSelectedDimensions] = useState<string[]>(['service']);
  const [reportName, setReportName] = useState('New Custom Report');
  const [isExecuting, setIsExecuting] = useState(false);

  const toggleMetric = (id: string) => {
    setSelectedMetrics(prev => 
      prev.includes(id) ? prev.filter(m => m !== id) : [...prev, id]
    );
  };

  const toggleDimension = (id: string) => {
    setSelectedDimensions(prev => 
      prev.includes(id) ? prev.filter(d => d !== id) : [...prev, id]
    );
  };

  return (
    <div className="space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-700">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold tracking-tight text-gray-900 flex items-center gap-3">
            <Settings2 className="h-8 w-8 text-primary-600" />
            Custom Report Builder
          </h2>
          <p className="text-gray-500 mt-1">Design and execute custom data exports for your business.</p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" className="gap-2">
            <Download className="h-4 w-4" />
            Export CSV
          </Button>
          <Button 
            className="gap-2 bg-primary-600 hover:bg-primary-700 shadow-lg shadow-primary-200"
            onClick={() => setIsExecuting(true)}
            disabled={isExecuting}
          >
            {isExecuting ? <Play className="h-4 w-4 animate-pulse" /> : <Play className="h-4 w-4" />}
            {isExecuting ? 'Running...' : 'Execute Report'}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-12 gap-8">
        {/* Configuration Sidebar */}
        <div className="col-span-4 space-y-6">
          <Card className="border-gray-100 shadow-sm">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg flex items-center gap-2">
                <BarChart3 className="h-5 w-5 text-blue-600" />
                Step 1: Choose Metrics
              </CardTitle>
              <CardDescription>What values do you want to calculate?</CardDescription>
            </CardHeader>
            <CardContent className="space-y-2">
              {METRICS.map(metric => (
                <button
                  key={metric.id}
                  onClick={() => toggleMetric(metric.id)}
                  className={cn(
                    "w-full text-left p-3 rounded-xl border transition-all flex items-center justify-between group",
                    selectedMetrics.includes(metric.id) 
                      ? "bg-primary-50 border-primary-200 ring-1 ring-primary-100 shadow-sm" 
                      : "bg-white border-gray-100 hover:border-gray-200 hover:bg-gray-50"
                  )}
                >
                  <div>
                    <div className={cn("font-semibold text-sm", selectedMetrics.includes(metric.id) ? "text-primary-900" : "text-gray-700")}>
                      {metric.name}
                    </div>
                    <div className="text-xs text-gray-400 group-hover:text-gray-500">{metric.description}</div>
                  </div>
                  <div className={cn(
                    "h-5 w-5 rounded-full border flex items-center justify-center transition-colors",
                    selectedMetrics.includes(metric.id) ? "bg-primary-600 border-primary-600 text-white" : "border-gray-200 bg-white"
                  )}>
                    {selectedMetrics.includes(metric.id) && <Plus className="h-3 w-3 stroke-[3]" />}
                  </div>
                </button>
              ))}
            </CardContent>
          </Card>

          <Card className="border-gray-100 shadow-sm">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg flex items-center gap-2">
                <FileText className="h-5 w-5 text-amber-600" />
                Step 2: Choose Dimensions
              </CardTitle>
              <CardDescription>How should the data be grouped?</CardDescription>
            </CardHeader>
            <CardContent className="space-y-2">
              {DIMENSIONS.map(dimension => (
                <button
                  key={dimension.id}
                  onClick={() => toggleDimension(dimension.id)}
                  className={cn(
                    "w-full text-left p-3 rounded-xl border transition-all flex items-center justify-between group",
                    selectedDimensions.includes(dimension.id) 
                      ? "bg-amber-50 border-amber-200 ring-1 ring-amber-100 shadow-sm" 
                      : "bg-white border-gray-100 hover:border-gray-200 hover:bg-gray-50"
                  )}
                >
                  <div>
                    <div className={cn("font-semibold text-sm", selectedDimensions.includes(dimension.id) ? "text-amber-900" : "text-gray-700")}>
                      {dimension.name}
                    </div>
                    <div className="text-xs text-gray-400 group-hover:text-gray-500">{dimension.description}</div>
                  </div>
                  <div className={cn(
                    "h-5 w-5 rounded-full border flex items-center justify-center transition-colors",
                    selectedDimensions.includes(dimension.id) ? "bg-amber-600 border-amber-600 text-white" : "border-gray-200 bg-white"
                  )}>
                    {selectedDimensions.includes(dimension.id) && <Plus className="h-3 w-3 stroke-[3]" />}
                  </div>
                </button>
              ))}
            </CardContent>
          </Card>
        </div>

        {/* Preview Area */}
        <div className="col-span-8 space-y-6">
          <div className="bg-white rounded-2xl border border-gray-100 shadow-xl overflow-hidden min-h-[600px] flex flex-col">
            <div className="px-6 py-4 border-b border-gray-100 bg-gray-50/50 flex items-center justify-between">
              <div className="flex items-center gap-4">
                <div className="px-3 py-1 bg-white border border-gray-100 rounded-lg text-sm font-medium flex items-center gap-2 text-gray-600">
                  <CalendarIcon className="h-4 w-4" />
                  Last 30 Days
                  <ChevronDown className="h-3 w-3" />
                </div>
                <div className="px-3 py-1 bg-white border border-gray-100 rounded-lg text-sm font-medium flex items-center gap-2 text-gray-600">
                  <Filter className="h-4 w-4" />
                  All Services
                  <ChevronDown className="h-3 w-3" />
                </div>
              </div>
              <div className="text-xs text-gray-400 font-mono">DEFINITION_ID-XC28</div>
            </div>

            <div className="flex-1 p-8 flex flex-col items-center justify-center text-center">
              {isExecuting ? (
                <div className="space-y-6">
                  <div className="relative">
                    <div className="h-24 w-24 border-8 border-gray-100 border-t-primary-600 rounded-full animate-spin mx-auto" />
                    <div className="absolute inset-0 flex items-center justify-center text-primary-600 font-bold">75%</div>
                  </div>
                  <div>
                    <h3 className="text-xl font-bold text-gray-900">Processing Big Data...</h3>
                    <p className="text-gray-500 mt-2">Aggregating transactional records across your tenant isolation layers.</p>
                  </div>
                </div>
              ) : (
                <div className="max-w-md">
                  <div className="h-16 w-16 bg-gray-50 rounded-2xl flex items-center justify-center mx-auto mb-6">
                    <Play className="h-8 w-8 text-gray-300" />
                  </div>
                  <h3 className="text-xl font-bold text-gray-900">Ready to Visualize</h3>
                  <p className="text-gray-500 mt-3 leading-relaxed">
                    Select your metrics and dimensions on the left, then click 
                    <span className="font-bold text-gray-700"> Execute Report </span> 
                    to generate your business intelligence preview.
                  </p>
                </div>
              )}
            </div>

            <div className="px-6 py-4 border-t border-gray-100 bg-gray-50/50 flex items-center justify-between text-sm text-gray-500 font-medium">
              <div className="flex gap-4">
                <span>Metrics: {selectedMetrics.length}</span>
                <span>Dimensions: {selectedDimensions.length}</span>
              </div>
              <div>Auto-save active</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
