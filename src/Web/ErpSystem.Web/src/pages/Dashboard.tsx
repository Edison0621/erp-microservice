import React from 'react';
import { ArrowUpRight, ArrowDownRight, DollarSign, Package, Users, Activity, Zap, ShieldCheck } from 'lucide-react';
import { Link } from 'react-router-dom';

const StatCard = ({ title, value, change, icon: Icon, trend }: any) => (
    <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm">
        <div className="flex items-center justify-between mb-4">
            <div className="w-12 h-12 rounded-lg bg-blue-50 flex items-center justify-center text-blue-600">
                <Icon size={24} />
            </div>
            {trend === 'up' ? (
                <span className="flex items-center text-emerald-600 text-sm font-medium bg-emerald-50 px-2 py-1 rounded-full">
                    <ArrowUpRight size={16} className="mr-1" /> {change}
                </span>
            ) : (
                <span className="flex items-center text-rose-600 text-sm font-medium bg-rose-50 px-2 py-1 rounded-full">
                    <ArrowDownRight size={16} className="mr-1" /> {change}
                </span>
            )}
        </div>
        <h3 className="text-gray-500 text-sm font-medium">{title}</h3>
        <p className="text-2xl font-bold text-gray-900 mt-1">{value}</p>
    </div>
);

export const Dashboard = () => {
    return (
        <div>
            <div className="mb-8">
                <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
                <p className="text-gray-500 mt-1">Overview of your enterprise performance.</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
                <StatCard
                    title="Total Revenue"
                    value="$124,500"
                    change="+12.5%"
                    trend="up"
                    icon={DollarSign}
                />
                <StatCard
                    title="Active Inventory"
                    value="1,240 Items"
                    change="-2.4%"
                    trend="down"
                    icon={Package}
                />
                <StatCard
                    title="OEE Efficiency"
                    value="84.2%"
                    change="+5.4%"
                    trend="up"
                    icon={Zap}
                />
                <StatCard
                    title="Quality Score"
                    value="98.4%"
                    change="+0.2%"
                    trend="up"
                    icon={ShieldCheck}
                />
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm">
                    <div className="flex items-center justify-between mb-6">
                        <h3 className="font-bold text-gray-900">Recent Transactions</h3>
                        <Link to="/finance" className="text-sm text-blue-600 hover:text-blue-700 font-medium">View All</Link>
                    </div>
                    <div className="space-y-4">
                        {[1, 2, 3].map(i => (
                            <div key={i} className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg transition-colors border border-gray-100">
                                <div className="flex items-center gap-3">
                                    <div className="w-10 h-10 rounded-full bg-gray-100 flex items-center justify-center text-gray-500 font-bold text-xs">
                                        INV
                                    </div>
                                    <div>
                                        <p className="font-medium text-gray-900">Invoice #{1000 + i}</p>
                                        <p className="text-xs text-gray-500">Today, 10:23 AM</p>
                                    </div>
                                </div>
                                <span className="font-bold text-gray-900">$1,200.00</span>
                            </div>
                        ))}
                    </div>
                </div>

                <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm">
                    <div className="flex items-center justify-between mb-6">
                        <h3 className="font-bold text-gray-900">Intelligence Briefing</h3>
                        <Link to="/analytics" className="text-sm text-blue-600 hover:text-blue-700 font-medium">Full Analytics</Link>
                    </div>
                    <div className="space-y-4">
                        <div className="p-4 bg-blue-50/50 border border-blue-100 rounded-lg">
                            <div className="flex items-center gap-2 text-blue-800 font-bold text-xs uppercase tracking-wider mb-2">
                                <Zap size={14} /> Automation Hub
                            </div>
                            <p className="text-sm text-blue-900 font-medium">1,429 events processed today with 84ms avg latency.</p>
                        </div>
                        <div className="p-4 bg-purple-50/50 border border-purple-100 rounded-lg">
                            <div className="flex items-center gap-2 text-purple-800 font-bold text-xs uppercase tracking-wider mb-2">
                                <Activity size={14} /> Demand Forecast
                            </div>
                            <p className="text-sm text-purple-900 font-medium">Next 30 days demand spike predicted for Steel Sheet (MAT-ST-002).</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};
