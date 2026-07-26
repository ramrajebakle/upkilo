'use client';

import {
    RadarChart, Radar, PolarGrid, PolarAngleAxis,
    ResponsiveContainer, Tooltip
} from 'recharts';
import {
    BarChart, Bar, XAxis, YAxis, CartesianGrid, Cell
} from 'recharts';
import { formatCurrency } from '@/lib/utils';

interface StaffMember {
    name: string;
    bookings: number;
    revenue: number;
    rating?: number;
    utilization?: number;
}

interface StaffPerformanceChartProps {
    data: StaffMember[];
    height?: number;
    variant?: 'bar' | 'radar';
}

const GRADIENTS = [
    ['#f59e0b', '#f97316'],
    ['#8b5cf6', '#6366f1'],
    ['#10b981', '#06b6d4'],
    ['#ec4899', '#f43f5e'],
    ['#3b82f6', '#06b6d4'],
];

function CustomTooltip({ active, payload }: any) {
    if (!active || !payload?.length) return null;
    const staff = payload[0]?.payload;
    return (
        <div className="bg-slate-900 text-white rounded-xl px-4 py-3 shadow-2xl text-xs space-y-1">
            <p className="font-semibold text-sm">{staff?.name}</p>
            <p>Bookings: <strong>{staff?.bookings}</strong></p>
            <p>Revenue: <strong>{formatCurrency(staff?.revenue)}</strong></p>
            {staff?.rating && <p>Rating: <strong>⭐ {staff.rating.toFixed(1)}</strong></p>}
        </div>
    );
}

export function StaffPerformanceChart({ data, height = 240, variant = 'bar' }: StaffPerformanceChartProps) {
    if (!data || data.length === 0) {
        return (
            <div className="flex items-center justify-center h-[240px] text-slate-400 text-sm">
                No data available
            </div>
        );
    }

    const max = Math.max(...data.map(d => d.revenue), 1);

    return (
        <div className="space-y-3">
            {data.slice(0, 5).map((staff, i) => {
                const [from, to] = GRADIENTS[i % GRADIENTS.length];
                const pct = (staff.revenue / max) * 100;
                return (
                    <div key={staff.name} className="flex items-center gap-3">
                        <div
                            className="w-8 h-8 rounded-lg flex items-center justify-center text-white font-bold text-xs flex-shrink-0"
                            style={{ background: `linear-gradient(135deg, ${from}, ${to})` }}
                        >
                            {i + 1}
                        </div>
                        <div className="flex-1 min-w-0">
                            <div className="flex justify-between text-xs mb-1">
                                <span className="font-medium text-slate-800 truncate">{staff.name}</span>
                                <span className="text-slate-500 ml-2 flex-shrink-0">
                                    {staff.bookings} · {formatCurrency(staff.revenue)}
                                </span>
                            </div>
                            <div className="h-1.5 bg-slate-100 rounded-full overflow-hidden">
                                <div
                                    className="h-full rounded-full transition-all duration-700"
                                    style={{
                                        width: `${pct}%`,
                                        background: `linear-gradient(90deg, ${from}, ${to})`
                                    }}
                                />
                            </div>
                        </div>
                        {staff.rating && (
                            <span className="text-xs font-semibold text-amber-500 flex-shrink-0">
                                ⭐{staff.rating.toFixed(1)}
                            </span>
                        )}
                    </div>
                );
            })}
        </div>
    );
}
