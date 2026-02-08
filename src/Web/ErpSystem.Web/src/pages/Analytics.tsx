import React from 'react';
import { BarChart3, TrendingUp, TrendingDown, Target, Clock, Zap, Timer } from 'lucide-react';

export const Analytics = () => {
    return (
        <div className="space-y-8">
            <div className="flex flex-col lg:flex-row lg:justify-between lg:items-end gap-6">
                <div>
                    <div className="flex items-center gap-2 mb-2">
                        <div className="w-2 h-8 bg-blue-600 rounded-full" />
                        <h1 className="text-3xl font-black text-gray-900 font-display tracking-tightest uppercase italic">Intelligence Hub</h1>
                    </div>
                    <p className="text-gray-500 font-display text-sm tracking-widest font-bold uppercase opacity-60">Predictive Modeling & Operational Efficiency</p>
                </div>
                <div className="flex p-1 bg-gray-100 rounded-xl border border-gray-200">
                    {['Real-time', 'Daily', 'Weekly', 'Quarterly'].map(tab => (
                        <button key={tab} className={`px-4 py-2 rounded-lg text-xs font-bold uppercase tracking-widest transition-all ${tab === 'Real-time' ? 'bg-white text-gray-900 shadow-sm' : 'text-gray-500 hover:text-gray-700'}`}>
                            {tab}
                        </button>
                    ))}
                </div>
            </div>

            {/* Main Intelligence Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <IntelligenceCard
                    title="Procurement Forecast"
                    value="↑ 124.5k"
                    trend="+12%"
                    isUp={true}
                    icon={Target}
                    color="blue"
                    description="Predicted demand for next 30 days"
                />
                <IntelligenceCard
                    title="Inventory Turnover"
                    value="4.2x"
                    trend="-2%"
                    isUp={false}
                    icon={Zap}
                    color="purple"
                    description="Annualized rotation frequency"
                />
                <IntelligenceCard
                    title="Operational OEE"
                    value="84.2%"
                    trend="+5.4%"
                    isUp={true}
                    icon={Timer}
                    color="indigo"
                    description="Aggregate equipment efficiency"
                />
                <IntelligenceCard
                    title="Cash Position (Pred.)"
                    value="$2.4M"
                    trend="+8%"
                    isUp={true}
                    icon={TrendingUp}
                    color="green"
                    description="Next 45 days liquidity forecast"
                />
            </div>

            {/* Detailed Charts Row */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Demand Prediction */}
                <div className="lg:col-span-2 bg-white p-8 rounded-[2rem] border border-gray-100 shadow-2xl shadow-blue-900/5">
                    <div className="flex justify-between items-start mb-8">
                        <div>
                            <h2 className="text-lg font-black text-gray-900 uppercase tracking-tighter">AI Demand Prediction</h2>
                            <p className="text-xs text-gray-400 font-bold uppercase tracking-widest mt-1">Cross-material requirements forecast</p>
                        </div>
                        <button className="text-xs bg-gray-50 text-gray-500 px-3 py-1.5 rounded-full font-bold uppercase tracking-widest hover:bg-blue-50 hover:text-blue-600 transition-all">Details</button>
                    </div>
                    {/* Visual Placeholder for Time Series */}
                    <div className="h-64 flex items-end gap-1 group">
                        {Array.from({ length: 30 }).map((_, i) => (
                            <div key={i} className="flex-1 flex flex-col items-center gap-2">
                                <div
                                    className={`w-full rounded-t-sm transition-all duration-500 ${i > 22 ? 'bg-blue-300 opacity-60' : 'bg-blue-600'}`}
                                    style={{ height: `${20 + Math.sin(i * 0.5) * 40 + Math.random() * 40}%` }}
                                />
                            </div>
                        ))}
                    </div>
                    <div className="mt-6 flex justify-between items-center bg-gray-50 p-4 rounded-2xl border border-gray-100">
                        <div className="flex items-center gap-3">
                            <div className="w-3 h-3 rounded-full bg-blue-600 shadow-lg shadow-blue-500/50" />
                            <span className="text-[10px] font-black text-gray-900 uppercase tracking-widest">Historical Data</span>
                        </div>
                        <div className="flex items-center gap-3">
                            <div className="w-3 h-3 rounded-full bg-blue-300 shadow-lg shadow-blue-500/20" />
                            <span className="text-[10px] font-black text-gray-400 uppercase tracking-widest italic">AI Prediction Cluster</span>
                        </div>
                        <div className="text-[10px] font-bold text-blue-600 uppercase tracking-widest bg-blue-100 px-3 py-1 rounded-full">92.4% Confidence</div>
                    </div>
                </div>

                {/* Efficiency breakdown */}
                <div className="bg-gray-900 p-8 rounded-[2rem] text-white shadow-2xl shadow-indigo-900/20 relative overflow-hidden group">
                    <div className="absolute top-0 right-0 w-64 h-64 bg-indigo-600/10 rounded-full -mr-32 -mt-32 blur-3xl transition-transform duration-1000 group-hover:scale-150" />
                    <div className="relative z-10">
                        <h2 className="text-lg font-black uppercase tracking-tighter italic mb-8">System OEE Analysis</h2>
                        <div className="space-y-6">
                            <EfficiencyBar label="Availability" percentage={88} color="indigo" />
                            <EfficiencyBar label="Performance" percentage={94} color="blue" />
                            <EfficiencyBar label="Quality Score" percentage={99} color="green" />
                        </div>
                        <div className="mt-12 pt-8 border-t border-white/5 flex flex-col items-center">
                            <div className="relative w-32 h-32 flex items-center justify-center">
                                <svg className="w-full h-full transform -rotate-90">
                                    <circle cx="64" cy="64" r="58" stroke="currentColor" strokeWidth="8" fill="transparent" className="text-white/5" />
                                    <circle cx="64" cy="64" r="58" stroke="currentColor" strokeWidth="8" fill="transparent" strokeDasharray={364} strokeDashoffset={364 * (1 - 0.84)} className="text-indigo-500 shadow-lg" strokeLinecap="round" />
                                </svg>
                                <div className="absolute inset-0 flex flex-col items-center justify-center">
                                    <span className="text-2xl font-black italic tracking-tighter">84.2</span>
                                    <span className="text-[10px] font-bold uppercase tracking-widest opacity-50">Global OEE</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            {/* Timescale Analytics Feed */}
            <div className="bg-white p-8 rounded-[2rem] border border-gray-100 shadow-sm">
                <div className="flex justify-between items-center mb-6">
                    <h2 className="text-lg font-black text-gray-900 uppercase tracking-tightest italic">Anomaly Detection Feed</h2>
                    <span className="text-[10px] font-bold text-gray-400 uppercase tracking-widest">Driven by TimescaleDB Hypertables</span>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {[
                        { title: "Inventory Spike", time: "14:22", type: "Forecast Error", severity: "Low", msg: "Aluminum stock levels deviating from 7-day pattern" },
                        { title: "Equipment Heatup", time: "12:05", type: "Maint Trigger", severity: "High", msg: "EQUIP-CNC-01 showing abnormal thermal profile" },
                        { title: "Usage Outlier", time: "09:41", type: "Cost Variance", severity: "Medium", msg: "Material MAT-ST-002 price surged 12.4% in last 12h" }
                    ].map((item, i) => (
                        <div key={i} className="p-5 bg-gray-50 rounded-2xl border border-gray-100 border-l-4 border-l-indigo-600 hover:scale-[1.02] transition-transform cursor-pointer">
                            <div className="flex justify-between items-start mb-2">
                                <h3 className="text-xs font-black text-gray-900 uppercase tracking-tighter">{item.title}</h3>
                                <span className="text-[10px] font-mono text-gray-400">{item.time}</span>
                            </div>
                            <p className="text-[10px] text-gray-500 mb-3">{item.msg}</p>
                            <div className="flex justify-between items-center">
                                <span className="text-[8px] font-black uppercase tracking-widest text-indigo-500 bg-indigo-50 px-2 py-0.5 rounded italic">{item.type}</span>
                                <span className={`text-[8px] font-black uppercase tracking-widest px-2 py-0.5 rounded ${item.severity === 'High' ? 'bg-red-50 text-red-600' : 'bg-gray-200 text-gray-600'}`}>{item.severity} Risk</span>
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
};

const IntelligenceCard = ({ title, value, trend, isUp, icon: Icon, color, description }: any) => (
    <div className="bg-white p-6 rounded-[2rem] border border-gray-100 shadow-lg shadow-gray-200/20 hover:shadow-xl transition-all cursor-pointer group">
        <div className="flex justify-between items-start mb-4">
            <div className={`w-12 h-12 bg-${color}-50 rounded-2xl flex items-center justify-center text-${color}-600 group-hover:scale-110 transition-transform`}>
                <Icon size={24} />
            </div>
            <div className={`flex items-center gap-0.5 text-xs font-black italic tracking-tightest ${isUp ? 'text-green-600' : 'text-red-600'}`}>
                {isUp ? <TrendingUp size={12} /> : <TrendingDown size={12} />} {trend}
            </div>
        </div>
        <p className="text-gray-400 text-[10px] font-bold uppercase tracking-widest font-display mb-1">{title}</p>
        <p className="text-3xl font-black text-gray-900 tracking-tighter italic font-display">{value}</p>
        <p className="text-[10px] text-gray-400 mt-4 leading-tight font-medium uppercase italic opacity-50">{description}</p>
    </div>
);

const EfficiencyBar = ({ label, percentage, color }: any) => (
    <div>
        <div className="flex justify-between items-center text-[10px] font-bold uppercase tracking-[0.2em] mb-2 text-white/60">
            <span>{label}</span>
            <span className={`text-${color}-400`}>{percentage}%</span>
        </div>
        <div className="h-2 bg-white/5 rounded-full overflow-hidden">
            <div className={`h-full bg-${color}-500 rounded-full transition-all duration-1000`} style={{ width: `${percentage}%` }} />
        </div>
    </div>
);
