"use client";

import React, { useState, useEffect } from "react";
import { 
  Award, Star, Gift, TrendingUp, 
  Settings2, Plus, Users, Loader2
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import api, { apiClient } from "@/lib/api";

export default function LoyaltyProgramPage() {
  const { success, error: toastError } = useToast();
  const [loading, setLoading] = useState(true);
  
  const [analytics, setAnalytics] = useState<any>(null);
  const [tiers, setTiers] = useState<any[]>([]);
  const [rewards, setRewards] = useState<any[]>([]);

  useEffect(() => {
    fetchLoyaltyData();
  }, []);

  const fetchLoyaltyData = async () => {
    setLoading(true);
    try {
      // Fetch Analytics
      const insightsRes = await apiClient.get('/api/v1/loyalty/analytics');
      setAnalytics(insightsRes.data);

      // Fetch Settings (Tiers)
      const settingsRes = await apiClient.get('/api/v1/loyalty/settings');
      if (settingsRes.data && settingsRes.data.tiers) {
         setTiers(JSON.parse(settingsRes.data.tiers));
      } else {
         // Fallback if none defined yet
         setTiers([
           { name: "Bronze", minPoints: 0, benefits: ["Basic Earn Rate"] },
           { name: "Silver", minPoints: 5000, benefits: ["1.5x Points", "Priority Booking"] },
           { name: "Gold", minPoints: 15000, benefits: ["2x Points", "Free Add-on Monthly"] }
         ]);
      }

      // Fetch Rewards
      const rewardsRes = await apiClient.get('/api/v1/loyalty/rewards');
      if (rewardsRes.data && rewardsRes.data.data) {
        setRewards(rewardsRes.data.data);
      }
    } catch (err: any) {
      console.error("Failed to load loyalty data", err);
      toastError("Failed to fetch loyalty program data from server.");
    } finally {
      setLoading(false);
    }
  };

  const getTierColor = (name: string) => {
      switch(name.toLowerCase()) {
         case "bronze": return "text-amber-700 bg-amber-50 border-amber-200";
         case "silver": return "text-gray-600 bg-gray-50 border-gray-200";
         case "gold": return "text-yellow-600 bg-yellow-50 border-yellow-200";
         default: return "text-blue-700 bg-blue-50 border-blue-200";
      }
  };

  if (loading) {
      return (
          <div className="flex flex-col items-center justify-center py-20">
              <Loader2 className="h-10 w-10 text-primary animate-spin mb-4" />
              <p className="text-gray-500">Loading Loyalty Data...</p>
          </div>
      );
  }

  return (
    <div className="space-y-8 max-w-6xl">
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Loyalty & Rewards</h1>
          <p className="text-muted-foreground">Incentivize retention with points, tiers, and referral bonuses.</p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline">
            <Settings2 className="h-4 w-4 mr-2" /> Global Rules
          </Button>
          <Button className="bg-primary hover:bg-primary/90">
            <Plus className="h-4 w-4 mr-2" /> Create Reward
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
         <div className="p-4 rounded-xl border bg-gray-50 flex items-center justify-between">
           <div>
             <div className="text-sm font-bold text-gray-500 mb-1">Points Issued</div>
             <div className="text-2xl font-bold">{analytics?.totalPointsIssued?.toLocaleString() || 0}</div>
           </div>
           <Star className="h-8 w-8 text-yellow-400 opacity-20" />
         </div>
         <div className="p-4 rounded-xl border bg-gray-50 flex items-center justify-between">
           <div>
             <div className="text-sm font-bold text-gray-500 mb-1">Points Redeemed</div>
             <div className="text-2xl font-bold">{analytics?.totalPointsRedeemed?.toLocaleString() || 0}</div>
           </div>
           <Gift className="h-8 w-8 text-primary opacity-20" />
         </div>
         <div className="p-4 rounded-xl border bg-gray-50 flex items-center justify-between">
           <div>
             <div className="text-sm font-bold text-gray-500 mb-1">Active Members</div>
             <div className="text-2xl font-bold">{analytics?.totalMembers?.toLocaleString() || 0}</div>
           </div>
           <Users className="h-8 w-8 text-blue-400 opacity-20" />
         </div>
      </div>

      <div className="grid lg:grid-cols-2 gap-8">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <TrendingUp className="h-5 w-5 text-green-500" />
              Tier Levels
            </CardTitle>
            <CardDescription>Reward your most valuable clients with accelerated point earning.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
             {tiers.map((tier: any) => (
               <div key={tier.name || tier.Name} className={`p-4 rounded-xl border flex justify-between items-center ${getTierColor(tier.name || tier.Name)}`}>
                 <div>
                   <div className="font-bold flex items-center gap-2">
                     <Award className="h-4 w-4" /> {tier.name || tier.Name} Tier
                   </div>
                   <div className="text-xs mt-1 opacity-80">
                     {(tier.minPoints || tier.MinPoints) === 0 ? 'Default starting tier' : `Requires ${(tier.minPoints || tier.MinPoints).toLocaleString()} lifetime points`}
                   </div>
                 </div>
                 <div className="text-sm font-bold bg-white/50 px-2 py-1 rounded shadow-sm">
                   {(tier.benefits || tier.Benefits)?.length || 0} Perks
                 </div>
               </div>
             ))}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Gift className="h-5 w-5 text-primary-500" />
              Redemption Catalog
            </CardTitle>
            <CardDescription>What clients can spend their points on.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
             <div className="space-y-3">
               {rewards.length === 0 ? (
                 <div className="text-center p-4 bg-gray-50 border border-dashed rounded-lg text-sm text-gray-500">
                   No rewards configured yet.
                 </div>
               ) : (
                 rewards.map((reward: any) => (
                   <div key={reward.id} className="flex justify-between items-center p-3 border rounded-lg hover:border-primary transition-colors cursor-pointer bg-white">
                     <span className="font-medium text-sm">{reward.name}</span>
                     <span className="text-xs font-bold text-primary bg-primary/10 px-2 py-1 rounded">
                       {reward.pointsCost.toLocaleString()} pts
                     </span>
                   </div>
                 ))
               )}
               <Button variant="ghost" className="w-full text-xs" size="sm">Manage Catalog</Button>
             </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
