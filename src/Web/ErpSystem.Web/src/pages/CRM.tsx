import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { 
    Plus, Users, Target, Megaphone, Search, Filter, 
    Phone, Mail, Building2, TrendingUp, DollarSign,
    CheckCircle2, XCircle, Clock, ArrowRight, MoreVertical,
    ChevronDown, Calendar, Star, Zap
} from 'lucide-react';

// ============== Types ==============

interface Lead {
    id: string;
    leadNumber: string;
    contact: string;
    status: string;
    source: string;
    score: number;
    assignedToUserId: string | null;
    createdAt: string;
    lastContactedAt: string | null;
}

interface Opportunity {
    id: string;
    opportunityNumber: string;
    name: string;
    customerName: string | null;
    estimatedValue: number;
    weightedValue: number;
    stage: string;
    priority: string;
    winProbability: number;
    expectedCloseDate: string;
    createdAt: string;
}

interface Campaign {
    id: string;
    campaignNumber: string;
    name: string;
    type: string;
    status: string;
    budget: number;
    totalExpenses: number;
    totalLeads: number;
    convertedLeads: number;
    roi: number;
    startDate: string;
    endDate: string;
}

interface PipelineStage {
    stage: string;
    count: number;
    totalValue: number;
    weightedValue: number;
}

// ============== Helper Components ==============

const StatusBadge = ({ status, type }: { status: string; type: 'lead' | 'opportunity' | 'campaign' }) => {
    const getColors = () => {
        if (type === 'lead') {
            switch (status) {
                case 'New': return 'bg-blue-100 text-blue-800';
                case 'Contacted': return 'bg-yellow-100 text-yellow-800';
                case 'Qualified': return 'bg-green-100 text-green-800';
                case 'Converted': return 'bg-purple-100 text-purple-800';
                case 'Lost': return 'bg-red-100 text-red-800';
                default: return 'bg-gray-100 text-gray-800';
            }
        }
        if (type === 'opportunity') {
            switch (status) {
                case 'Prospecting': return 'bg-gray-100 text-gray-800';
                case 'Qualification': return 'bg-blue-100 text-blue-800';
                case 'NeedsAnalysis': return 'bg-yellow-100 text-yellow-800';
                case 'ValueProposition': return 'bg-orange-100 text-orange-800';
                case 'Negotiation': return 'bg-purple-100 text-purple-800';
                case 'ClosedWon': return 'bg-green-100 text-green-800';
                case 'ClosedLost': return 'bg-red-100 text-red-800';
                default: return 'bg-gray-100 text-gray-800';
            }
        }
        // Campaign
        switch (status) {
            case 'Draft': return 'bg-gray-100 text-gray-800';
            case 'Scheduled': return 'bg-blue-100 text-blue-800';
            case 'Active': return 'bg-green-100 text-green-800';
            case 'Paused': return 'bg-yellow-100 text-yellow-800';
            case 'Completed': return 'bg-purple-100 text-purple-800';
            case 'Cancelled': return 'bg-red-100 text-red-800';
            default: return 'bg-gray-100 text-gray-800';
        }
    };

    return (
        <span className={`px-2.5 py-1 rounded-full text-xs font-semibold ${getColors()}`}>
            {status}
        </span>
    );
};

const ScoreBadge = ({ score }: { score: number }) => {
    const getColor = () => {
        if (score >= 80) return 'bg-green-500';
        if (score >= 60) return 'bg-yellow-500';
        if (score >= 40) return 'bg-orange-500';
        return 'bg-gray-400';
    };

    return (
        <div className="flex items-center gap-2">
            <div className="w-16 h-2 bg-gray-200 rounded-full overflow-hidden">
                <div className={`h-full ${getColor()} rounded-full`} style={{ width: `${score}%` }} />
            </div>
            <span className="text-sm font-medium text-gray-700">{score}</span>
        </div>
    );
};

const StatCard = ({ icon: Icon, label, value, subValue, color }: { 
    icon: any; label: string; value: string | number; subValue?: string; color: string 
}) => (
    <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm hover:shadow-md transition-shadow">
        <div className="flex items-start justify-between">
            <div>
                <p className="text-sm text-gray-500 mb-1">{label}</p>
                <p className="text-2xl font-bold text-gray-900">{value}</p>
                {subValue && <p className="text-xs text-gray-400 mt-1">{subValue}</p>}
            </div>
            <div className={`w-10 h-10 rounded-lg ${color} flex items-center justify-center`}>
                <Icon size={20} className="text-white" />
            </div>
        </div>
    </div>
);

// ============== Leads Tab ==============

const LeadsTab = () => {
    const [leads, setLeads] = useState<Lead[]>([]);
    const [loading, setLoading] = useState(false);
    const [statusFilter, setStatusFilter] = useState('all');

    useEffect(() => {
        fetchLeads();
    }, [statusFilter]);

    const fetchLeads = async () => {
        setLoading(true);
        try {
            const params = statusFilter !== 'all' ? `?status=${statusFilter}` : '';
            const { data } = await api.get(`/crm/leads${params}`);
            setLeads(data.items || data);
        } catch (error) {
            console.error("Failed to fetch leads", error);
            // Mock data for demo
            setLeads([
                { id: '1', leadNumber: 'LD-20260208-ABC12345', contact: '{"firstName":"张","lastName":"伟","email":"zhang.wei@example.com"}', status: 'New', source: 'Website', score: 0, assignedToUserId: null, createdAt: '2026-02-08T10:00:00Z', lastContactedAt: null },
                { id: '2', leadNumber: 'LD-20260208-DEF67890', contact: '{"firstName":"李","lastName":"娜","email":"li.na@corp.com"}', status: 'Contacted', source: 'Referral', score: 45, assignedToUserId: 'user1', createdAt: '2026-02-07T14:30:00Z', lastContactedAt: '2026-02-08T09:00:00Z' },
                { id: '3', leadNumber: 'LD-20260207-GHI11111', contact: '{"firstName":"王","lastName":"强","email":"wang.qiang@tech.cn"}', status: 'Qualified', source: 'TradeShow', score: 85, assignedToUserId: 'user2', createdAt: '2026-02-06T08:00:00Z', lastContactedAt: '2026-02-08T11:00:00Z' },
                { id: '4', leadNumber: 'LD-20260206-JKL22222', contact: '{"firstName":"刘","lastName":"芳","email":"liu.fang@startup.io"}', status: 'Converted', source: 'EmailCampaign', score: 92, assignedToUserId: 'user1', createdAt: '2026-02-05T16:00:00Z', lastContactedAt: '2026-02-07T15:00:00Z' },
            ]);
        } finally {
            setLoading(false);
        }
    };

    const parseContact = (contactJson: string) => {
        try {
            const c = JSON.parse(contactJson);
            return { name: `${c.firstName} ${c.lastName}`, email: c.email };
        } catch {
            return { name: 'Unknown', email: '' };
        }
    };

    const stats = {
        total: leads.length,
        new: leads.filter(l => l.status === 'New').length,
        qualified: leads.filter(l => l.status === 'Qualified').length,
        converted: leads.filter(l => l.status === 'Converted').length,
    };

    return (
        <div className="space-y-6">
            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
                <StatCard icon={Users} label="Total Leads" value={stats.total} color="bg-blue-500" />
                <StatCard icon={Zap} label="New Leads" value={stats.new} subValue="Pending contact" color="bg-yellow-500" />
                <StatCard icon={Star} label="Qualified" value={stats.qualified} subValue="Ready to convert" color="bg-green-500" />
                <StatCard icon={CheckCircle2} label="Converted" value={stats.converted} subValue="Won opportunities" color="bg-purple-500" />
            </div>

            {/* Filters & Actions */}
            <div className="flex justify-between items-center">
                <div className="flex gap-3">
                    <div className="relative">
                        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                        <input 
                            type="text" 
                            placeholder="Search leads..." 
                            className="pl-9 pr-4 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                    </div>
                    <select 
                        value={statusFilter} 
                        onChange={(e) => setStatusFilter(e.target.value)}
                        className="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                        <option value="all">All Status</option>
                        <option value="New">New</option>
                        <option value="Contacted">Contacted</option>
                        <option value="Qualified">Qualified</option>
                        <option value="Converted">Converted</option>
                        <option value="Lost">Lost</option>
                    </select>
                </div>
                <button className="btn btn-primary flex items-center gap-2">
                    <Plus size={16} /> Add Lead
                </button>
            </div>

            {/* Table */}
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
                <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                        <tr>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Lead</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Source</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Score</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Created</th>
                            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Actions</th>
                        </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                        {leads.map(lead => {
                            const contact = parseContact(lead.contact);
                            return (
                                <tr key={lead.id} className="hover:bg-gray-50 transition-colors">
                                    <td className="px-6 py-4">
                                        <div className="flex items-center gap-3">
                                            <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-400 to-blue-600 flex items-center justify-center text-white font-medium">
                                                {contact.name.charAt(0)}
                                            </div>
                                            <div>
                                                <p className="font-medium text-gray-900">{contact.name}</p>
                                                <p className="text-sm text-gray-500">{contact.email}</p>
                                            </div>
                                        </div>
                                    </td>
                                    <td className="px-6 py-4 text-sm text-gray-600">{lead.source}</td>
                                    <td className="px-6 py-4"><StatusBadge status={lead.status} type="lead" /></td>
                                    <td className="px-6 py-4"><ScoreBadge score={lead.score} /></td>
                                    <td className="px-6 py-4 text-sm text-gray-500">{new Date(lead.createdAt).toLocaleDateString()}</td>
                                    <td className="px-6 py-4 text-right">
                                        <button className="text-blue-600 hover:text-blue-800 text-sm font-medium">View</button>
                                    </td>
                                </tr>
                            );
                        })}
                    </tbody>
                </table>
            </div>
        </div>
    );
};

// ============== Opportunities Tab ==============

const OpportunitiesTab = () => {
    const [opportunities, setOpportunities] = useState<Opportunity[]>([]);
    const [pipeline, setPipeline] = useState<PipelineStage[]>([]);
    const [viewMode, setViewMode] = useState<'list' | 'pipeline'>('pipeline');
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        setLoading(true);
        try {
            const [oppsRes, pipelineRes] = await Promise.all([
                api.get('/crm/opportunities'),
                api.get('/crm/opportunities/pipeline')
            ]);
            setOpportunities(oppsRes.data.items || oppsRes.data);
            setPipeline(pipelineRes.data.stages || []);
        } catch (error) {
            console.error("Failed to fetch opportunities", error);
            // Mock data
            setOpportunities([
                { id: '1', opportunityNumber: 'OPP-20260208-AAA', name: 'Enterprise Software Deal', customerName: 'Tech Corp', estimatedValue: 150000, weightedValue: 15000, stage: 'Prospecting', priority: 'High', winProbability: 10, expectedCloseDate: '2026-03-15', createdAt: '2026-02-08' },
                { id: '2', opportunityNumber: 'OPP-20260207-BBB', name: 'Cloud Migration Project', customerName: 'Retail Inc', estimatedValue: 80000, weightedValue: 32000, stage: 'NeedsAnalysis', priority: 'Medium', winProbability: 40, expectedCloseDate: '2026-02-28', createdAt: '2026-02-07' },
                { id: '3', opportunityNumber: 'OPP-20260205-CCC', name: 'Annual Support Contract', customerName: 'Finance Ltd', estimatedValue: 45000, weightedValue: 36000, stage: 'Negotiation', priority: 'High', winProbability: 80, expectedCloseDate: '2026-02-20', createdAt: '2026-02-05' },
            ]);
            setPipeline([
                { stage: 'Prospecting', count: 5, totalValue: 250000, weightedValue: 25000 },
                { stage: 'Qualification', count: 3, totalValue: 180000, weightedValue: 36000 },
                { stage: 'NeedsAnalysis', count: 4, totalValue: 320000, weightedValue: 128000 },
                { stage: 'ValueProposition', count: 2, totalValue: 150000, weightedValue: 90000 },
                { stage: 'Negotiation', count: 2, totalValue: 95000, weightedValue: 76000 },
            ]);
        } finally {
            setLoading(false);
        }
    };

    const totalPipeline = pipeline.reduce((sum, s) => sum + s.totalValue, 0);
    const totalWeighted = pipeline.reduce((sum, s) => sum + s.weightedValue, 0);

    const stageColors: Record<string, string> = {
        'Prospecting': 'from-gray-400 to-gray-500',
        'Qualification': 'from-blue-400 to-blue-500',
        'NeedsAnalysis': 'from-yellow-400 to-yellow-500',
        'ValueProposition': 'from-orange-400 to-orange-500',
        'Negotiation': 'from-purple-400 to-purple-500',
    };

    return (
        <div className="space-y-6">
            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
                <StatCard icon={Target} label="Open Opportunities" value={opportunities.length} color="bg-blue-500" />
                <StatCard icon={DollarSign} label="Pipeline Value" value={`¥${(totalPipeline / 1000).toFixed(0)}K`} color="bg-green-500" />
                <StatCard icon={TrendingUp} label="Weighted Value" value={`¥${(totalWeighted / 1000).toFixed(0)}K`} subValue="Probability adjusted" color="bg-purple-500" />
                <StatCard icon={Calendar} label="Closing This Month" value={opportunities.filter(o => new Date(o.expectedCloseDate).getMonth() === new Date().getMonth()).length} color="bg-orange-500" />
            </div>

            {/* View Toggle & Actions */}
            <div className="flex justify-between items-center">
                <div className="flex gap-2 bg-gray-100 p-1 rounded-lg">
                    <button 
                        onClick={() => setViewMode('pipeline')}
                        className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${viewMode === 'pipeline' ? 'bg-white text-gray-900 shadow-sm' : 'text-gray-600'}`}
                    >
                        Pipeline View
                    </button>
                    <button 
                        onClick={() => setViewMode('list')}
                        className={`px-4 py-2 text-sm font-medium rounded-md transition-colors ${viewMode === 'list' ? 'bg-white text-gray-900 shadow-sm' : 'text-gray-600'}`}
                    >
                        List View
                    </button>
                </div>
                <button className="btn btn-primary flex items-center gap-2">
                    <Plus size={16} /> New Opportunity
                </button>
            </div>

            {/* Pipeline View */}
            {viewMode === 'pipeline' && (
                <div className="grid grid-cols-5 gap-4">
                    {pipeline.map(stage => (
                        <div key={stage.stage} className="bg-white rounded-xl border border-gray-200 overflow-hidden">
                            <div className={`h-1 bg-gradient-to-r ${stageColors[stage.stage] || 'from-gray-400 to-gray-500'}`} />
                            <div className="p-4">
                                <div className="flex items-center justify-between mb-3">
                                    <h3 className="font-semibold text-gray-900 text-sm">{stage.stage.replace(/([A-Z])/g, ' $1').trim()}</h3>
                                    <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded-full">{stage.count}</span>
                                </div>
                                <p className="text-lg font-bold text-gray-900">¥{(stage.totalValue / 1000).toFixed(0)}K</p>
                                <p className="text-xs text-gray-500">Weighted: ¥{(stage.weightedValue / 1000).toFixed(0)}K</p>
                            </div>
                            <div className="px-4 pb-4 space-y-2">
                                {opportunities.filter(o => o.stage === stage.stage).slice(0, 3).map(opp => (
                                    <div key={opp.id} className="bg-gray-50 rounded-lg p-3 hover:bg-gray-100 cursor-pointer transition-colors">
                                        <p className="font-medium text-sm text-gray-900 truncate">{opp.name}</p>
                                        <p className="text-xs text-gray-500">{opp.customerName}</p>
                                        <p className="text-sm font-semibold text-green-600 mt-1">¥{opp.estimatedValue.toLocaleString()}</p>
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* List View */}
            {viewMode === 'list' && (
                <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Opportunity</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Customer</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Value</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Stage</th>
                                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase">Win %</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Close Date</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Actions</th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {opportunities.map(opp => (
                                <tr key={opp.id} className="hover:bg-gray-50 transition-colors">
                                    <td className="px-6 py-4">
                                        <p className="font-medium text-gray-900">{opp.name}</p>
                                        <p className="text-xs text-gray-500">{opp.opportunityNumber}</p>
                                    </td>
                                    <td className="px-6 py-4 text-sm text-gray-600">{opp.customerName || '-'}</td>
                                    <td className="px-6 py-4 text-right">
                                        <p className="font-semibold text-gray-900">¥{opp.estimatedValue.toLocaleString()}</p>
                                        <p className="text-xs text-gray-500">Weighted: ¥{opp.weightedValue.toLocaleString()}</p>
                                    </td>
                                    <td className="px-6 py-4"><StatusBadge status={opp.stage} type="opportunity" /></td>
                                    <td className="px-6 py-4 text-center">
                                        <span className={`font-semibold ${opp.winProbability >= 60 ? 'text-green-600' : opp.winProbability >= 30 ? 'text-yellow-600' : 'text-gray-500'}`}>
                                            {opp.winProbability}%
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 text-sm text-gray-500">{new Date(opp.expectedCloseDate).toLocaleDateString()}</td>
                                    <td className="px-6 py-4 text-right">
                                        <button className="text-blue-600 hover:text-blue-800 text-sm font-medium">View</button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
};

// ============== Campaigns Tab ==============

const CampaignsTab = () => {
    const [campaigns, setCampaigns] = useState<Campaign[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        fetchCampaigns();
    }, []);

    const fetchCampaigns = async () => {
        setLoading(true);
        try {
            const { data } = await api.get('/crm/campaigns');
            setCampaigns(data.items || data);
        } catch (error) {
            console.error("Failed to fetch campaigns", error);
            // Mock data
            setCampaigns([
                { id: '1', campaignNumber: 'CMP-20260201-AAA', name: 'Spring Product Launch', type: 'Email', status: 'Active', budget: 50000, totalExpenses: 35000, totalLeads: 245, convertedLeads: 32, roi: 85, startDate: '2026-02-01', endDate: '2026-03-31' },
                { id: '2', campaignNumber: 'CMP-20260115-BBB', name: 'Tech Conference 2026', type: 'TradeShow', status: 'Completed', budget: 80000, totalExpenses: 78000, totalLeads: 180, convertedLeads: 45, roi: 120, startDate: '2026-01-15', endDate: '2026-01-20' },
                { id: '3', campaignNumber: 'CMP-20260210-CCC', name: 'Social Media Awareness', type: 'SocialMedia', status: 'Draft', budget: 25000, totalExpenses: 0, totalLeads: 0, convertedLeads: 0, roi: 0, startDate: '2026-02-15', endDate: '2026-04-15' },
            ]);
        } finally {
            setLoading(false);
        }
    };

    const totalBudget = campaigns.reduce((sum, c) => sum + c.budget, 0);
    const totalExpenses = campaigns.reduce((sum, c) => sum + c.totalExpenses, 0);
    const totalLeads = campaigns.reduce((sum, c) => sum + c.totalLeads, 0);
    const avgROI = campaigns.filter(c => c.status === 'Completed' || c.status === 'Active').reduce((sum, c) => sum + c.roi, 0) / Math.max(campaigns.filter(c => c.status === 'Completed' || c.status === 'Active').length, 1);

    return (
        <div className="space-y-6">
            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
                <StatCard icon={Megaphone} label="Active Campaigns" value={campaigns.filter(c => c.status === 'Active').length} color="bg-blue-500" />
                <StatCard icon={DollarSign} label="Total Budget" value={`¥${(totalBudget / 1000).toFixed(0)}K`} subValue={`Spent: ¥${(totalExpenses / 1000).toFixed(0)}K`} color="bg-green-500" />
                <StatCard icon={Users} label="Leads Generated" value={totalLeads} color="bg-purple-500" />
                <StatCard icon={TrendingUp} label="Avg. ROI" value={`${avgROI.toFixed(0)}%`} color="bg-orange-500" />
            </div>

            {/* Actions */}
            <div className="flex justify-between items-center">
                <div className="flex gap-3">
                    <select className="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                        <option value="all">All Types</option>
                        <option value="Email">Email</option>
                        <option value="SocialMedia">Social Media</option>
                        <option value="TradeShow">Trade Show</option>
                        <option value="Webinar">Webinar</option>
                    </select>
                    <select className="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                        <option value="all">All Status</option>
                        <option value="Active">Active</option>
                        <option value="Draft">Draft</option>
                        <option value="Completed">Completed</option>
                    </select>
                </div>
                <button className="btn btn-primary flex items-center gap-2">
                    <Plus size={16} /> Create Campaign
                </button>
            </div>

            {/* Campaign Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {campaigns.map(campaign => (
                    <div key={campaign.id} className="bg-white rounded-xl border border-gray-200 shadow-sm hover:shadow-md transition-all overflow-hidden">
                        <div className="p-5">
                            <div className="flex items-start justify-between mb-3">
                                <div>
                                    <StatusBadge status={campaign.status} type="campaign" />
                                    <h3 className="font-semibold text-gray-900 mt-2">{campaign.name}</h3>
                                    <p className="text-sm text-gray-500">{campaign.type}</p>
                                </div>
                            </div>

                            <div className="space-y-3 mt-4">
                                {/* Budget Progress */}
                                <div>
                                    <div className="flex justify-between text-sm mb-1">
                                        <span className="text-gray-500">Budget Usage</span>
                                        <span className="font-medium">{((campaign.totalExpenses / campaign.budget) * 100).toFixed(0)}%</span>
                                    </div>
                                    <div className="w-full h-2 bg-gray-100 rounded-full overflow-hidden">
                                        <div 
                                            className="h-full bg-blue-500 rounded-full transition-all" 
                                            style={{ width: `${Math.min((campaign.totalExpenses / campaign.budget) * 100, 100)}%` }}
                                        />
                                    </div>
                                    <div className="flex justify-between text-xs text-gray-400 mt-1">
                                        <span>¥{campaign.totalExpenses.toLocaleString()}</span>
                                        <span>¥{campaign.budget.toLocaleString()}</span>
                                    </div>
                                </div>

                                {/* Metrics */}
                                <div className="grid grid-cols-3 gap-3 pt-3 border-t border-gray-100">
                                    <div className="text-center">
                                        <p className="text-lg font-bold text-gray-900">{campaign.totalLeads}</p>
                                        <p className="text-xs text-gray-500">Leads</p>
                                    </div>
                                    <div className="text-center">
                                        <p className="text-lg font-bold text-gray-900">{campaign.convertedLeads}</p>
                                        <p className="text-xs text-gray-500">Converted</p>
                                    </div>
                                    <div className="text-center">
                                        <p className={`text-lg font-bold ${campaign.roi >= 100 ? 'text-green-600' : campaign.roi >= 50 ? 'text-yellow-600' : 'text-gray-600'}`}>
                                            {campaign.roi}%
                                        </p>
                                        <p className="text-xs text-gray-500">ROI</p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <div className="px-5 py-3 bg-gray-50 border-t border-gray-100 flex justify-between items-center">
                            <span className="text-xs text-gray-500">
                                {new Date(campaign.startDate).toLocaleDateString()} - {new Date(campaign.endDate).toLocaleDateString()}
                            </span>
                            <button className="text-blue-600 hover:text-blue-800 text-sm font-medium">Details</button>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};

// ============== Main CRM Component ==============

export const CRM = () => {
    const [activeTab, setActiveTab] = useState<'leads' | 'opportunities' | 'campaigns'>('leads');

    const tabs = [
        { id: 'leads', label: 'Leads', icon: Users },
        { id: 'opportunities', label: 'Opportunities', icon: Target },
        { id: 'campaigns', label: 'Campaigns', icon: Megaphone },
    ] as const;

    return (
        <div>
            {/* Header */}
            <div className="mb-6">
                <h1 className="text-2xl font-bold text-gray-900">Customer Relationship Management</h1>
                <p className="text-gray-500">Manage leads, opportunities, and marketing campaigns.</p>
            </div>

            {/* Tabs */}
            <div className="border-b border-gray-200 mb-6">
                <nav className="flex gap-8">
                    {tabs.map(tab => (
                        <button
                            key={tab.id}
                            onClick={() => setActiveTab(tab.id)}
                            className={`flex items-center gap-2 pb-4 px-1 border-b-2 transition-colors ${
                                activeTab === tab.id 
                                    ? 'border-blue-600 text-blue-600 font-medium' 
                                    : 'border-transparent text-gray-500 hover:text-gray-700'
                            }`}
                        >
                            <tab.icon size={18} />
                            {tab.label}
                        </button>
                    ))}
                </nav>
            </div>

            {/* Tab Content */}
            {activeTab === 'leads' && <LeadsTab />}
            {activeTab === 'opportunities' && <OpportunitiesTab />}
            {activeTab === 'campaigns' && <CampaignsTab />}
        </div>
    );
};
