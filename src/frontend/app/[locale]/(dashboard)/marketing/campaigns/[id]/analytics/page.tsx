"use client";

import { useParams } from 'next/navigation';
import CampaignAnalytics from '@/components/campaigns/CampaignAnalytics';
import { Button } from '@/components/ui/Button';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { format } from 'date-fns';

export default function CampaignAnalyticsPage() {
    const params = useParams();
    const id = params?.id as string;

    if (!id) return null;

    return (
        <div className="flex-1 space-y-4 p-8 pt-6">
            <div className="flex items-center justify-between space-y-2">
                <div className="flex items-center gap-4">
                    <Link href="/marketing/campaigns">
                        <Button variant="outline" size="sm">
                            <ArrowLeft className="h-4 w-4" />
                        </Button>
                    </Link>
                    <div>
                        <h2 className="text-3xl font-bold tracking-tight">Campaign Analytics</h2>
                        <p className="text-muted-foreground">
                            Real-time performance metrics for your campaign
                        </p>
                    </div>
                </div>
                <div className="flex items-center space-x-2">
                    <Button>Export Report</Button>
                </div>
            </div>

            <div className="h-full py-6">
                <CampaignAnalytics campaignId={id} />
            </div>
        </div>
    );
}
