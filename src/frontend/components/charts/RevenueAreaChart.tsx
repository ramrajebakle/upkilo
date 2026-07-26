'use client';

import {
    AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip,
    ResponsiveContainer, Legend
} from 'recharts';
import { formatCurrency } from '@/lib/utils';

interface DataPoint {
    label: string;
    revenue: number;
    expenses?: number;
}

interface RevenueAreaChartProps {
    data: DataPoint[];
    height?: number;
    showExpenses?: boolean;
}

function CustomTooltip({ active, payload, label }: any) {
    if (!active || !payload?.length) return null;
    return (
        <div className="bg-slate-900 text-white rounded-xl px-4 py-3 shadow-2xl text-sm">
            <p className="text-slate-400 mb-2 font-medium">{label}</p>
            {payload.map((entry: any) => (
                <div key={entry.dataKey} className="flex items-center gap-2">
                    <span
                        className="w-2.5 h-2.5 rounded-full inline-block"
                        style={{ background: entry.color }}
                    />
                    <span className="capitalize">{entry.name}:</span>
                    <span className="font-bold">{formatCurrency(entry.value)}</span>
                </div>
            ))}
        </div>
    );
}

export function RevenueAreaChart({ data, height = 280, showExpenses = false }: RevenueAreaChartProps) {
    if (!data || data.length === 0) {
        return (
            <div className="flex items-center justify-center h-[280px] text-slate-400 text-sm">
                No data available
            </div>
        );
    }
    return (
        <ResponsiveContainer width="100%" height={height}>
            <AreaChart data={data} margin={{ top: 10, right: 10, left: -10, bottom: 0 }}>
                <defs>
                    <linearGradient id="revenueGrad" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#06b6d4" stopOpacity={0.25} />
                        <stop offset="95%" stopColor="#06b6d4" stopOpacity={0} />
                    </linearGradient>
                    <linearGradient id="expensesGrad" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="5%" stopColor="#f43f5e" stopOpacity={0.2} />
                        <stop offset="95%" stopColor="#f43f5e" stopOpacity={0} />
                    </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
                <XAxis
                    dataKey="label"
                    tick={{ fontSize: 12, fill: '#94a3b8' }}
                    axisLine={false}
                    tickLine={false}
                />
                <YAxis
                    tickFormatter={(v) => `$${v >= 1000 ? `${(v / 1000).toFixed(0)}k` : v}`}
                    tick={{ fontSize: 12, fill: '#94a3b8' }}
                    axisLine={false}
                    tickLine={false}
                />
                <Tooltip content={<CustomTooltip />} />
                {showExpenses && (
                    <Area
                        type="monotone"
                        dataKey="expenses"
                        name="Expenses"
                        stroke="#f43f5e"
                        strokeWidth={2}
                        fill="url(#expensesGrad)"
                        dot={false}
                        activeDot={{ r: 5, fill: '#f43f5e' }}
                    />
                )}
                <Area
                    type="monotone"
                    dataKey="revenue"
                    name="Revenue"
                    stroke="#06b6d4"
                    strokeWidth={2.5}
                    fill="url(#revenueGrad)"
                    dot={false}
                    activeDot={{ r: 6, fill: '#06b6d4', stroke: '#fff', strokeWidth: 2 }}
                />
            </AreaChart>
        </ResponsiveContainer>
    );
}
