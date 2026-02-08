import React, { useMemo } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, BarChart, Bar } from 'recharts';
import { useAnalyticsHub } from '../hooks/useAnalyticsHub';
import { Activity, Zap } from 'lucide-react';

const RealTimeDashboard: React.FC = () => {
    const { stats } = useAnalyticsHub();

    // Grouping stats by material for potentially multiple lines or just showing aggregate
    // For simplicity, let's visualize the average change for the top materials

    const chartData = useMemo(() => {
        if (!stats.length) return [];
        // Transform for chart if needed. Currently stats is flat list of MaterialStatsDto.
        // If multiple materials are present for same hour, it might be messy.
        // The backend query groups by hour and material_id.
        // Let's filter for a specific material or aggregate.
        // For the "Wow" factor, let's show top 5 active materials in a Bar chart of average change.
        return stats.slice(0, 10);
    }, [stats]);

    return (
        <div className="p-6 bg-slate-50 min-h-screen">
            <div className="mb-8">
                <h1 className="text-3xl font-bold text-slate-900 flex items-center gap-2">
                    <Activity className="h-8 w-8 text-blue-600" />
                    Real-Time Analytics
                    <span className="text-xs font-normal text-white bg-red-500 px-2 py-1 rounded-full animate-pulse flex items-center gap-1">
                        <Zap size={12} /> LIVE
                    </span>
                </h1>
                <p className="text-slate-600 mt-2">Streaming advanced statistical aggregates from TimescaleDB via SignalR</p>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-100">
                    <h3 className="text-lg font-semibold mb-4 text-slate-800">Inventory Velocity (Last 24h)</h3>
                    <div className="h-[300px] w-full">
                        <ResponsiveContainer width="100%" height="100%">
                            <BarChart data={chartData}>
                                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                                <XAxis dataKey="materialId" />
                                <YAxis />
                                <Tooltip
                                    contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}
                                />
                                <Legend />
                                <Bar dataKey="averageChange" fill="#3b82f6" radius={[4, 4, 0, 0]} name="Avg Movement" />
                            </BarChart>
                        </ResponsiveContainer>
                    </div>
                </div>

                <div className="bg-white p-6 rounded-xl shadow-sm border border-slate-100">
                    <h3 className="text-lg font-semibold mb-4 text-slate-800">Volatility Index (StdDev)</h3>
                    <div className="h-[300px] w-full">
                        <ResponsiveContainer width="100%" height="100%">
                            <LineChart data={chartData}>
                                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                                <XAxis dataKey="materialId" />
                                <YAxis />
                                <Tooltip
                                    contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}
                                />
                                <Legend />
                                <Line type="monotone" dataKey="stdDevChange" stroke="#ef4444" strokeWidth={2} name="Volatility" dot={{ r: 4 }} />
                            </LineChart>
                        </ResponsiveContainer>
                    </div>
                </div>
            </div>

            <div className="mt-8 bg-white rounded-xl shadow-sm border border-slate-100 overflow-hidden">
                <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
                    <h3 className="font-semibold text-slate-800">Live Data Stream</h3>
                </div>
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead className="text-xs text-slate-500 uppercase bg-slate-50">
                            <tr>
                                <th className="px-6 py-3">Timestamp</th>
                                <th className="px-6 py-3">Material ID</th>
                                <th className="px-6 py-3 text-right">Avg Change</th>
                                <th className="px-6 py-3 text-right">Median</th>
                                <th className="px-6 py-3 text-right">Std Dev</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                            {stats.map((item, idx) => (
                                <tr key={idx} className="hover:bg-slate-50 transition-colors">
                                    <td className="px-6 py-4 font-mono text-slate-600">
                                        {new Date(item.hour).toLocaleTimeString()}
                                    </td>
                                    <td className="px-6 py-4 font-medium text-slate-900">{item.materialId}</td>
                                    <td className="px-6 py-4 text-right">{item.averageChange.toFixed(2)}</td>
                                    <td className="px-6 py-4 text-right text-slate-600">{item.medianChange.toFixed(2)}</td>
                                    <td className="px-6 py-4 text-right font-mono text-xs">
                                        <span className={`px-2 py-1 rounded-full ${item.stdDevChange > 2 ? 'bg-red-100 text-red-700' : 'bg-green-100 text-green-700'}`}>
                                            {item.stdDevChange.toFixed(4)}
                                        </span>
                                    </td>
                                </tr>
                            ))}
                            {stats.length === 0 && (
                                <tr>
                                    <td colSpan={5} className="px-6 py-8 text-center text-slate-400">
                                        Waiting for data stream...
                                    </td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
};

export default RealTimeDashboard;
