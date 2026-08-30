'use client';

import {
    LineChart, Line, XAxis, YAxis, CartesianGrid,
    Tooltip, ResponsiveContainer, ReferenceLine
} from 'recharts';

interface RetentionDataPoint {
    label: string;
    rate: number;
    newClients?: number;
    returning?: number;
}

interface RetentionLineChartProps {
    data: RetentionDataPoint[];
    height?: number;
    targetRate?: number;
}

function CustomTooltip({ active, payload, label }: any) {
    if (!active || !payload?.length) return null;
    return (
        <div className="bg-popover text-popover-foreground border border-border shadow-[var(--shadow-popover)] rounded-xl px-4 py-3 shadow-2xl text-sm">
            <p className="text-foreground-secondary mb-2 font-medium">{label}</p>
            {payload.map((entry: any) => (
                <div key={entry.dataKey} className="flex items-center gap-2">
                    <span className="w-2.5 h-2.5 rounded-full" style={{ background: entry.color }} />
                    <span>{entry.name}:</span>
                    <span className="font-bold">
                        {entry.dataKey === 'rate' ? `${entry.value}%` : entry.value}
                    </span>
                </div>
            ))}
        </div>
    );
}

export function RetentionLineChart({ data, height = 220, targetRate = 75 }: RetentionLineChartProps) {
    if (!data || data.length === 0) {
        return (
            <div className="flex items-center justify-center h-[220px] text-foreground-muted text-sm">
                No data available
            </div>
        );
    }

    return (
        <ResponsiveContainer width="100%" height={height}>
            <LineChart data={data} margin={{ top: 10, right: 10, left: -15, bottom: 0 }}>
                <defs>
                    <linearGradient id="retentionLine" x1="0" y1="0" x2="1" y2="0">
                        <stop offset="0%" stopColor="#8b5cf6" />
                        <stop offset="100%" stopColor="#06b6d4" />
                    </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis
                    dataKey="label"
                    tick={{ fontSize: 11 }}
                    axisLine={false}
                    tickLine={false}
                />
                <YAxis
                    domain={[0, 100]}
                    tickFormatter={(v) => `${v}%`}
                    tick={{ fontSize: 11 }}
                    axisLine={false}
                    tickLine={false}
                />
                <Tooltip content={<CustomTooltip />} />
                <ReferenceLine
                    y={targetRate}
                    stroke="#10b981"
                    strokeDasharray="4 4"
                    label={{ value: `Target ${targetRate}%`, position: 'right', fontSize: 10, fill: '#10b981' }}
                />
                <Line
                    type="monotone"
                    dataKey="rate"
                    name="Retention Rate"
                    stroke="url(#retentionLine)"
                    strokeWidth={2.5}
                    dot={{ fill: '#8b5cf6', r: 4, strokeWidth: 2 }}
                    activeDot={{ r: 6, fill: '#8b5cf6', strokeWidth: 2 }}
                />
                {data[0]?.newClients !== undefined && (
                    <Line
                        type="monotone"
                        dataKey="newClients"
                        name="New Clients"
                        stroke="#06b6d4"
                        strokeWidth={2}
                        strokeDasharray="5 3"
                        dot={false}
                        activeDot={{ r: 4 }}
                    />
                )}
            </LineChart>
        </ResponsiveContainer>
    );
}
