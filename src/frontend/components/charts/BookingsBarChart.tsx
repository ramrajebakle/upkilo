'use client';

import {
    BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip,
    ResponsiveContainer, Cell
} from 'recharts';

interface DataPoint {
    label: string;
    bookings: number;
    cancelled?: number;
}

interface BookingsBarChartProps {
    data: DataPoint[];
    height?: number;
    showCancelled?: boolean;
}

const HIGHLIGHT_COLOR = '#8b5cf6';
const BASE_COLOR = '#c4b5fd';
const CANCEL_COLOR = '#fca5a5';

function CustomTooltip({ active, payload, label }: any) {
    if (!active || !payload?.length) return null;
    return (
        <div className="bg-popover text-popover-foreground border border-border shadow-[var(--shadow-popover)] rounded-xl px-4 py-3 shadow-2xl text-sm">
            <p className="text-foreground-secondary mb-2 font-medium">{label}</p>
            {payload.map((entry: any) => (
                <div key={entry.dataKey} className="flex items-center gap-2">
                    <span
                        className="w-2.5 h-2.5 rounded-full inline-block"
                        style={{ background: entry.color }}
                    />
                    <span className="capitalize">{entry.name}:</span>
                    <span className="font-bold">{entry.value}</span>
                </div>
            ))}
        </div>
    );
}

export function BookingsBarChart({ data, height = 280, showCancelled = false }: BookingsBarChartProps) {
    if (!data || data.length === 0) {
        return (
            <div className="flex items-center justify-center h-[280px] text-foreground-muted text-sm">
                No data available
            </div>
        );
    }

    const maxVal = Math.max(...data.map(d => d.bookings));

    return (
        <ResponsiveContainer width="100%" height={height}>
            <BarChart data={data} margin={{ top: 10, right: 10, left: -10, bottom: 0 }} barGap={4}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis
                    dataKey="label"
                    tick={{ fontSize: 12 }}
                    axisLine={false}
                    tickLine={false}
                />
                <YAxis
                    tick={{ fontSize: 12 }}
                    axisLine={false}
                    tickLine={false}
                    allowDecimals={false}
                />
                <Tooltip content={<CustomTooltip />} cursor={{ fill: 'rgba(148,163,184,0.08)' }} />
                {showCancelled && (
                    <Bar dataKey="cancelled" name="Cancelled" fill={CANCEL_COLOR} radius={[4, 4, 0, 0]} maxBarSize={40} />
                )}
                <Bar dataKey="bookings" name="Bookings" radius={[4, 4, 0, 0]} maxBarSize={40}>
                    {data.map((entry, index) => (
                        <Cell
                            key={`cell-${index}`}
                            fill={entry.bookings === maxVal ? HIGHLIGHT_COLOR : BASE_COLOR}
                        />
                    ))}
                </Bar>
            </BarChart>
        </ResponsiveContainer>
    );
}
