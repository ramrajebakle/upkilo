import React from "react";
import { AlertCircle, TrendingUp, CheckCircle, Lightbulb, X } from "lucide-react";
import { Card, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";

export interface AIAction {
  label: string;
  onClick: () => void;
  primary?: boolean;
}

export interface AIInsightCardProps {
  type: "risk" | "opportunity" | "win" | "trend";
  title: string;
  description: string;
  confidence?: number;
  actions: AIAction[];
  dismissible?: boolean;
  onDismiss?: () => void;
}

const typeConfig = {
  risk: {
    icon: AlertCircle,
    color: "text-danger-500",
    bgColor: "bg-danger-50",
    borderColor: "border-l-danger-500",
    label: "RISK",
  },
  opportunity: {
    icon: Lightbulb,
    color: "text-warning-500", // system design mentions platform-500 but also amber for opportunity, let's use platform-500 as per spec "border-platform-500"
    bgColor: "bg-platform-50",
    borderColor: "border-l-platform-500",
    label: "OPPORTUNITY",
  },
  win: {
    icon: CheckCircle,
    color: "text-success-500",
    bgColor: "bg-success-50",
    borderColor: "border-l-success-500",
    label: "WIN",
  },
  trend: {
    icon: TrendingUp,
    color: "text-info-500",
    bgColor: "bg-info-50",
    borderColor: "border-l-info-500",
    label: "TREND",
  },
};

export const AIInsightCard: React.FC<AIInsightCardProps> = ({
  type,
  title,
  description,
  confidence,
  actions,
  dismissible = true,
  onDismiss,
}) => {
  const config = typeConfig[type];
  const Icon = config.icon;

  return (
    <Card className={`relative overflow-hidden border-l-[4px] ${config.borderColor} animate-fade-in-up`}>
      <CardContent className="p-5">
        {dismissible && (
          <button
            onClick={onDismiss}
            className="absolute top-3 right-3 text-text-tertiary hover:text-text-primary transition-colors"
            aria-label="Dismiss insight"
          >
            <X size={16} />
          </button>
        )}

        <div className="flex items-start gap-4">
          <div className={`p-2 rounded-lg ${config.bgColor} ${config.color} shrink-0`}>
            <Icon size={20} />
          </div>

          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <span className={`text-xs font-bold tracking-wider ${config.color}`}>
                {config.label}
              </span>
              {confidence !== undefined && (
                <span className="text-xs text-text-tertiary">
                  Confidence: {confidence}%
                </span>
              )}
            </div>

            <h3 className="text-base font-semibold text-text-primary mb-1">
              {title}
            </h3>
            <p className="text-sm text-text-secondary leading-relaxed mb-4">
              {description}
            </p>

            {actions.length > 0 && (
              <div className="flex flex-wrap items-center gap-2">
                {actions.map((action, index) => (
                  <Button
                    key={index}
                    variant={action.primary ? "primary" : "secondary"}
                    size="sm"
                    onClick={action.onClick}
                  >
                    {action.label}
                  </Button>
                ))}
              </div>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
};
