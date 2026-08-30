import React from "react";
import { MoreHorizontal } from "lucide-react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";

interface KanbanDeal {
  id: string;
  title: string;
  value: number;
  clientName: string;
}

interface KanbanColumnProps {
  title: string;
  deals: KanbanDeal[];
}

export const KanbanColumn: React.FC<KanbanColumnProps> = ({ title, deals }) => {
  return (
    <div className="flex flex-col w-80 shrink-0 bg-muted/50 rounded-xl p-3 h-[calc(100vh-12rem)] border border-border-subtle">
      <div className="flex items-center justify-between mb-4 px-1">
        <h3 className="font-semibold text-foreground flex items-center gap-2">
          {title} <span className="text-xs bg-gray-200 text-foreground-secondary px-2 py-0.5 rounded-full">{deals.length}</span>
        </h3>
        <Button variant="ghost" size="icon" className="h-6 w-6">
          <MoreHorizontal className="w-4 h-4 text-foreground-muted" />
        </Button>
      </div>

      <div className="flex-1 overflow-y-auto space-y-3 pr-1 pb-2 scrollbar-thin scrollbar-thumb-gray-200">
        {deals.map((deal) => (
          <Card key={deal.id} className="cursor-pointer hover:border-primary/50 transition-colors shadow-sm bg-card">
            <CardContent className="p-4">
              <div className="text-sm font-medium mb-1 truncate">{deal.title}</div>
              <div className="text-xs text-foreground-secondary mb-3">{deal.clientName}</div>
              <div className="flex items-center justify-between">
                <span className="text-sm font-semibold text-foreground">${deal.value.toLocaleString()}</span>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
};
