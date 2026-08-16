'use client';

import React from 'react';
import { useSubscription } from '@/hooks/useSubscription';
import { Lock } from 'lucide-react';
import Link from 'next/link';

interface FeatureGateProps {
  featureName: string;
  children: React.ReactNode;
  fallback?: React.ReactNode;
  title?: string;
  description?: string;
}

export function FeatureGate({ 
  featureName, 
  children, 
  fallback,
  title = "Premium Feature",
  description = "Upgrade your plan to unlock this feature and take your business to the next level."
}: FeatureGateProps) {
  const { hasFeature, isLoading } = useSubscription();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-12 min-h-[400px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (hasFeature(featureName)) {
    return <>{children}</>;
  }

  if (fallback) {
    return <>{fallback}</>;
  }

  return (
    <div className="flex flex-col items-center justify-center p-12 text-center border rounded-2xl bg-gray-50/50 dark:bg-gray-800/20 border-dashed min-h-[400px]">
      <div className="w-16 h-16 bg-primary-100 dark:bg-primary-900/30 rounded-full flex items-center justify-center mb-6">
        <Lock className="w-8 h-8 text-primary-600 dark:text-primary-400" />
      </div>
      <h2 className="text-2xl font-semibold text-gray-900 dark:text-white mb-2">
        {title}
      </h2>
      <p className="text-gray-500 dark:text-gray-400 max-w-md mb-8">
        {description}
      </p>
      <Link
        href="/settings/billing"
        className="inline-flex items-center justify-center px-6 py-3 border border-transparent text-base font-medium rounded-lg text-white bg-primary-600 hover:bg-primary-700 transition-colors shadow-sm"
      >
        View Upgrade Options
      </Link>
    </div>
  );
}
