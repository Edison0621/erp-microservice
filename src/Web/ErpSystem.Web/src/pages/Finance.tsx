import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Search, Plus, Calendar, AlertCircle, TrendingUp, TrendingDown, Package, CheckCircle, X } from 'lucide-react';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

interface Invoice {
    invoiceId: string;
    invoiceNumber: string;
    type: number; // 1=AR, 2=AP
    partyName: string;
    invoiceDate: string;
    dueDate: string;
    totalAmount: number;
    outstandingAmount: number;
    status: number; // 0=Draft, 1=Issued, 2=PartiallyPaid, 3=FullyPaid
    linesJson: string; // JSON string of InvoiceLine[]
}

interface InvoiceLine {
    description: string;
    quantity: number;
    unitPrice: number;
    amount: number;
}

interface DashboardStats {
    totalReceivable: number;
    totalPayable: number;
    orderCount: number;
    reconciledCount: number;
    trends: { month: string; incoming: number; outgoing: number }[];
}

export const Finance = () => {
    const [invoices, setInvoices] = useState<Invoice[]>([]);
    const [stats, setStats] = useState<DashboardStats | null>(null);
    const [loading, setLoading] = useState(true);

    // Statement Reconciliation State
    const [reconModalOpen, setReconModalOpen] = useState(false);
    const [lookupSupplierId, setLookupSupplierId] = useState('');
    const [lookupPeriod, setLookupPeriod] = useState(new Date().toISOString().slice(0, 7).replace('-', '')); // yyyyMM
    const [selectedStatement, setSelectedStatement] = useState<any | null>(null);
    const [reconLoading, setReconLoading] = useState(false);

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        setLoading(true);
        try {
            const [invRes, statsRes] = await Promise.all([
                api.get('/finance/invoices'),
                api.get('/finance/stats/dashboard')
            ]);
            setInvoices(invRes.data);
            setStats(statsRes.data);
        } catch (error) {
            console.error("Failed to fetch finance data", error);
        } finally {
            setLoading(false);
        }
    };

    const handleLookupStatement = async () => {
        setReconLoading(true);
        try {
            const { data } = await api.get(`/finance/statements/lookup?supplierId=${lookupSupplierId}&period=${lookupPeriod}`);
            setSelectedStatement(data);
        } catch (error) {
            alert("Statement not found for this period/supplier.");
            setSelectedStatement(null);
        } finally {
            setReconLoading(false);
        }
    };

    const handleReconcile = async () => {
        if (!selectedStatement) return;
        try {
            await api.post(`/finance/statements/${selectedStatement.statementId}/reconcile`);
            alert("Statement reconciled successfully!");
            // Optionally refresh or close
            handleLookupStatement(); // Refresh details
        } catch (error) {
            alert("Failed to reconcile statement.");
        }
    };

    const statusColors: any = {
        0: 'bg-gray-100 text-gray-800',
        1: 'bg-blue-100 text-blue-800',
        2: 'bg-yellow-100 text-yellow-800',
        3: 'bg-green-100 text-green-800',
        4: 'bg-red-100 text-red-800',
    };

    const statusLabels: any = {
        0: 'Draft', 1: 'Issued', 2: 'Partial', 3: 'Paid', 4: 'Written Off', 5: 'Cancelled'
    };

    const getFirstLineProduct = (json: string) => {
        try {
            const lines: InvoiceLine[] = JSON.parse(json);
            return lines.length > 0 ? lines[0].description : 'N/A';
        } catch { return 'N/A'; }
    };

    return (
        <div className="p-6 max-w-7xl mx-auto space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Financial Management</h1>
                    <p className="text-gray-500">Overview of AR/AP, Invoices and Cash Flow.</p>
                </div>
                <div className="flex gap-3">
                    <button className="btn bg-blue-600 text-white hover:bg-blue-700 px-4 py-2 rounded-lg flex items-center">
                        <Plus size={16} className="mr-2" /> New Invoice
                    </button>
                    <button className="btn bg-white border border-gray-300 text-gray-700 hover:bg-gray-50 px-4 py-2 rounded-lg flex items-center">
                        Export Reports
                    </button>
                </div>
            </div>

            {/* Dashboard Stats */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <StatCard
                    title="Total Receivable (AR)"
                    value={`$${stats?.totalReceivable.toLocaleString() ?? '0.00'}`}
                    icon={<TrendingUp className="text-emerald-500" />}
                    trend="+12% from last month"
                />
                <StatCard
                    title="Total Payable (AP)"
                    value={`$${stats?.totalPayable.toLocaleString() ?? '0.00'}`}
                    icon={<TrendingDown className="text-orange-500" />}
                    trend="+5% from last month"
                />
                <StatCard
                    title="Open Orders"
                    value={stats?.orderCount.toString() ?? '0'}
                    icon={<Package className="text-blue-500" />}
                    trend="24 active orders"
                />
                <StatCard
                    title="Reconciled Invoices"
                    value={stats?.reconciledCount.toString() ?? '0'}
                    icon={<CheckCircle className="text-purple-500" />}
                    trend="Last 30 days"
                />
            </div>

            {/* Charts Section */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                <div className="lg:col-span-2 bg-white p-6 rounded-xl border border-gray-200 shadow-sm">
                    <h3 className="text-lg font-semibold text-gray-800 mb-4">Cash Flow Trend (Incoming vs Outgoing)</h3>
                    <div className="h-64">
                        <ResponsiveContainer width="100%" height="100%">
                            <AreaChart data={stats?.trends ?? []}>
                                <defs>
                                    <linearGradient id="colorIn" x1="0" y1="0" x2="0" y2="1">
                                        <stop offset="5%" stopColor="#10B981" stopOpacity={0.1} />
                                        <stop offset="95%" stopColor="#10B981" stopOpacity={0} />
                                    </linearGradient>
                                    <linearGradient id="colorOut" x1="0" y1="0" x2="0" y2="1">
                                        <stop offset="5%" stopColor="#F59E0B" stopOpacity={0.1} />
                                        <stop offset="95%" stopColor="#F59E0B" stopOpacity={0} />
                                    </linearGradient>
                                </defs>
                                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#E5E7EB" />
                                <XAxis dataKey="month" axisLine={false} tickLine={false} tick={{ fill: '#6B7280', fontSize: 12 }} />
                                <YAxis axisLine={false} tickLine={false} tick={{ fill: '#6B7280', fontSize: 12 }} />
                                <Tooltip contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)' }} />
                                <Area type="monotone" dataKey="incoming" stroke="#10B981" fillOpacity={1} fill="url(#colorIn)" name="Incoming" strokeWidth={2} />
                                <Area type="monotone" dataKey="outgoing" stroke="#F59E0B" fillOpacity={1} fill="url(#colorOut)" name="Outgoing" strokeWidth={2} />
                            </AreaChart>
                        </ResponsiveContainer>
                    </div>
                </div>

                <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm">
                    <h3 className="text-lg font-semibold text-gray-800 mb-4">Quick Actions</h3>
                    <div className="space-y-3">
                        <button className="w-full text-left px-4 py-3 rounded-lg bg-gray-50 hover:bg-gray-100 flex items-center justify-between group">
                            <span className="text-sm font-medium text-gray-700">Record Payment</span>
                            <span className="text-gray-400 group-hover:text-gray-600">→</span>
                        </button>
                        <button
                            onClick={() => setReconModalOpen(true)}
                            className="w-full text-left px-4 py-3 rounded-lg bg-blue-50 hover:bg-blue-100 border border-blue-100 flex items-center justify-between group"
                        >
                            <span className="text-sm font-medium text-blue-700 font-bold">Statement Reconciliation</span>
                            <span className="text-blue-400 group-hover:text-blue-600">→</span>
                        </button>
                        <button className="w-full text-left px-4 py-3 rounded-lg bg-gray-50 hover:bg-gray-100 flex items-center justify-between group">
                            <span className="text-sm font-medium text-gray-700">Aging Report</span>
                            <span className="text-gray-400 group-hover:text-gray-600">→</span>
                        </button>
                    </div>
                </div>
            </div>

            {/* Detailed Table */}
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
                <div className="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
                    <h3 className="font-semibold text-gray-700">Recent Transactions</h3>
                    <div className="flex items-center gap-2 bg-white px-3 py-1.5 rounded-md border border-gray-300">
                        <Search size={14} className="text-gray-400" />
                        <input type="text" placeholder="Search invoices..." className="text-sm outline-none w-48" />
                    </div>
                </div>
                <div className="overflow-x-auto">
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Invoice / Order</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Product / Desc</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Party</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Amount</th>
                                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Due Date</th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {invoices.map((inv) => (
                                <tr key={inv.invoiceId} className="hover:bg-gray-50 transition-colors">
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <div className="text-sm font-medium text-blue-600 cursor-pointer hover:underline">{inv.invoiceNumber}</div>
                                        <div className="text-xs text-gray-400">{inv.type === 1 ? 'AR Invoice' : 'AP Invoice'}</div>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <div className="text-sm text-gray-900">{getFirstLineProduct(inv.linesJson)}</div>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-700">
                                        {inv.partyName}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm">
                                        <div className="font-medium text-gray-900">${inv.totalAmount.toLocaleString()}</div>
                                        {inv.outstandingAmount > 0 && inv.outstandingAmount < inv.totalAmount && (
                                            <div className="text-xs text-orange-500">Unpaid: ${inv.outstandingAmount.toLocaleString()}</div>
                                        )}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-center">
                                        <span className={`px-2.5 py-0.5 inline-flex text-xs font-medium rounded-full ${statusColors[inv.status]}`}>
                                            {statusLabels[inv.status]}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-500">
                                        {new Date(inv.dueDate).toLocaleDateString()}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Reconciliation Modal */}
            {reconModalOpen && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
                    <div className="bg-white rounded-lg shadow-xl max-w-4xl w-full">
                        <div className="p-6 border-b border-gray-200 flex justify-between items-center">
                            <h2 className="text-lg font-bold">Statement Reconciliation</h2>
                            <button onClick={() => setReconModalOpen(false)} className="text-gray-400 hover:text-gray-600">
                                <X size={24} />
                            </button>
                        </div>
                        <div className="p-6 space-y-4">
                            <div className="flex gap-4 items-end">
                                <div className="flex-1 space-y-1">
                                    <label className="text-xs font-medium text-gray-500">Supplier ID</label>
                                    <input
                                        type="text"
                                        className="w-full border rounded px-3 py-2"
                                        value={lookupSupplierId}
                                        onChange={e => setLookupSupplierId(e.target.value)}
                                        placeholder="Enter Supplier ID"
                                    />
                                </div>
                                <div className="w-40 space-y-1">
                                    <label className="text-xs font-medium text-gray-500">Period (yyyyMM)</label>
                                    <input
                                        type="text"
                                        className="w-full border rounded px-3 py-2"
                                        value={lookupPeriod}
                                        onChange={e => setLookupPeriod(e.target.value)}
                                        placeholder="202310"
                                    />
                                </div>
                                <button
                                    onClick={handleLookupStatement}
                                    className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 h-[42px]"
                                    disabled={reconLoading}
                                >
                                    {reconLoading ? 'Searching...' : 'Search'}
                                </button>
                            </div>

                            {selectedStatement && (
                                <div className="mt-6 border rounded-lg overflow-hidden">
                                    <div className="bg-gray-50 p-4 border-b flex justify-between items-center">
                                        <div>
                                            <span className="text-sm text-gray-500">Status: </span>
                                            <span className={`text-sm font-bold ${selectedStatement.status === 'Reconciled' ? 'text-green-600' : 'text-blue-600'}`}>
                                                {selectedStatement.status}
                                            </span>
                                        </div>
                                        <div className="text-lg font-bold">
                                            Total: ${selectedStatement.totalAmount.toLocaleString()}
                                        </div>
                                    </div>
                                    <div className="max-h-64 overflow-y-auto">
                                        <table className="min-w-full divide-y divide-gray-200">
                                            <thead className="bg-gray-50 sticky top-0">
                                                <tr>
                                                    <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
                                                    <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
                                                    <th className="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Ref #</th>
                                                    <th className="px-4 py-2 text-right text-xs font-medium text-gray-500 uppercase">Amount</th>
                                                </tr>
                                            </thead>
                                            <tbody className="bg-white divide-y divide-gray-200">
                                                {selectedStatement.lines.map((line: any, i: number) => (
                                                    <tr key={i}>
                                                        <td className="px-4 py-2 text-sm">{new Date(line.date).toLocaleDateString()}</td>
                                                        <td className="px-4 py-2 text-sm">
                                                            <span className={line.type === 'GoodsReturned' ? 'text-red-600' : 'text-gray-900'}>
                                                                {line.type}
                                                            </span>
                                                        </td>
                                                        <td className="px-4 py-2 text-sm text-gray-500">{line.sourceNumber}</td>
                                                        <td className="px-4 py-2 text-right text-sm font-medium">
                                                            ${line.amount.toLocaleString()}
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    </div>
                                </div>
                            )}
                        </div>
                        <div className="p-6 border-t border-gray-200 flex justify-end gap-3">
                            <button onClick={() => setReconModalOpen(false)} className="btn border border-gray-300 px-4 py-2 rounded text-gray-700">Close</button>
                            {selectedStatement && selectedStatement.status === 'Open' && (
                                <button onClick={handleReconcile} className="btn bg-green-600 text-white px-4 py-2 rounded hover:bg-green-700 flex items-center">
                                    <CheckCircle size={16} className="mr-2" /> Reconcile Statement
                                </button>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};


const StatCard = ({ title, value, icon, trend }: any) => (
    <div className="bg-white p-6 rounded-xl border border-gray-200 shadow-sm flex flex-col justify-between">
        <div className="flex justify-between items-start">
            <div>
                <p className="text-sm font-medium text-gray-500">{title}</p>
                <h3 className="text-2xl font-bold text-gray-900 mt-2">{value}</h3>
            </div>
            <div className="p-2 bg-gray-50 rounded-lg">{icon}</div>
        </div>
        <div className="mt-4 flex items-center text-xs text-gray-500">
            {trend}
        </div>
    </div>
);

