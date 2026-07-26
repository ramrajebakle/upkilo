'use client';

import React, { useEffect, useState } from 'react';
import {
    AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
    PieChart, Pie, Cell, Legend
} from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { ArrowUpRight, Users, MousePointer, Mail } from 'lucide-react';
import { apiClient } from '@/lib/api';

interface AnalyticsData {
    campaignId: string;
    timeline: Array<{ hour: number; sent: number; opened: number; clicked: number }>;
    deviceBreakdown: { desktop: number; mobile: number; tablet: number };
    topLinks: Array<{ url: string; clicks: number }>;
    locationBreakdown: Array<{ city: string; opens: number }>;
}

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042'];

export default function CampaignAnalytics({ campaignId }: { campaignId: string }) {
    const [data, setData] = useState<AnalyticsData | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchData = async () => {
            try {
                // Mock data is returned by Controller regardless of ID
                const res = await apiClient.get<AnalyticsData>(`/api/v1/campaigns/${campaignId}/analytics`);
                setData(res.data);
            } catch (err) {
                console.error("Failed to fetch analytics", err);
            } finally {
                setLoading(false);
            }
        };
        fetchData();
    }, [campaignId]);

    if (loading) return <div className="p-8 text-center">Loading analytics...</div>;
    if (!data) return <div className="p-8 text-center">No data available</div>;

    const deviceData = [
        { name: 'Desktop', value: data.deviceBreakdown.desktop },
        { name: 'Mobile', value: data.deviceBreakdown.mobile },
        { name: 'Tablet', value: data.deviceBreakdown.tablet },
    ];

    const totalOpens = data.timeline.reduce((acc, curr) => acc + curr.opened, 0);
    const totalClicks = data.timeline.reduce((acc, curr) => acc + curr.clicked, 0);

    return (
        <div className="space-y-6">
            {/* KPI Cards */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Total Opens</CardTitle>
                        <Mail className="h-4 w-4 text-muted-foreground" />
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{totalOpens}</div>
                        <p className="text-xs text-muted-foreground">+12% from last hour</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Total Clicks</CardTitle>
                        <MousePointer className="h-4 w-4 text-muted-foreground" />
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{totalClicks}</div>
                        <p className="text-xs text-muted-foreground">4.5% CTR</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Active Readers</CardTitle>
                        <Users className="h-4 w-4 text-muted-foreground" />
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{Math.round(totalOpens * 0.8)}</div>
                        <p className="text-xs text-muted-foreground">Estimate unique opens</p>
                    </CardContent>
                </Card>
            </div>

            {/* Engagement Timeline */}
            <Card>
                <CardHeader>
                    <CardTitle>Engagement Over Time (24h)</CardTitle>
                </CardHeader>
                <CardContent className="h-[300px]">
                    <ResponsiveContainer width="100%" height="100%">
                        <AreaChart data={data.timeline}>
                            <defs>
                                <linearGradient id="colorOpened" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="5%" stopColor="#8884d8" stopOpacity={0.8} />
                                    <stop offset="95%" stopColor="#8884d8" stopOpacity={0} />
                                </linearGradient>
                                <linearGradient id="colorClicked" x1="0" y1="0" x2="0" y2="1">
                                    <stop offset="5%" stopColor="#82ca9d" stopOpacity={0.8} />
                                    <stop offset="95%" stopColor="#82ca9d" stopOpacity={0} />
                                </linearGradient>
                            </defs>
                            <XAxis dataKey="hour" />
                            <YAxis />
                            <CartesianGrid strokeDasharray="3 3" />
                            <Tooltip />
                            <Area type="monotone" dataKey="opened" stroke="#8884d8" fillOpacity={1} fill="url(#colorOpened)" />
                            <Area type="monotone" dataKey="clicked" stroke="#82ca9d" fillOpacity={1} fill="url(#colorClicked)" />
                        </AreaChart>
                    </ResponsiveContainer>
                </CardContent>
            </Card>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Device Breakdown */}
                <Card>
                    <CardHeader>
                        <CardTitle>Device Breakdown</CardTitle>
                    </CardHeader>
                    <CardContent className="h-[300px] flex items-center justify-center">
                        <ResponsiveContainer width="100%" height="100%">
                            <PieChart>
                                <Pie
                                    data={deviceData}
                                    cx="50%"
                                    cy="50%"
                                    innerRadius={60}
                                    outerRadius={80}
                                    fill="#8884d8"
                                    paddingAngle={5}
                                    dataKey="value"
                                >
                                    {deviceData.map((entry, index) => (
                                        <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                                    ))}
                                </Pie>
                                <Tooltip />
                                <Legend />
                            </PieChart>
                        </ResponsiveContainer>
                    </CardContent>
                </Card>

                {/* Top Links */}
                <Card>
                    <CardHeader>
                        <CardTitle>Top Clicked Links</CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-4">
                            {data.topLinks.map((link, i) => (
                                <div key={i} className="flex items-center justify-between border-b pb-2 last:border-0">
                                    <div className="flex items-center gap-2 overflow-hidden">
                                        <ArrowUpRight className="h-4 w-4 text-blue-500" />
                                        <a href={link.url} target="_blank" rel="noreferrer" className="text-sm truncate hover:underline text-blue-600 max-w-[200px]">
                                            {link.url}
                                        </a>
                                    </div>
                                    <span className="font-bold text-sm">{link.clicks} clicks</span>
                                </div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
