'use client';

import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, Legend } from 'recharts';

interface ServiceDataPoint {
    name: string;
    value: number;
    color?: string;
}

interface ServiceDonutChartProps {
    data: ServiceDataPoint[];
    height?: number;
}

const PALETTE = [
    '#8b5cf6', '#06b6d4', '#10b981', '#f59e0b',
    '#ec4899', '#6366f1', '#f43f5e', '#14b8a6'
];

function CustomTooltip({ active, payload }: any) {
    if (!active || !payload?.length) return null;
    const { name, value, payload: { percent } } = payload[0];
    return (
        <div className="bg-slate-900 text-white rounded-xl px-4 py-3 shadow-2xl text-sm">
            <p className="font-semibold">{name}</p>
            <p className="text-slate-300">{value} bookings ({(percent * 100).toFixed(1)}%)</p>
        </div>
    );
}

function CustomLegend({ data }: { data: ServiceDataPoint[] }) {
    return (
        <div className="flex flex-col gap-1.5 mt-2">
            {data.map((item, i) => (
                <div key={item.name} className="flex items-center gap-2 text-xs">
                    <span
                        className="w-2.5 h-2.5 rounded-full flex-shrink-0"
                        style={{ background: item.color || PALETTE[i % PALETTE.length] }}
                    />
                    <span className="text-slate-600 truncate">{item.name}</span>
                    <span className="ml-auto font-semibold text-slate-700">{item.value}</span>
                </div>
            ))}
        </div>
    );
}

export function ServiceDonutChart({ data, height = 200 }: ServiceDonutChartProps) {
    if (!data || data.length === 0) {
        return (
            <div className="flex items-center justify-center h-[200px] text-slate-400 text-sm">
                No data available
            </div>
        );
    }

    return (
        <div>
            <ResponsiveContainer width="100%" height={height}>
                <PieChart>
                    <Pie
                        data={data}
                        cx="50%"
                        cy="50%"
                        innerRadius="58%"
                        outerRadius="80%"
                        paddingAngle={3}
                        dataKey="value"
                        startAngle={90}
                        endAngle={-270}
                    >
                        {data.map((entry, i) => (
                            <Cell
                                key={`cell-${i}`}
                                fill={entry.color || PALETTE[i % PALETTE.length]}
                                stroke="none"
                            />
                        ))}
                    </Pie>
                    <Tooltip content={<CustomTooltip />} />
                </PieChart>
            </ResponsiveContainer>
            <CustomLegend data={data} />
        </div>
    );
}
