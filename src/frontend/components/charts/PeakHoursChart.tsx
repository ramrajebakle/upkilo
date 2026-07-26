'use client';

import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell, CartesianGrid } from 'recharts';
import { cn } from '@/lib/utils';

interface HourData {
    hour: string;
    bookings: number;
}

interface PeakHoursChartProps {
    data: HourData[];
    height?: number;
}

function CustomTooltip({ active, payload, label }: any) {
    if (!active || !payload?.length) return null;
    return (
        <div className="bg-slate-900 text-white rounded-xl px-3 py-2 shadow-2xl text-xs">
            <p className="text-slate-300">{label}</p>
            <p className="font-bold">{payload[0].value} bookings</p>
        </div>
    );
}

function intensityColor(val: number, max: number): string {
    const ratio = val / max;
    if (ratio >= 0.85) return '#10b981'; // peak
    if (ratio >= 0.6) return '#06b6d4';  // high
    if (ratio >= 0.35) return '#818cf8'; // medium
    return '#e2e8f0';                     // low
}

export function PeakHoursChart({ data, height = 180 }: PeakHoursChartProps) {
    if (!data || data.length === 0) {
        return (
            <div className="flex items-center justify-center h-[180px] text-slate-400 text-sm">
                No data available
            </div>
        );
    }

    const max = Math.max(...data.map(d => d.bookings), 1);
    const peak = data.find(d => d.bookings === max);

    return (
        <div>
            <ResponsiveContainer width="100%" height={height}>
                <BarChart data={data} margin={{ top: 4, right: 0, left: -30, bottom: 0 }} barGap={2}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" vertical={false} />
                    <XAxis
                        dataKey="hour"
                        tick={{ fontSize: 10, fill: '#94a3b8' }}
                        axisLine={false}
                        tickLine={false}
                        interval={2}
                    />
                    <YAxis hide />
                    <Tooltip content={<CustomTooltip />} cursor={{ fill: 'rgba(148,163,184,0.06)' }} />
                    <Bar dataKey="bookings" radius={[3, 3, 0, 0]} maxBarSize={28}>
                        {data.map((entry, i) => (
                            <Cell key={i} fill={intensityColor(entry.bookings, max)} />
                        ))}
                    </Bar>
                </BarChart>
            </ResponsiveContainer>
            {peak && (
                <div className="flex items-center justify-between text-xs mt-3 pt-3 border-t border-slate-100">
                    <span className="text-slate-500">Peak hour</span>
                    <span className="font-semibold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded-full">
                        {peak.hour} · {peak.bookings} bookings
                    </span>
                </div>
            )}
            <div className="flex items-center gap-3 mt-2 text-[10px] text-slate-400">
                {[
                    { color: '#e2e8f0', label: 'Low' },
                    { color: '#818cf8', label: 'Medium' },
                    { color: '#06b6d4', label: 'High' },
                    { color: '#10b981', label: 'Peak' },
                ].map(item => (
                    <div key={item.label} className="flex items-center gap-1">
                        <span className="w-2 h-2 rounded-sm inline-block" style={{ background: item.color }} />
                        {item.label}
                    </div>
                ))}
            </div>
        </div>
    );
}
