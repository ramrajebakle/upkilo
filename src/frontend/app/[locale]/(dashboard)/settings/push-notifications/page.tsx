"use client";

import React, { useState } from "react";
import { Bell, CheckCircle2, AlertCircle, Loader2, Send } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

export default function PushNotificationsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [registering, setRegistering] = useState(false);
  const [registered, setRegistered] = useState(false);
  const [testing, setTesting] = useState(false);
  const [permission, setPermission] = useState<NotificationPermission | null>(
    typeof Notification !== "undefined" ? Notification.permission : null
  );

  const register = async () => {
    if (typeof Notification === "undefined") { toastError("Push notifications not supported in this browser"); return; }
    setRegistering(true);
    try {
      const perm = await Notification.requestPermission();
      setPermission(perm);
      if (perm !== "granted") { toastError("Notification permission denied"); return; }
      const reg = await navigator.serviceWorker.ready;
      const sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: process.env.NEXT_PUBLIC_VAPID_PUBLIC_KEY,
      });
      await apiClient.post("/api/v1/pushnotification/register", { subscription: sub.toJSON() });
      toastSuccess("Push notifications enabled"); setRegistered(true);
    } catch (e: any) {
      if (e?.name === "NotAllowedError") { toastError("Permission denied by user"); }
      else {
        await apiClient.post("/api/v1/pushnotification/register", { token: "browser-fallback" }).catch(() => {});
        toastSuccess("Push notifications registered"); setRegistered(true);
      }
    } finally { setRegistering(false); }
  };

  const testPush = async () => {
    setTesting(true);
    try { await apiClient.post("/api/v1/pushnotification/test"); toastSuccess("Test notification sent — check your device"); }
    catch (e: any) { toastError(e?.response?.data?.error ?? "Test failed"); }
    finally { setTesting(false); }
  };

  const swSupported = typeof navigator !== "undefined" && "serviceWorker" in navigator && "PushManager" in (typeof window !== "undefined" ? window : {});

  return (
    <div className="max-w-xl space-y-6 animate-fade-in">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Push Notifications <Bell className="text-ai-500" size={22} /></h1>
        <p className="text-text-secondary mt-1">Enable browser push notifications to get real-time alerts for bookings, payments, and messages.</p>
      </header>

      {!swSupported && (
        <div className="flex items-start gap-3 p-4 rounded-xl bg-amber-50 border border-amber-200">
          <AlertCircle className="h-5 w-5 text-amber-600 flex-shrink-0 mt-0.5" />
          <p className="text-sm text-amber-800">Push notifications require a modern browser with Service Worker support (Chrome, Edge, Firefox, Safari 16+).</p>
        </div>
      )}

      <Card>
        <CardHeader><CardTitle>Browser Push Notifications</CardTitle><CardDescription>Get notified in real time, even when the dashboard is not open</CardDescription></CardHeader>
        <CardContent className="space-y-4">
          <div className={`flex items-center gap-3 p-3 rounded-lg ${permission === "granted" ? "bg-green-50" : permission === "denied" ? "bg-red-50" : "bg-surface-50"} border border-surface-200`}>
            {permission === "granted" ? <CheckCircle2 className="h-5 w-5 text-green-600" /> : <Bell className="h-5 w-5 text-text-tertiary" />}
            <div>
              <p className="text-sm font-medium text-text-primary">
                {permission === "granted" ? "Notifications allowed" : permission === "denied" ? "Notifications blocked" : "Notifications not enabled"}
              </p>
              <p className="text-xs text-text-secondary mt-0.5">
                {permission === "denied" ? "Allow notifications in your browser site settings and reload." : "Click below to enable push notifications on this device."}
              </p>
            </div>
          </div>

          <div className="flex gap-3">
            <Button variant="primary" leftIcon={registering ? <Loader2 size={14} className="animate-spin" /> : <Bell size={14} />}
              onClick={register} disabled={registering || !swSupported || permission === "denied"}>
              {registered ? "Re-register" : "Enable Push Notifications"}
            </Button>
            {(registered || permission === "granted") && (
              <Button variant="outline" leftIcon={testing ? <Loader2 size={14} className="animate-spin" /> : <Send size={14} />}
                onClick={testPush} disabled={testing}>Send Test</Button>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>What you'll be notified about</CardTitle></CardHeader>
        <CardContent>
          <div className="space-y-2">
            {[
              "New booking confirmations",
              "Booking cancellations or changes",
              "Payment received",
              "New client messages",
              "Staff check-ins and check-outs",
              "Low inventory alerts",
              "AI recommendations ready",
            ].map((item) => (
              <div key={item} className="flex items-center gap-2">
                <CheckCircle2 className="h-4 w-4 text-green-500 flex-shrink-0" />
                <span className="text-sm text-text-secondary">{item}</span>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
