import { useEffect, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { useToast } from '@/components/ui/Toast';

const HUB_URL = process.env.NEXT_PUBLIC_API_URL 
    ? `${process.env.NEXT_PUBLIC_API_URL}/hubs/notifications` 
    : 'https://localhost:7001/hubs/notifications';

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

export interface SystemNotification {
    id: string;
    title: string;
    message: string;
    type: 'info' | 'error' | 'success' | 'warning';
    timestamp: string;
    isUrgent: boolean;
}

export function useSignalR() {
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [status, setStatus] = useState<ConnectionStatus>('disconnected');
    const { addToast } = useToast();

    // Initialize connection
    useEffect(() => {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(HUB_URL, {
                // In production, you'd add the access token here
                // accessTokenFactory: () => getAccessToken()
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        setStatus('connecting');

        newConnection.start()
            .then(() => {
                console.log('SignalR Connected');
                setStatus('connected');
            })
            .catch((err) => {
                console.error('SignalR Connection Error:', err);
                setStatus('disconnected');
            });

        newConnection.onreconnecting(() => setStatus('reconnecting'));
        newConnection.onreconnected(() => setStatus('connected'));
        newConnection.onclose(() => setStatus('disconnected'));

        setConnection(newConnection);

        return () => {
            if (newConnection) {
                newConnection.stop();
            }
        };
    }, []);

    // Set up global listeners
    useEffect(() => {
        if (!connection) return;

        // Toast notifications from server
        connection.on('ToastMessage', (notification: { title: string, message?: string, type: 'success' | 'error' | 'warning' | 'info' }) => {
            const text = notification.message ? `${notification.title}: ${notification.message}` : notification.title;
            // Map types if necessary or pass directly if they match
            addToast(text, notification.type);
        });

        // System notifications
        connection.on('SystemNotification', (notif: SystemNotification) => {
            addToast(notif.message, notif.type === 'error' ? 'error' : 'info');
        });

        return () => {
            connection.off('ToastMessage');
            connection.off('SystemNotification');
        };
    }, [connection, addToast]);

    // Helper to subscribe to specific events
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const on = useCallback((methodName: string, callback: (...args: any[]) => void) => {
        if (!connection) return () => {};
        
        connection.on(methodName, callback);
        return () => connection.off(methodName, callback);
    }, [connection]);

    // Helper to invoke server methods
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const invoke = useCallback(async (methodName: string, ...args: any[]) => {
        if (!connection || status !== 'connected') {
            return Promise.reject('SignalR not connected');
        }
        return connection.invoke(methodName, ...args);
    }, [connection, status]);

    return {
        connection,
        status,
        on,
        invoke
    };
}
