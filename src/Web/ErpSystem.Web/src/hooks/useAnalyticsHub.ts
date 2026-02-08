import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useEffect, useState, useRef } from 'react';

export interface MaterialStats {
    hour: string;
    materialId: string;
    medianChange: number;
    averageChange: number;
    stdDevChange: number;
}

export const useAnalyticsHub = (hubUrl: string = '/hubs/analytics') => {
    const [connection, setConnection] = useState<HubConnection | null>(null);
    const [stats, setStats] = useState<MaterialStats[]>([]);
    const latestStatsRef = useRef<MaterialStats[]>([]);

    useEffect(() => {
        const newConnection = new HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        setConnection(newConnection);
    }, [hubUrl]);

    useEffect(() => {
        if (connection) {
            connection.start()
                .then(() => {
                    console.log('Connected to SignalR Hub');
                    connection.on('ReceiveStats', (data: MaterialStats[]) => {
                        // Merge or replace stats. Since it's a dashboard snapshot, replacement or appending?
                        // The backend sends a snapshot of top 50 stats. So we can just replace.
                        // However, for a real-time feel, maybe we want to append if it was a stream.
                        // But here it sends "latest stats". Let's replace.
                        setStats(data);
                        latestStatsRef.current = data;
                    });
                })
                .catch(e => console.error('Connection failed: ', e));
        }

        return () => {
            connection?.stop();
        };
    }, [connection]);

    return { stats, connection };
};
