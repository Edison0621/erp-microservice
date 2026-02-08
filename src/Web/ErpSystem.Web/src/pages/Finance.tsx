import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Search, Plus, Calendar, AlertCircle } from 'lucide-react';

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
}

interface AgingReport {
    buckets: { bucket: string; amount: number; count: number }[];
}

export const Finance = () => {
    const [invoices, setInvoices] = useState<Invoice[]>([]);
    const [aging, setAging] = useState<AgingReport | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        setLoading(true);
        try {
            const [invRes, ageRes] = await Promise.all([
                api.get('/finance/invoices'),
                api.get('/finance/reports/aging')
            ]);
            setInvoices(invRes.data);
            setAging(ageRes.data);
        } catch (error) {
            console.error("Failed to fetch finance data", error);
            // Dummy data
            setInvoices([
                { invoiceId: '1', invoiceNumber: 'INV-2023-001', type: 1, partyName: 'Acme Corp', invoiceDate: '2023-10-01', dueDate: '2023-10-31', totalAmount: 1200, outstandingAmount: 600, status: 2 },
                { invoiceId: '2', invoiceNumber: 'INV-2023-002', type: 1, partyName: 'Beta Ltd', invoiceDate: '2023-10-05', dueDate: '2023-11-05', totalAmount: 5000, outstandingAmount: 5000, status: 1 },
            ]);
            setAging({
                buckets: [
                    { bucket: 'Current', amount: 4500, count: 5 },
                    { bucket: '1-30 Days', amount: 1200, count: 2 },
                    { bucket: '31-60 Days', amount: 0, count: 0 },
                    { bucket: '61-90 Days', amount: 500, count: 1 },
                    { bucket: '90+ Days', amount: 0, count: 0 },
                ]
            });
        } finally {
            setLoading(false);
        }
    };

    const statusColors: any = {
        0: 'bg-gray-100 text-gray-800', // Draft
        1: 'bg-blue-100 text-blue-800', // Issued
        2: 'bg-yellow-100 text-yellow-800', // Partial
        3: 'bg-green-100 text-green-800', // Paid
        4: 'bg-red-100 text-red-800', // WrittenOff
    };

    const statusLabels: any = {
        0: 'Draft', 1: 'Issued', 2: 'Partial', 3: 'Paid', 4: 'Written Off', 5: 'Cancelled'
    };

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Finance & Accounting</h1>
                    <p className="text-gray-500">Manage Invoices, Payments, and Cash Flow.</p>
                </div>
                <div className="flex gap-3">
                    <button className="btn btn-primary">
                        <Plus size={16} className="mr-2" /> Create Invoice
                    </button>
                </div>
            </div>

            {/* Aging Report Widget */}
            <div className="grid grid-cols-2 md:grid-cols-5 gap-4 mb-8">
                {aging?.buckets.map((b, i) => (
                    <div key={i} className={`p-4 rounded-lg border ${i === 0 ? 'bg-blue-50 border-blue-200' : 'bg-white border-gray-200'}`}>
                        <p className="text-xs font-medium text-gray-500 uppercase">{b.bucket}</p>
                        <p className="text-xl font-bold text-gray-900 mt-1">${b.amount.toLocaleString()}</p>
                        <p className="text-xs text-gray-400 mt-1">{b.count} invoices</p>
                    </div>
                ))}
            </div>

            {/* Invoice List */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm">
                <div className="p-4 border-b border-gray-200 flex justify-between items-center bg-gray-50">
                    <h3 className="font-semibold text-gray-700">Recent Invoices</h3>
                    <div className="flex items-center gap-2">
                        <Filter size={16} className="text-gray-400" />
                        <span className="text-sm text-gray-500">Filter</span>
                    </div>
                </div>
                <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-white">
                        <tr>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Number</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Client</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
                            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Amount</th>
                            <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Action</th>
                        </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                        {invoices.map((inv) => (
                            <tr key={inv.invoiceId} className="hover:bg-gray-50">
                                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-blue-600">
                                    {inv.invoiceNumber}
                                </td>
                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                                    {inv.partyName}
                                </td>
                                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                    {new Date(inv.invoiceDate).toLocaleDateString()}
                                    <div className="text-xs text-gray-400">Due: {new Date(inv.dueDate).toLocaleDateString()}</div>
                                </td>
                                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium text-gray-900">
                                    ${inv.totalAmount.toLocaleString()}
                                    {inv.outstandingAmount > 0 && inv.outstandingAmount < inv.totalAmount && (
                                        <div className="text-xs text-orange-500">Due: ${inv.outstandingAmount.toLocaleString()}</div>
                                    )}
                                </td>
                                <td className="px-6 py-4 whitespace-nowrap text-center">
                                    <span className={`px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${statusColors[inv.status] || 'bg-gray-100'}`}>
                                        {statusLabels[inv.status] || 'Unknown'}
                                    </span>
                                </td>
                                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                    <button className="text-blue-600 hover:text-blue-900 mr-3">View</button>
                                    {inv.outstandingAmount > 0 && <button className="text-emerald-600 hover:text-emerald-900">Pay</button>}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
};

function Filter({ size, className }: any) {
    return <Search size={size} className={className} />; // Placeholder
}
