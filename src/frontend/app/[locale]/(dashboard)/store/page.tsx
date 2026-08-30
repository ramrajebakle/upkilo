"use client";

import React, { useState, useEffect } from "react";
import { 
  ShoppingBag, Package, Download, Tag, 
  RefreshCcw, AlertTriangle, Plus, LayoutGrid, Loader2
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import api, { apiClient } from "@/lib/api";

export default function EcommerceDashboardPage() {
  const { success, error: toastError } = useToast();
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState(false);

  const [inventory, setInventory] = useState<any[]>([]);
  const [valueMetrics, setValueMetrics] = useState<any>(null);
  const [lowStockAlerts, setLowStockAlerts] = useState<any>(null);

  const fetchData = async () => {
    try {
      // Fetch core inventory list
      const itemsRes = await apiClient.get('/api/v1/inventory');
      if (itemsRes.data?.data) {
        setInventory(itemsRes.data.data);
      }

      // Fetch summary metrics (value)
      const valueRes = await apiClient.get('/api/v1/inventory/value');
      setValueMetrics(valueRes.data);

      // Fetch low stock summary
      const lowStockRes = await apiClient.get('/api/v1/inventory/low-stock');
      setLowStockAlerts(lowStockRes.data?.summary);

    } catch (err: any) {
      console.error("Failed to load inventory data", err);
      toastError("Failed to synchronize store data from the server.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleSync = async () => {
    setSyncing(true);
    await fetchData();
    setSyncing(false);
    success("Inventory synced successfully with live database.");
  };

  if (loading) {
    return (
        <div className="flex flex-col items-center justify-center py-20">
            <Loader2 className="h-10 w-10 text-primary animate-spin mb-4" />
            <p className="text-foreground-secondary">Loading Store & Inventory Data...</p>
        </div>
    );
  }

  return (
    <div className="space-y-8 max-w-6xl">
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Store & Inventory</h1>
          <p className="text-muted-foreground">Manage physical products, digital downloads, and e-commerce stock.</p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" onClick={handleSync} disabled={syncing}>
            <RefreshCcw className={`h-4 w-4 mr-2 ${syncing ? 'animate-spin' : ''}`} />
            Sync Stock
          </Button>
          <Button className="bg-primary hover:bg-primary/90">
            <Plus className="h-4 w-4 mr-2" /> Add Product
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
         <div className="p-4 rounded-xl border bg-muted">
           <div className="flex justify-between items-center mb-2">
             <div className="text-sm font-bold text-foreground-secondary">Retail Value</div>
             <Tag className="h-4 w-4 text-foreground-muted" />
           </div>
           <div className="text-2xl font-bold">
             ${(valueMetrics?.totalRetailValue || 0).toLocaleString(undefined, { minimumFractionDigits: 2 })}
           </div>
           <div className="text-xs text-success-fg font-medium">Estimated potential revenue</div>
         </div>
         <div className="p-4 rounded-xl border bg-muted">
           <div className="flex justify-between items-center mb-2">
             <div className="text-sm font-bold text-foreground-secondary">Total Units</div>
             <Package className="h-4 w-4 text-foreground-muted" />
           </div>
           <div className="text-2xl font-bold">{valueMetrics?.totalUnits || 0}</div>
           <div className="text-xs text-foreground-secondary font-medium tracking-tight">Across {valueMetrics?.totalItems || 0} products</div>
         </div>
         <div className="p-4 rounded-xl border bg-muted">
           <div className="flex justify-between items-center mb-2">
             <div className="text-sm font-bold text-foreground-secondary">Digital Catalog</div>
             <Download className="h-4 w-4 text-foreground-muted" />
           </div>
           <div className="text-2xl font-bold">
             {inventory.filter(i => !i.isRetail).length}
           </div>
           <div className="text-xs text-foreground-secondary font-medium">Automated delivery</div>
         </div>
         <div className="p-4 rounded-xl border bg-muted">
           <div className="flex justify-between items-center mb-2">
             <div className="text-sm font-bold text-foreground-secondary">Low Stock Alerts</div>
             <AlertTriangle className="h-4 w-4 text-danger-fg" />
           </div>
           <div className="text-2xl font-bold text-danger-fg">
             {(lowStockAlerts?.outOfStock || 0) + (lowStockAlerts?.low || 0)}
           </div>
           <div className="text-xs text-danger-fg font-medium hover:underline cursor-pointer">
             {lowStockAlerts?.outOfStock || 0} Critical / {lowStockAlerts?.low || 0} Low
           </div>
         </div>
      </div>

      <Card>
         <CardHeader>
           <CardTitle className="flex items-center gap-2">
             <LayoutGrid className="h-5 w-5 text-foreground-secondary" />
             Product Catalog
           </CardTitle>
           <CardDescription>Live database representation of all store items.</CardDescription>
         </CardHeader>
         <CardContent>
           <div className="rounded-md border overflow-hidden">
             <div className="grid grid-cols-5 text-sm font-medium text-foreground-secondary p-4 border-b bg-muted">
                <div className="col-span-2">Product Name</div>
                <div>Type</div>
                <div>Price</div>
                <div>Stock</div>
             </div>
             
             {inventory.length === 0 ? (
               <div className="p-8 text-center text-foreground-secondary text-sm">
                 No inventory items found. Add products to populate this list.
               </div>
             ) : (
               inventory.map((item: any) => (
                  <div key={item.id} className="grid grid-cols-5 text-sm p-4 border-b last:border-0 items-center">
                     <div className="col-span-2 font-medium">
                       <div>{item.name}</div>
                       <div className="text-xs text-foreground-muted font-mono mt-0.5">{item.sku}</div>
                     </div>
                     <div>
                       <span className={`px-2 py-1 rounded text-[10px] font-bold uppercase tracking-wider ${!item.isRetail ? 'bg-brand-subtle text-primary' : 'bg-blue-100 text-blue-700'}`}>
                         {!item.isRetail ? 'Digital' : 'Physical'}
                       </span>
                     </div>
                     <div>${item.salePrice?.toFixed(2) || '0.00'}</div>
                     <div>
                       {item.quantityOnHand === 0 && item.isRetail ? (
                         <span className="text-danger-fg font-bold flex items-center gap-1"><AlertTriangle className="h-3 w-3" /> Out</span>
                       ) : !item.isRetail ? (
                         <span className="text-foreground-secondary">Unlimited</span>
                       ) : (
                         <span className={item.isLowStock ? 'text-warning-fg font-bold' : ''}>{item.quantityOnHand} units</span>
                       )}
                     </div>
                  </div>
               ))
             )}
           </div>
         </CardContent>
      </Card>
    </div>
  );
}
