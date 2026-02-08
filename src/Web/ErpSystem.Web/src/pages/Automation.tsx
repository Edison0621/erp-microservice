import React from 'react';
import { Zap, Play, ToggleLeft as Toggle, Settings, History, Plus } from 'lucide-react';

export const Automation = () => {
    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 font-display uppercase tracking-tight">Automation Hub</h1>
                    <p className="text-gray-500 font-display text-sm tracking-tight font-medium uppercase opacity-60">Event-driven Workflow Engine & Rule Design</p>
                </div>
                <button className="bg-blue-600 text-white px-4 py-2 rounded-lg font-bold hover:bg-blue-700 transition-all shadow-lg shadow-blue-500/20 flex items-center gap-2 uppercase tracking-tighter text-sm">
                    <Plus size={18} /> Create New Rule
                </button>
            </div>

            {/* Quick Stats */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="bg-gradient-to-br from-blue-600 to-indigo-700 p-6 rounded-2xl text-white shadow-xl shadow-blue-600/10">
                    <div className="flex justify-between items-center mb-4">
                        <div className="w-10 h-10 bg-white/20 rounded-xl flex items-center justify-center backdrop-blur-md">
                            <Zap size={20} />
                        </div>
                        <span className="text-[10px] font-bold uppercase tracking-widest bg-white/20 px-2 py-1 rounded">Live</span>
                    </div>
                    <p className="text-blue-100 text-xs font-bold uppercase tracking-widest opacity-80">Executions Today</p>
                    <p className="text-4xl font-black mt-1">1,429</p>
                    <p className="text-blue-200 text-xs mt-4 flex items-center gap-1">
                        <TrendingUp size={12} /> 12% increase vs yesterday
                    </p>
                </div>
                <div className="bg-white p-6 rounded-2xl border border-gray-100 shadow-sm">
                    <div className="w-10 h-10 bg-green-50 rounded-xl flex items-center justify-center text-green-600 mb-4">
                        <Play size={20} />
                    </div>
                    <p className="text-gray-400 text-[10px] font-bold uppercase tracking-widest">Active Rules</p>
                    <p className="text-3xl font-black text-gray-900 font-display">42</p>
                    <div className="mt-4 flex items-center gap-2">
                        <div className="flex -space-x-2">
                            {[1, 2, 3].map(i => (
                                <div key={i} className={`w-6 h-6 rounded-full border-2 border-white bg-gray-200`} />
                            ))}
                        </div>
                        <span className="text-xs text-gray-500 font-medium tracking-tight">Managed by 3 teams</span>
                    </div>
                </div>
                <div className="bg-white p-6 rounded-2xl border border-gray-100 shadow-sm">
                    <div className="w-10 h-10 bg-purple-50 rounded-xl flex items-center justify-center text-purple-600 mb-4">
                        <History size={20} />
                    </div>
                    <p className="text-gray-400 text-[10px] font-bold uppercase tracking-widest">Avg. Response Time</p>
                    <p className="text-3xl font-black text-gray-900 font-display">84ms</p>
                    <p className="text-[10px] text-purple-600 font-bold mt-4 uppercase tracking-widest">Enterprise performance</p>
                </div>
            </div>

            {/* Active Rules List */}
            <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden font-display">
                <div className="p-6 border-b border-gray-100 flex justify-between items-center">
                    <h2 className="font-black text-gray-900 uppercase tracking-tight text-lg">Active Workflows</h2>
                    <div className="flex gap-2">
                        <button className="p-2 bg-gray-50 rounded-lg text-gray-400 hover:text-gray-600 transition-colors">
                            <Settings size={18} />
                        </button>
                    </div>
                </div>
                <div className="divide-y divide-gray-50 font-display">
                    {[
                        { name: "Low Stock Alert", trigger: "InventoryBelowMinimum", actions: 3, status: true, lastRun: "2m ago" },
                        { name: "Auto-Approve Micro-Purchases", trigger: "PurchaseOrderCreated", actions: 2, status: true, lastRun: "15m ago" },
                        { name: "Production Delay Notification", trigger: "ProductionScheduleMissed", actions: 4, status: false, lastRun: "1d ago" },
                        { name: "New Customer Onboarding", trigger: "CustomerOnboarded", actions: 5, status: true, lastRun: "3h ago" },
                    ].map((rule, idx) => (
                        <div key={idx} className="p-6 flex items-center justify-between hover:bg-gray-50/50 transition-colors group">
                            <div className="flex items-center gap-4">
                                <div className={`w-10 h-10 rounded-xl flex items-center justify-center transition-all ${rule.status ? 'bg-blue-50 text-blue-600 group-hover:bg-blue-600 group-hover:text-white group-hover:rotate-12' : 'bg-gray-100 text-gray-400'}`}>
                                    <Zap size={20} />
                                </div>
                                <div>
                                    <h3 className="font-bold text-gray-900">{rule.name}</h3>
                                    <div className="flex items-center gap-2 mt-1">
                                        <span className="text-[10px] font-bold uppercase tracking-widest text-gray-400 bg-gray-50 px-2 py-0.5 rounded italic">Trigger: {rule.trigger}</span>
                                        <span className="text-[10px] font-bold uppercase tracking-widest text-blue-500">{rule.actions} Actions</span>
                                    </div>
                                </div>
                            </div>
                            <div className="flex items-center gap-8">
                                <div className="text-right">
                                    <p className="text-[10px] font-bold text-gray-400 uppercase tracking-widest">Last Triggered</p>
                                    <p className="text-xs font-bold text-gray-600">{rule.lastRun}</p>
                                </div>
                                <div className="flex items-center gap-3">
                                    <div className={`w-12 h-6 rounded-full relative transition-colors cursor-pointer ${rule.status ? 'bg-blue-600' : 'bg-gray-200'}`}>
                                        <div className={`absolute top-1 w-4 h-4 rounded-full bg-white transition-all ${rule.status ? 'right-1' : 'left-1'}`} />
                                    </div>
                                    <button className="text-gray-300 hover:text-gray-900 transition-colors">
                                        <Settings size={18} />
                                    </button>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            {/* Event Console (Real-time simplified) */}
            <div className="bg-gray-900 rounded-2xl p-6 shadow-2xl border border-gray-800 font-mono text-xs">
                <div className="flex items-center justify-between mb-4 border-b border-gray-800 pb-4">
                    <div className="flex items-center gap-2">
                        <div className="w-2 h-2 rounded-full bg-red-500 animate-pulse" />
                        <span className="text-gray-400 font-bold uppercase tracking-widest text-[10px]">Real-time Hub Events</span>
                    </div>
                    <span className="text-gray-500">v2.4.0-stable</span>
                </div>
                <div className="space-y-2">
                    <p className="text-green-400"><span className="text-gray-600">[16:05:22]</span> EVENT_RECEIVED: <span className="text-blue-400">InventoryBelowMinimum</span> (Material: MAT-ST-002)</p>
                    <p className="text-blue-300"> └─ RULE_MATCH: <span className="font-bold text-white">Low Stock Alert</span></p>
                    <p className="text-blue-300"> └─ ACTION_EXECUTED: <span className="text-gray-400">SendEmail(Procurement)</span> {'->'} STATUS: SUCCESS</p>
                    <p className="text-gray-500 mt-2"><span className="text-gray-600">[16:05:18]</span> HEARTBEAT: All Dapr sidecars operational</p>
                </div>
            </div>
        </div>
    );
};

const TrendingUp = ({ size }: { size: number }) => (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="23 6 13.5 15.5 8.5 10.5 1 18"></polyline><polyline points="17 6 23 6 23 12"></polyline></svg>
);
