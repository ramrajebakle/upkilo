'use client';

import React, { createContext, useContext, useEffect, useState, ReactNode, useRef } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr';
import { useAuthStore } from '../store/authStore';
import { useToast } from '@/components/ui/Toast';

// Define types matching the backend DTOs
export interface DashboardStats {
    todayBookings: number;
    todayRevenue: number;
    newClients: number;
    pendingBookings: number;
}

export interface BookingNotification {
    bookingId: string;
    status: string;
    message: string;
    clientName?: string;
    serviceName?: string;
    startTime?: string;
    staffName?: string;
}

export interface EscalationNotification {
    id: string;
    tenantId: string;
    module: string;
    reason: string;
    severity: string;
    metadata: any;
    timestamp: string;
    requiresApproval: boolean;
}

interface SignalRContextType {
    connection: HubConnection | null;
    isConnected: boolean;
    invoke: (methodName: string, ...args: any[]) => Promise<any>;
}

const SignalRContext = createContext<SignalRContextType>({
    connection: null,
    isConnected: false,
    invoke: async () => { },
});

export const useSignalR = () => useContext(SignalRContext);

interface SignalRProviderProps {
    children: ReactNode;
}

export const SignalRProvider: React.FC<SignalRProviderProps> = ({ children }) => {
    const { token, user } = useAuthStore();
    const [connection, setConnection] = useState<HubConnection | null>(null);
    const [isConnected, setIsConnected] = useState(false);
    const connectionRef = useRef<HubConnection | null>(null);
    const { success, warning, addToast } = useToast();

    useEffect(() => {
        if (!token) return;

        // Guard: if a connection is already connected/connecting, don't create a new one
        if (
            connectionRef.current &&
            (connectionRef.current.state === HubConnectionState.Connected ||
             connectionRef.current.state === HubConnectionState.Connecting ||
             connectionRef.current.state === HubConnectionState.Reconnecting)
        ) {
            return;
        }

        const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

        // The hub rejects raw JWTs in the query string (they would leak into access logs and
        // browser history). Exchange the JWT for a single-use 30s ticket instead. Called again
        // on every reconnect, which is required since each ticket is consumed on use.
        const fetchTicket = async () => {
            const res = await fetch(`${API_URL}/api/v1/signalr/ticket`, {
                method: 'POST',
                headers: { Authorization: `Bearer ${token}` },
                credentials: 'include',
            });
            if (!res.ok) throw new Error(`SignalR ticket request failed: ${res.status}`);
            const { ticket } = await res.json();
            return ticket as string;
        };

        const newConnection = new HubConnectionBuilder()
            .withUrl(`${API_URL}/hubs/notifications`, {
                accessTokenFactory: fetchTicket,
            })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Warning)
            .build();

        connectionRef.current = newConnection;

        let cancelled = false;
        const startConnection = async () => {
            try {
                if (cancelled) return;
                await newConnection.start();
                if (cancelled) { newConnection.stop(); return; }
                setIsConnected(true);
                setConnection(newConnection);

                if (user?.tenantId) {
                    await newConnection.invoke('SubscribeToDashboard');
                }

            } catch (err) {
                if (!cancelled) {
                    console.error('SignalR Connection Error: ', err);
                    setTimeout(startConnection, 5000);
                }
            }
        };

        startConnection();

        // Event Listeners
        newConnection.on('BookingCreated', (notification: BookingNotification) => {
            success(`New Booking: ${notification.clientName} booked ${notification.serviceName}`);
        });

        newConnection.on('BookingCancelled', (notification: BookingNotification) => {
            warning(`Booking Cancelled: ${notification.clientName} cancelled ${notification.serviceName}`);
        });

        newConnection.on('SystemEscalation', (notification: EscalationNotification) => {
            addToast(`URGENT: ${notification.reason} (Module: ${notification.module}, Severity: ${notification.severity})`, 'warning', 10000);
        });

        newConnection.onclose(() => {
            setIsConnected(false);
            setConnection(null);
        });

        return () => {
            cancelled = true;
            if (connectionRef.current) {
                connectionRef.current.stop();
                connectionRef.current = null;
            }
            setConnection(null);
            setIsConnected(false);
        };
    }, [token, user?.tenantId, success, warning]);

    const invoke = async (methodName: string, ...args: any[]) => {
        if (connection && isConnected) {
            return await connection.invoke(methodName, ...args);
        }
        return Promise.reject('No SignalR connection');
    };

    return (
        <SignalRContext.Provider value={{ connection, isConnected, invoke }}>
            {children}
        </SignalRContext.Provider>
    );
};
