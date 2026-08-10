'use client';

import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { 
  Zap, 
  TrendingUp, 
  Users, 
  Target, 
  Eye, 
  Play, 
  Pause, 
  AlertTriangle, 
  LineChart, 
  Globe, 
  Search, 
  Share2, 
  FileText, 
  BarChart3,
  CheckCircle2,
  Clock,
  ChevronRight,
  MoreVertical,
  Activity
} from 'lucide-react';
import { 
  AreaChart, 
  Area, 
  XAxis, 
  YAxis, 
  CartesianGrid, 
  Tooltip, 
  ResponsiveContainer,
  LineChart as ReLineChart,
  Line
} from 'recharts';
import { motion, AnimatePresence } from 'framer-motion';
import { cn, formatCurrency } from '@/lib/utils';
import api from '@/lib/api';
import { toast } from 'sonner';

export default function MarketingAutomationPage() {
  const t = useTranslations('MarketingAutomation');
  const [loading, setLoading] = useState(true);
  const [data, setData] = useState<any>(null);
  const [forecasts, setForecasts] = useState<any[]>([]);
  const [forecastType, setForecastType] = useState('Traffic');
  const [actions, setActions] = useState<any[]>([]);
  const [integrations, setIntegrations] = useState<any[]>([]);
  const [isAutonomous, setIsAutonomous] = useState(false);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [dashRes, forecastRes, actionRes, integrationsRes] = await Promise.all([
        api.marketingAutomation.dashboard(),
        api.marketingAutomation.forecasts(),
        api.marketingAutomation.actions(),
        api.marketingAutomation.getIntegrations()
      ]);

      setData(dashRes.data);
      setForecasts(forecastRes.data);
      setActions(actionRes.data);
      setIntegrations(integrationsRes.data);
      setIsAutonomous(dashRes.data.config?.isAutonomousMode || false);
    } catch (error) {
      console.error('Failed to fetch marketing automation data', error);
      toast.error('Failed to load dashboard');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleAutonomous = async () => {
    const newState = !isAutonomous;
    try {
      await api.marketingAutomation.toggle(newState);
      setIsAutonomous(newState);
      toast.success(newState ? 'Autonomous mode enabled' : 'Autonomous mode paused');
    } catch (error) {
      toast.error('Failed to update automation state');
    }
  };

  const handleConnectIntegration = async (platform: string) => {
    try {
      await api.marketingAutomation.connectIntegration(platform);
      toast.success(`${platform} connected successfully`);
      const res = await api.marketingAutomation.getIntegrations();
      setIntegrations(res.data);
    } catch (error) {
      toast.error(`Failed to connect ${platform}`);
    }
  };

    return (
      <div className="flex flex-col items-center justify-center min-h-[60vh]">
        <div className="w-12 h-12 border-4 border-primary border-t-transparent rounded-full animate-spin mb-4" />
        <p className="text-slate-500 dark:text-slate-400 animate-pulse">Initializing marketing logic...</p>
      </div>
    );

  // Handle Onboarding State
  if (!data?.config?.isOnboarded) {
    return <OnboardingView onComplete={fetchData} />;
  }

  return (
    <div className="space-y-8 pb-12 animate-in fade-in duration-700">
      {/* Header & Toggle */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2 bg-gradient-to-br from-primary-500 to-primary-700 rounded-xl shadow-lg shadow-primary-500/25">
              <Zap className="h-6 w-6 text-white" />
            </div>
            <h1 className="text-3xl font-bold text-slate-900 dark:text-white" style={{ fontFamily: 'var(--font-display)' }}>
              {t('title')}
            </h1>
          </div>
          <p className="text-slate-500 dark:text-slate-400">{t('description')}</p>
        </div>

        <div className="flex items-center gap-4 bg-white dark:bg-slate-900 p-2 pr-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
          <div className={cn(
            "p-3 rounded-xl transition-colors",
            isAutonomous ? "bg-emerald-100 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-400" : "bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400"
          )}>
            {isAutonomous ? <Play className="h-5 w-5" /> : <Pause className="h-5 w-5" />}
          </div>
          <div className="flex-1 pr-4">
            <p className="text-sm font-semibold text-slate-900 dark:text-white">{t('autonomousMode')}</p>
            <p className="text-xs text-slate-500 dark:text-slate-400">{isAutonomous ? t('status.running') : t('status.paused')}</p>
          </div>
          <button
            onClick={handleToggleAutonomous}
            className={cn(
              "relative inline-flex h-7 w-12 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2",
              isAutonomous ? "bg-emerald-500" : "bg-slate-300 dark:bg-slate-700"
            )}
          >
            <span
              className={cn(
                "inline-block h-5 w-5 transform rounded-full bg-white transition-transform",
                isAutonomous ? "translate-x-6" : "translate-x-1"
              )}
            />
          </button>
        </div>
      </div>

      {/* Safety Alert (Demo/Placeholder if needed) */}
      {!isAutonomous && (
        <motion.div 
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="p-4 bg-amber-50 dark:bg-amber-900/10 border border-amber-200 dark:border-amber-900/30 rounded-2xl flex items-center gap-4"
        >
          <div className="p-2 bg-amber-100 dark:bg-amber-900/30 rounded-lg text-amber-600 dark:text-amber-400">
            <AlertTriangle className="h-5 w-5" />
          </div>
          <div>
            <p className="font-semibold text-amber-900 dark:text-amber-400">Safety Check Active</p>
            <p className="text-sm text-amber-700 dark:text-amber-300/80">Autonomous mode is currently paused. AI agents will not execute until re-enabled.</p>
          </div>
        </motion.div>
      )}

      {/* Stats Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard 
          label={t('stats.totalViews')} 
          value={data.dashboard.totalViews.toLocaleString()} 
          icon={Eye} 
          color="blue" 
          trend="+12%" 
        />
        <StatCard 
          label={t('stats.totalLeads')} 
          value={data.dashboard.totalLeads.toLocaleString()} 
          icon={Target} 
          color="emerald" 
          trend="+8%" 
        />
        <StatCard 
          label={t('stats.convRate')} 
          value={`${data.dashboard.conversionRate.toFixed(1)}%`} 
          icon={Users} 
          color="violet" 
          trend="+2%" 
        />
        <StatCard 
          label={t('stats.projectedRevenue')} 
          value={formatCurrency(data.dashboard.revenue30d)} 
          icon={TrendingUp} 
          color="amber" 
          trend="+5%" 
        />
      </div>
 
      {/* Integrations Grid */}
      <div className="card-elevated dark:bg-slate-900 dark:border-slate-800 p-6 shadow-sm border border-slate-200">
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-xl font-bold text-slate-900 dark:text-white">External Integrations</h3>
          <span className="text-xs font-medium text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-slate-800 px-3 py-1 rounded-full border border-slate-200 dark:border-slate-700">
            {integrations.filter(i => i.isConnected).length}/4 Connected
          </span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {integrations.map((integration: any) => (
            <div key={integration.platform} className="p-4 bg-slate-50 dark:bg-slate-800/50 rounded-2xl border border-slate-200 dark:border-slate-800 flex items-center justify-between group transition-all hover:bg-white dark:hover:bg-slate-800 hover:border-primary/50 dark:hover:border-primary/50 hover:shadow-md">
              <div className="flex items-center gap-4">
                <div className={cn(
                  "p-2.5 rounded-xl transition-colors",
                  integration.isConnected ? "bg-emerald-100 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-400" : "bg-slate-200 text-slate-400 dark:bg-slate-700 dark:text-slate-500"
                )}>
                  {integration.platform === 'Google' && <Search className="h-5 w-5" />}
                  {integration.platform === 'Bing' && <Globe className="h-5 w-5" />}
                  {integration.platform === 'LinkedIn' && <Share2 className="h-5 w-5" />}
                  {integration.platform === 'Twitter' && <Activity className="h-5 w-5" />}
                </div>
                <div>
                  <h4 className="text-sm font-bold text-slate-900 dark:text-white leading-none mb-1">{integration.platform}</h4>
                  <p className="text-[10px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-tighter">
                    {integration.isConnected ? 'Connected' : 'Not Linked'}
                  </p>
                </div>
              </div>
              <button 
                onClick={() => handleConnectIntegration(integration.platform)}
                disabled={integration.isConnected}
                className={cn(
                  "px-3 py-1.5 text-[10px] font-bold rounded-lg transition-all",
                  integration.isConnected 
                    ? "bg-emerald-50 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-400 cursor-default" 
                    : "bg-white dark:bg-slate-900 text-primary dark:text-primary-400 border border-slate-200 dark:border-slate-700 hover:bg-primary/5 hover:text-primary dark:hover:bg-primary-900/20 active:scale-95 shadow-sm"
                )}
              >
                {integration.isConnected ? 'Connected' : 'Connect'}
              </button>
            </div>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Forecast Section */}
        <div className="lg:col-span-2 space-y-6">
          <div className="card-elevated dark:bg-slate-900 dark:border-slate-800 p-6 shadow-sm border border-slate-200">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between mb-8 gap-4">
              <div>
                <h3 className="text-xl font-bold text-slate-900 dark:text-white">{t('forecast.title')}</h3>
                <p className="text-sm text-slate-500 dark:text-slate-400">Autonomous 30-day projection</p>
              </div>
              <div className="flex items-center gap-1 p-1 bg-slate-50 dark:bg-slate-800 rounded-lg border border-slate-200 dark:border-slate-700">
                {['Traffic', 'Leads', 'Revenue'].map(type => (
                  <button
                    key={type}
                    onClick={() => setForecastType(type)}
                    className={cn(
                      "px-3 py-1.5 text-xs font-bold rounded-md transition-all",
                      forecastType === type 
                        ? "bg-white dark:bg-slate-700 text-primary dark:text-primary-400 shadow-sm border border-slate-200 dark:border-slate-600" 
                        : "text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
                    )}
                  >
                    {type}
                  </button>
                ))}
              </div>
            </div>
            
            <div className="h-[350px] w-full">
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={forecasts.filter(f => f.forecastType === forecastType)}>
                  <defs>
                    <linearGradient id="colorMain" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor={forecastType === 'Revenue' ? '#F59E0B' : '#3B82F6'} stopOpacity={0.3}/>
                      <stop offset="95%" stopColor={forecastType === 'Revenue' ? '#F59E0B' : '#3B82F6'} stopOpacity={0}/>
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#64748B" strokeOpacity={0.1} />
                  <XAxis 
                    dataKey="forecastDate" 
                    tickFormatter={(val) => new Date(val).toLocaleDateString(undefined, { day: 'numeric', month: 'short' })}
                    axisLine={false}
                    tickLine={false}
                    tick={{ fontSize: 12, fill: '#64748B' }}
                  />
                  <YAxis hide />
                   <Tooltip 
                    contentStyle={{ backgroundColor: 'rgb(15 23 42)', borderRadius: '12px', border: 'none', boxShadow: '0 10px 15px -3px rgb(0 0 0 / 0.1)' }}
                    itemStyle={{ color: '#fff' }}
                  />
                  <Area 
                    type="monotone" 
                    dataKey="predictedValue" 
                    name={`Projected ${forecastType}`}
                    stroke={forecastType === 'Revenue' ? '#F59E0B' : '#3B82F6'}
                    strokeWidth={3}
                    fillOpacity={1} 
                    fill="url(#colorMain)" 
                  />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </div>

          {/* Lead Attribution Section */}
          <div className="card-elevated dark:bg-slate-900 dark:border-slate-800 p-6 shadow-sm border border-slate-200">
            <h3 className="text-xl font-bold text-slate-900 dark:text-white mb-6">Autonomous Lead Attribution</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
              {data.dashboard.attribution?.map((attr: any) => (
                <div key={attr.source} className="p-4 bg-slate-50 dark:bg-slate-800/50 rounded-2xl border border-slate-100 dark:border-slate-800 group hover:border-primary-100 dark:hover:border-primary-900 transition-colors">
                  <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest">{attr.source}</p>
                  <div className="flex items-end justify-between mt-1">
                    <p className="text-2xl font-bold text-slate-900 dark:text-white leading-none">{attr.count}</p>
                    <span className="text-[10px] font-bold text-emerald-600 dark:text-emerald-400">+{Math.round((attr.count / data.dashboard.leadsCapturedThisMonth) * 100)}%</span>
                  </div>
                  <div className="mt-3 w-full h-1.5 bg-white dark:bg-slate-800 rounded-full overflow-hidden border border-slate-100 dark:border-slate-700 shadow-inner">
                    <div 
                      className="h-full bg-primary rounded-full group-hover:bg-primary/80 transition-all duration-500" 
                      style={{ width: `${Math.min(100, (attr.count / data.dashboard.leadsCapturedThisMonth) * 100)}%` }} 
                    />
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Agent Status Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <AgentCard name={t('agents.seo')} icon={Search} status="active" lastRun="2h ago" type="SEO" />
            <AgentCard name={t('agents.content')} icon={FileText} status="active" lastRun="4h ago" type="Content" />
            <AgentCard name={t('agents.discovery')} icon={Globe} status="active" lastRun="12h ago" type="Discovery" />
            <AgentCard name={t('agents.social')} icon={Share2} status="active" lastRun="1h ago" type="Distribution" />
            <AgentCard name={t('agents.lead')} icon={Target} status="active" lastRun="6h ago" type="LeadOptimizer" />
            <AgentCard name={t('agents.analytics')} icon={BarChart3} status="active" lastRun="30m ago" type="Analytics" />
          </div>
        </div>

        {/* Activity Feed */}
        <div className="space-y-6">
          <div className="card-elevated dark:bg-slate-900 dark:border-slate-800 p-6 shadow-sm border border-slate-200 flex flex-col h-full min-h-[600px]">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-lg font-bold text-slate-900 dark:text-white">{t('activity.title')}</h3>
              <Activity className="h-5 w-5 text-slate-400 dark:text-slate-500" />
            </div>
            
            <div className="flex-1 space-y-6 overflow-y-auto pr-2 scrollbar-thin">
              {actions.length === 0 ? (
                <div className="text-center py-12">
                  <div className="w-16 h-16 bg-slate-100 dark:bg-slate-800 rounded-full flex items-center justify-center mx-auto mb-4">
                    <Clock className="h-8 w-8 text-slate-400 dark:text-slate-500" />
                  </div>
                  <p className="text-slate-500 dark:text-slate-400 text-sm leading-relaxed">{t('activity.empty')}</p>
                </div>
              ) : (
                actions.map((action, i) => (
                  <ActivityItem 
                    key={action.id} 
                    action={action} 
                    isLast={i === actions.length - 1} 
                  />
                ))
              )}
            </div>

            <button className="w-full mt-6 py-2.5 text-sm font-bold text-primary dark:text-primary-400 hover:bg-primary/5 dark:hover:bg-primary-900/20 rounded-xl transition-all border border-primary/20 dark:border-primary-900/50 shadow-sm active:scale-[0.98]">
              {t('activity.viewAll')}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function StatCard({ label, value, icon: Icon, color, trend }: any) {
  const colors: any = {
    blue: 'from-blue-50 to-blue-100/50 dark:from-blue-900/10 dark:to-blue-900/5 text-blue-600 dark:text-blue-400 icon-bg:bg-blue-600/10',
    emerald: 'from-emerald-50 to-emerald-100/50 dark:from-emerald-900/10 dark:to-emerald-900/5 text-emerald-600 dark:text-emerald-400 icon-bg:bg-emerald-600/10',
    violet: 'from-violet-50 to-violet-100/50 dark:from-violet-900/10 dark:to-violet-900/5 text-violet-600 dark:text-violet-400 icon-bg:bg-violet-600/10',
    amber: 'from-amber-50 to-amber-100/50 dark:from-amber-900/10 dark:to-amber-900/5 text-amber-600 dark:text-amber-400 icon-bg:bg-amber-600/10',
  };

  return (
    <div className="card-elevated dark:bg-slate-900 dark:border-slate-800 p-5 relative overflow-hidden group shadow-sm border border-slate-200">
      <div className={cn("absolute top-0 right-0 w-24 h-24 bg-gradient-to-br -mr-8 -mt-8 rounded-full blur-2xl opacity-20", colors[color].split(' ')[0])} />
      <div className="flex items-start justify-between relative z-10">
        <div className={cn("p-3 rounded-xl", colors[color].split('icon-bg:')[1])}>
          <Icon className="h-6 w-6" />
        </div>
        <div className="text-right">
          <span className="text-xs font-bold text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-900/30 px-2 py-0.5 rounded-full">+12%</span>
        </div>
      </div>
      <div className="mt-4 relative z-10">
        <h4 className="text-sm font-medium text-slate-500 dark:text-slate-400">{label}</h4>
        <p className="text-2xl font-bold text-slate-900 dark:text-white mt-1" style={{ fontFamily: 'var(--font-display)' }}>{value}</p>
      </div>
    </div>
  );
}

function AgentCard({ name, icon: Icon, status, lastRun }: any) {
  return (
    <div className="card-elevated dark:bg-slate-900 dark:border-slate-800 p-4 flex items-center justify-between hover:border-primary/50 dark:hover:border-primary/50 transition-all cursor-pointer group shadow-sm border border-slate-200">
      <div className="flex items-center gap-4">
        <div className="p-2.5 bg-slate-50 dark:bg-slate-800 rounded-lg text-slate-600 dark:text-slate-400 group-hover:bg-primary/10 dark:group-hover:bg-primary-900/30 group-hover:text-primary dark:group-hover:text-primary-400 transition-colors">
          <Icon className="h-5 w-5" />
        </div>
        <div>
          <h4 className="text-sm font-bold text-slate-900 dark:text-white leading-none mb-1">{name}</h4>
          <p className="text-xs text-slate-500 dark:text-slate-400 flex items-center gap-1.5">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
            {status === 'active' ? 'Active' : 'Paused'} • {lastRun}
          </p>
        </div>
      </div>
      <ChevronRight className="h-4 w-4 text-slate-300 dark:text-slate-700 group-hover:text-primary-400 transition-colors" />
    </div>
  );
}

function ActivityItem({ action, isLast }: any) {
  const getIcon = (type: string) => {
    if (type.includes('SEO')) return <Search className="h-4 w-4" />;
    if (type.includes('Content')) return <FileText className="h-4 w-4" />;
    if (type.includes('Social')) return <Share2 className="h-4 w-4" />;
    return <Zap className="h-4 w-4" />;
  };

  return (
    <div className="flex gap-4 relative">
      {!isLast && (
        <div className="absolute left-[15px] top-[30px] bottom-[-24px] w-[2px] bg-slate-100 dark:bg-slate-800" />
      )}
      <div className="w-[32px] h-[32px] rounded-full bg-white dark:bg-slate-900 border-2 border-slate-100 dark:border-slate-800 flex items-center justify-center shrink-0 z-10 transition-colors">
        <span className="text-slate-600 dark:text-slate-400">
          {getIcon(action.agentName)}
        </span>
      </div>
      <div className="flex-1 pb-6 min-w-0">
        <div className="flex items-center justify-between gap-2 mb-1">
          <h5 className="text-sm font-bold text-slate-900 dark:text-white truncate">{action.actionType}</h5>
          <span className="text-[10px] whitespace-nowrap text-slate-400 dark:text-slate-500 font-medium">
            {new Date(action.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
          </span>
        </div>
        <p className="text-xs text-slate-500 dark:text-slate-400 leading-relaxed line-clamp-2">{action.description}</p>
        <div className="mt-2 flex items-center gap-3">
          <span className="text-[10px] px-2 py-0.5 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 rounded-full font-bold uppercase tracking-wider">{action.agentName}</span>
          {action.wasAutoApplied && (
            <span className="text-[10px] text-emerald-600 dark:text-emerald-400 font-bold flex items-center gap-1">
              <CheckCircle2 className="h-2.5 w-2.5" />
              Auto-applied
            </span>
          )}
        </div>
      </div>
    </div>
  );
}

function OnboardingView({ onComplete }: { onComplete: () => void }) {
  const t = useTranslations('MarketingAutomation.onboarding');
  const [step, setStep] = useState(1);
  const [formData, setFormData] = useState({
    businessUrl: '',
    primaryGoal: 'Leads',
    targetRegions: ''
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async () => {
    setIsSubmitting(true);
    try {
      await api.marketingAutomation.onboard(formData);
      toast.success('Onboarding successful! Triggering AI crawler...');
      onComplete();
    } catch (error) {
      toast.error('Failed to complete onboarding');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="max-w-3xl mx-auto py-12 px-6">
      <div className="text-center mb-12">
        <div className="w-20 h-20 bg-gradient-to-br from-primary-500 to-indigo-600 rounded-3xl shadow-2xl flex items-center justify-center mx-auto mb-6 transform -rotate-6">
          <Zap className="h-10 w-10 text-white" />
        </div>
        <h2 className="text-4xl font-bold text-slate-900 dark:text-white mb-4" style={{ fontFamily: 'var(--font-display)' }}>
          {t('title')}
        </h2>
        <p className="text-lg text-slate-500 dark:text-slate-400">{t('description')}</p>
      </div>

      <div className="card-elevated dark:bg-slate-900 dark:border-slate-800 p-8 md:p-12 shadow-sm border border-slate-200">
        <div className="space-y-8">
          <div>
            <label className="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-2">{t('urlLabel')}</label>
            <div className="relative">
              <Globe className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-400 dark:text-slate-500" />
              <input 
                type="url" 
                className="w-full pl-12 pr-4 py-4 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white rounded-2xl focus:ring-4 focus:ring-primary/10 focus:border-primary transition-all outline-none"
                placeholder={t('urlPlaceholder')}
                value={formData.businessUrl}
                onChange={(e) => setFormData({...formData, businessUrl: e.target.value})}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-2">{t('goalLabel')}</label>
              <select 
                className="w-full px-4 py-4 bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white rounded-2xl focus:ring-4 focus:ring-primary-500/10 focus:border-primary-500 outline-none"
                value={formData.primaryGoal}
                onChange={(e) => setFormData({...formData, primaryGoal: e.target.value})}
              >
                <option value="Leads">{t('goalLeads')}</option>
                <option value="Traffic">{t('goalBrand')}</option>
                <option value="Sales">{t('goalSales')}</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-2">{t('regionsLabel')}</label>
              <input 
                className="w-full px-4 py-4 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white rounded-2xl focus:ring-4 focus:ring-primary/10 focus:border-primary outline-none"
                placeholder={t('regionsPlaceholder')}
                value={formData.targetRegions}
                onChange={(e) => setFormData({...formData, targetRegions: e.target.value})}
              />
            </div>
          </div>

          <button 
            disabled={isSubmitting || !formData.businessUrl}
            onClick={handleSubmit}
            className="w-full btn btn-primary py-5 text-lg font-bold shadow-2xl shadow-primary/30 disabled:opacity-50 active:scale-[0.98] transition-all"
          >
            {isSubmitting ? (
              <span className="flex items-center justify-center gap-3">
                <span className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                Initializing AI Core...
              </span>
            ) : (
              <span className="flex items-center justify-center gap-3">
                {t('submit')} <ChevronRight className="h-5 w-5" />
              </span>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
