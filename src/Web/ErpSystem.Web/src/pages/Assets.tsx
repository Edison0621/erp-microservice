import { useState, useEffect } from 'react';
import { api } from '../services/api';
import {
    Building2,
    Laptop,
    Car,
    Wrench,
    TrendingDown,
    MapPin,
    Calendar,
    DollarSign,
    Plus,
    ChevronRight,
    AlertCircle,
    CheckCircle2,
    XCircle,
    Settings,
    History,
} from 'lucide-react';

// Types
interface Asset {
    id: string;
    assetNumber: string;
    name: string;
    description?: string;
    type: string;
    status: string;
    acquisitionCost: number;
    acquisitionDate: string;
    currentValue: number;
    accumulatedDepreciation: number;
    bookValue: number;
    locationId: string;
    maintenanceCount: number;
    totalMaintenanceCost: number;
    createdAt: string;
}

interface Maintenance {
    id: string;
    assetId: string;
    assetNumber: string;
    assetName: string;
    type: string;
    description: string;
    maintenanceDate: string;
    cost: number;
    performedBy?: string;
}

// Type icons
const TypeIcon = ({ type }: { type: string }) => {
    switch (type) {
        case 'Equipment': return <Settings className="w-5 h-5 text-blue-400" />;
        case 'Vehicle': return <Car className="w-5 h-5 text-purple-400" />;
        case 'IT': return <Laptop className="w-5 h-5 text-cyan-400" />;
        case 'Building': return <Building2 className="w-5 h-5 text-amber-400" />;
        default: return <Building2 className="w-5 h-5 text-slate-400" />;
    }
};

// Status badge
const StatusBadge = ({ status }: { status: string }) => {
    const getStyle = () => {
        switch (status) {
            case 'Draft': return 'bg-slate-500/20 text-slate-400 border border-slate-500/30';
            case 'Active': return 'bg-green-500/20 text-green-400 border border-green-500/30';
            case 'InMaintenance': return 'bg-amber-500/20 text-amber-400 border border-amber-500/30';
            case 'Disposed': return 'bg-red-500/20 text-red-400 border border-red-500/30';
            default: return 'bg-slate-500/20 text-slate-400 border border-slate-500/30';
        }
    };
    return <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${getStyle()}`}>{status}</span>;
};

// Stat card
const StatCard = ({ icon: Icon, label, value, subValue, color = 'blue' }: {
    icon: React.ElementType; label: string; value: string | number; subValue?: string; color?: 'blue' | 'green' | 'amber' | 'purple';
}) => {
    const colorClasses = {
        blue: 'from-blue-500/20 to-cyan-500/20 border-blue-500/30',
        green: 'from-green-500/20 to-emerald-500/20 border-green-500/30',
        amber: 'from-amber-500/20 to-orange-500/20 border-amber-500/30',
        purple: 'from-purple-500/20 to-pink-500/20 border-purple-500/30',
    };
    const iconColors = { blue: 'text-blue-400', green: 'text-green-400', amber: 'text-amber-400', purple: 'text-purple-400' };

    return (
        <div className={`bg-gradient-to-br ${colorClasses[color]} border rounded-xl p-4`}>
            <div className="flex items-center justify-between"><Icon className={`w-5 h-5 ${iconColors[color]}`} /></div>
            <div className="mt-3">
                <div className="text-2xl font-bold text-white">{value}</div>
                <div className="text-sm text-slate-400">{label}</div>
                {subValue && <div className="text-xs text-slate-500 mt-1">{subValue}</div>}
            </div>
        </div>
    );
};

// Mock data
const mockAssets: Asset[] = [
    { id: 'a1', assetNumber: 'AST-20260101-ABC123', name: 'Dell PowerEdge R750', description: 'Production server', type: 'IT', status: 'Active', acquisitionCost: 85000, acquisitionDate: '2026-01-15', currentValue: 72000, accumulatedDepreciation: 13000, bookValue: 72000, locationId: 'DC-01', maintenanceCount: 1, totalMaintenanceCost: 2500, createdAt: '2026-01-15' },
    { id: 'a2', assetNumber: 'AST-20260102-DEF456', name: 'Toyota Hiace Van', description: 'Delivery vehicle', type: 'Vehicle', status: 'Active', acquisitionCost: 320000, acquisitionDate: '2026-01-10', currentValue: 295000, accumulatedDepreciation: 25000, bookValue: 295000, locationId: 'GARAGE-01', maintenanceCount: 2, totalMaintenanceCost: 4800, createdAt: '2026-01-10' },
    { id: 'a3', assetNumber: 'AST-20260103-GHI789', name: 'CNC Milling Machine', description: 'Production equipment', type: 'Equipment', status: 'InMaintenance', acquisitionCost: 580000, acquisitionDate: '2025-06-20', currentValue: 490000, accumulatedDepreciation: 90000, bookValue: 490000, locationId: 'FACTORY-A', maintenanceCount: 5, totalMaintenanceCost: 28000, createdAt: '2025-06-20' },
    { id: 'a4', assetNumber: 'AST-20260104-JKL012', name: 'Office Building Wing B', description: 'Office space', type: 'Building', status: 'Active', acquisitionCost: 5800000, acquisitionDate: '2020-03-15', currentValue: 5200000, accumulatedDepreciation: 600000, bookValue: 5200000, locationId: 'CAMPUS-01', maintenanceCount: 12, totalMaintenanceCost: 180000, createdAt: '2020-03-15' },
];

const mockMaintenance: Maintenance[] = [
    { id: 'm1', assetId: 'a3', assetNumber: 'AST-20260103-GHI789', assetName: 'CNC Milling Machine', type: 'Corrective', description: 'Spindle bearing replacement', maintenanceDate: '2026-02-05', cost: 12000, performedBy: 'TechServe Inc.' },
    { id: 'm2', assetId: 'a2', assetNumber: 'AST-20260102-DEF456', assetName: 'Toyota Hiace Van', type: 'Preventive', description: 'Regular service - Oil change, brake check', maintenanceDate: '2026-02-01', cost: 2800, performedBy: 'Toyota Service Center' },
    { id: 'm3', assetId: 'a1', assetNumber: 'AST-20260101-ABC123', assetName: 'Dell PowerEdge R750', type: 'Preventive', description: 'Firmware update and diagnostics', maintenanceDate: '2026-01-25', cost: 2500, performedBy: 'Dell Support' },
];

export default function Assets() {
    const [activeTab, setActiveTab] = useState<'assets' | 'maintenance' | 'depreciation'>('assets');
    const [assets, setAssets] = useState<Asset[]>(mockAssets);
    const [maintenance, setMaintenance] = useState<Maintenance[]>(mockMaintenance);
    const [typeFilter, setTypeFilter] = useState<string>('');

    const stats = {
        totalAssets: assets.length,
        totalValue: assets.reduce((sum, a) => sum + a.bookValue, 0),
        totalDepreciation: assets.reduce((sum, a) => sum + a.accumulatedDepreciation, 0),
        inMaintenance: assets.filter(a => a.status === 'InMaintenance').length
    };

    const filteredAssets = typeFilter ? assets.filter(a => a.type === typeFilter) : assets;

    useEffect(() => {
        const fetchData = async () => {
            try {
                const [assetsRes, maintRes] = await Promise.all([
                    api.get('/assets'),
                    api.get('/assets/maintenance')
                ]);
                if (assetsRes.data?.items) setAssets(assetsRes.data.items);
                if (maintRes.data?.items) setMaintenance(maintRes.data.items);
            } catch (error) {
                console.log('Using mock data');
            }
        };
        fetchData();
    }, []);

    return (
        <div className="p-6 space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-white flex items-center gap-3">
                        <Building2 className="w-7 h-7 text-amber-400" />
                        Asset Management
                    </h1>
                    <p className="text-slate-400 mt-1">Track fixed assets, maintenance, and depreciation</p>
                </div>
                <button className="flex items-center gap-2 px-4 py-2 bg-amber-600 hover:bg-amber-500 text-white rounded-lg transition-colors">
                    <Plus className="w-4 h-4" /> Register Asset
                </button>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
                <StatCard icon={Building2} label="Total Assets" value={stats.totalAssets} color="blue" />
                <StatCard icon={DollarSign} label="Total Book Value" value={`¥${(stats.totalValue / 10000).toFixed(0)}万`} color="green" />
                <StatCard icon={TrendingDown} label="Accumulated Depreciation" value={`¥${(stats.totalDepreciation / 10000).toFixed(0)}万`} color="amber" />
                <StatCard icon={Wrench} label="In Maintenance" value={stats.inMaintenance} color="purple" />
            </div>

            {/* Tabs */}
            <div className="border-b border-slate-700">
                <nav className="flex gap-6">
                    {(['assets', 'maintenance', 'depreciation'] as const).map((tab) => (
                        <button
                            key={tab}
                            onClick={() => setActiveTab(tab)}
                            className={`pb-3 px-1 border-b-2 transition-colors capitalize ${activeTab === tab ? 'border-amber-500 text-white' : 'border-transparent text-slate-400 hover:text-slate-300'
                                }`}
                        >
                            {tab === 'assets' && <Building2 className="w-4 h-4 inline mr-2" />}
                            {tab === 'maintenance' && <Wrench className="w-4 h-4 inline mr-2" />}
                            {tab === 'depreciation' && <TrendingDown className="w-4 h-4 inline mr-2" />}
                            {tab}
                        </button>
                    ))}
                </nav>
            </div>

            {/* Assets Tab */}
            {activeTab === 'assets' && (
                <div className="space-y-4">
                    {/* Type filters */}
                    <div className="flex gap-2">
                        {['', 'IT', 'Equipment', 'Vehicle', 'Building'].map((type) => (
                            <button
                                key={type}
                                onClick={() => setTypeFilter(type)}
                                className={`px-3 py-1.5 text-sm rounded-lg transition-colors ${typeFilter === type ? 'bg-amber-600 text-white' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
                                    }`}
                            >
                                {type || 'All'}
                            </button>
                        ))}
                    </div>

                    {/* Asset list */}
                    <div className="bg-slate-800/50 border border-slate-700/50 rounded-xl overflow-hidden">
                        <table className="w-full">
                            <thead className="bg-slate-900/50 border-b border-slate-700">
                                <tr>
                                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Asset</th>
                                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Type</th>
                                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Location</th>
                                    <th className="px-4 py-3 text-right text-xs font-medium text-slate-400 uppercase">Acquisition</th>
                                    <th className="px-4 py-3 text-right text-xs font-medium text-slate-400 uppercase">Book Value</th>
                                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Status</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-700/50">
                                {filteredAssets.map((asset) => (
                                    <tr key={asset.id} className="hover:bg-slate-700/30 transition-colors cursor-pointer">
                                        <td className="px-4 py-3">
                                            <div className="flex items-center gap-3">
                                                <TypeIcon type={asset.type} />
                                                <div>
                                                    <div className="text-white font-medium">{asset.name}</div>
                                                    <div className="text-xs text-slate-500 font-mono">{asset.assetNumber}</div>
                                                </div>
                                            </div>
                                        </td>
                                        <td className="px-4 py-3 text-slate-400">{asset.type}</td>
                                        <td className="px-4 py-3"><span className="flex items-center gap-1 text-slate-400"><MapPin className="w-3 h-3" /> {asset.locationId}</span></td>
                                        <td className="px-4 py-3 text-right text-white">¥{asset.acquisitionCost.toLocaleString()}</td>
                                        <td className="px-4 py-3 text-right">
                                            <div className="text-amber-400 font-medium">¥{asset.bookValue.toLocaleString()}</div>
                                            <div className="text-xs text-slate-500">-¥{asset.accumulatedDepreciation.toLocaleString()}</div>
                                        </td>
                                        <td className="px-4 py-3"><StatusBadge status={asset.status} /></td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}

            {/* Maintenance Tab */}
            {activeTab === 'maintenance' && (
                <div className="space-y-4">
                    {maintenance.map((m) => (
                        <div key={m.id} className="bg-slate-800/50 border border-slate-700/50 rounded-xl p-4 hover:border-amber-500/50 transition-all">
                            <div className="flex items-center justify-between">
                                <div className="flex items-center gap-4">
                                    <div className={`p-2 rounded-lg ${m.type === 'Corrective' ? 'bg-red-500/20' : 'bg-green-500/20'}`}>
                                        <Wrench className={`w-5 h-5 ${m.type === 'Corrective' ? 'text-red-400' : 'text-green-400'}`} />
                                    </div>
                                    <div>
                                        <div className="text-white font-medium">{m.assetName}</div>
                                        <div className="text-sm text-slate-400">{m.description}</div>
                                        <div className="text-xs text-slate-500 mt-1 flex items-center gap-4">
                                            <span><Calendar className="w-3 h-3 inline mr-1" />{new Date(m.maintenanceDate).toLocaleDateString()}</span>
                                            {m.performedBy && <span>By: {m.performedBy}</span>}
                                        </div>
                                    </div>
                                </div>
                                <div className="text-right">
                                    <div className="text-lg font-bold text-amber-400">¥{m.cost.toLocaleString()}</div>
                                    <span className={`text-xs px-2 py-0.5 rounded ${m.type === 'Corrective' ? 'bg-red-500/20 text-red-400' : 'bg-green-500/20 text-green-400'}`}>
                                        {m.type}
                                    </span>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Depreciation Tab */}
            {activeTab === 'depreciation' && (
                <div className="space-y-4">
                    <div className="bg-slate-800/50 border border-slate-700/50 rounded-xl p-6">
                        <h3 className="text-lg font-semibold text-white mb-4 flex items-center gap-2">
                            <TrendingDown className="w-5 h-5 text-amber-400" />
                            Depreciation Summary
                        </h3>
                        <div className="grid grid-cols-3 gap-6">
                            <div className="text-center p-4 bg-slate-900/50 rounded-lg">
                                <div className="text-3xl font-bold text-white">¥{(stats.totalValue / 10000).toFixed(0)}万</div>
                                <div className="text-sm text-slate-400 mt-1">Total Book Value</div>
                            </div>
                            <div className="text-center p-4 bg-slate-900/50 rounded-lg">
                                <div className="text-3xl font-bold text-amber-400">¥{(stats.totalDepreciation / 10000).toFixed(0)}万</div>
                                <div className="text-sm text-slate-400 mt-1">Accumulated Depreciation</div>
                            </div>
                            <div className="text-center p-4 bg-slate-900/50 rounded-lg">
                                <div className="text-3xl font-bold text-slate-300">¥{((stats.totalValue + stats.totalDepreciation) / 10000).toFixed(0)}万</div>
                                <div className="text-sm text-slate-400 mt-1">Original Cost</div>
                            </div>
                        </div>
                        <div className="mt-6">
                            <button className="flex items-center gap-2 px-4 py-2 bg-amber-600 hover:bg-amber-500 text-white rounded-lg transition-colors">
                                <History className="w-4 h-4" /> Run Monthly Depreciation
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
