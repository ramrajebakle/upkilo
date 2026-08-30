"use client";

import React from "react";
import { ShieldCheck, AlertCircle, Plug, Puzzle, Loader2, FlaskConical, Unplug } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";

export interface IntegrationItem {
  id: string;
  name: string;
  category: string;
  description: string;
  icon?: string;
  authType: "api_key" | "key_pair" | "oauth";
  fields: { key: string; label: string; secret: boolean; placeholder: string }[];
  features: string[];
}

export interface IntegrationWrapper {
  item: IntegrationItem;
  isConnected: boolean;
  isVerified: boolean;
  lastVerifiedAt?: string;
  verificationError?: string;
  connectedAt?: string;
  externalAccountId?: string;
}

interface Props {
  integration: IntegrationWrapper;
  onConnect: (id: string) => void;
  onDisconnect: (id: string) => void;
  onTest: (id: string) => void;
  onManage: (id: string) => void;
  loading: boolean;
  testing: boolean;
}

export function IntegrationCard({
  integration,
  onConnect,
  onDisconnect,
  onTest,
  onManage,
  loading,
  testing,
}: Props) {
  const { item, isConnected, isVerified, verificationError } = integration;

  const statusBadge = () => {
    if (!isConnected)
      return <Badge variant="secondary" className="text-xs">Not connected</Badge>;
    if (isVerified)
      return (
        <Badge className="bg-green-100 text-green-700 text-xs flex items-center gap-1">
          <ShieldCheck className="w-3 h-3" /> Verified
        </Badge>
      );
    return (
      <Badge className="bg-yellow-100 text-yellow-700 text-xs flex items-center gap-1">
        <AlertCircle className="w-3 h-3" /> Unverified
      </Badge>
    );
  };

  return (
    <Card className="relative overflow-hidden group hover:border-primary/50 transition-colors flex flex-col">
      <CardHeader className="pb-3">
        <div className="flex justify-between items-start mb-2">
          <div className="w-10 h-10 bg-muted rounded-lg flex items-center justify-center shrink-0">
            {item.icon ? (
              <img src={item.icon} alt={item.name} className="w-6 h-6 object-contain" />
            ) : (
              <Puzzle className="w-5 h-5 text-foreground-muted" />
            )}
          </div>
          {statusBadge()}
        </div>
        <CardTitle className="text-base">{item.name}</CardTitle>
        <CardDescription className="text-[10px] uppercase tracking-wider font-semibold text-foreground-muted">
          {item.category}
        </CardDescription>
      </CardHeader>

      <CardContent className="flex-1 flex flex-col gap-4">
        <p className="text-sm text-foreground-secondary flex-1">{item.description}</p>

        {/* Feature chips */}
        <div className="flex flex-wrap gap-1">
          {item.features.slice(0, 3).map((f) => (
            <span key={f} className="text-[10px] bg-muted text-foreground-secondary rounded px-2 py-0.5">{f}</span>
          ))}
        </div>

        {/* Verification error */}
        {isConnected && !isVerified && verificationError && (
          <p className="text-xs text-red-600 bg-red-50 rounded p-2 flex items-start gap-1">
            <AlertCircle className="w-3 h-3 mt-0.5 shrink-0" />
            {verificationError}
          </p>
        )}

        {/* Actions */}
        <div className="mt-auto flex flex-col gap-2">
          {isConnected ? (
            <>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  className="flex-1 text-xs"
                  onClick={() => onManage(item.id)}
                >
                  Manage
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  className="flex-1 text-xs"
                  onClick={() => onTest(item.id)}
                  disabled={testing || loading}
                >
                  {testing ? <Loader2 className="w-3 h-3 animate-spin" /> : <FlaskConical className="w-3 h-3 mr-1" />}
                  {testing ? "Testing…" : "Test"}
                </Button>
              </div>
              <Button
                variant="ghost"
                size="sm"
                className="w-full text-danger-fg hover:text-red-700 hover:bg-red-50 text-xs"
                onClick={() => onDisconnect(item.id)}
                disabled={loading}
              >
                {loading ? <Loader2 className="w-3 h-3 animate-spin mr-1" /> : <Unplug className="w-3 h-3 mr-1" />}
                Disconnect
              </Button>
            </>
          ) : (
            <Button
              className="w-full bg-gray-900 hover:bg-gray-800 text-sm"
              onClick={() => onConnect(item.id)}
              disabled={loading}
            >
              {loading ? <Loader2 className="w-4 h-4 animate-spin mr-2" /> : <Plug className="w-4 h-4 mr-2" />}
              Connect
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
