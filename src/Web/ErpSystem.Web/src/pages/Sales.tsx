import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Plus, TrendingUp } from 'lucide-react';

interface SalesOrder {
    id: string;
    soNumber: string;
    customerName: string;
    orderDate: string;
    totalAmount: number;
    status: string; // Draft, Confirmed, Shipped
}

export const Sales = () => {
    const [orders, setOrders] = useState<SalesOrder[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        fetchOrders();
    }, []);

    const fetchOrders = async () => {
        setLoading(true);
        try {
            const { data } = await api.get('/sales/orders');
            setOrders(data);
        } catch (error) {
            console.error("Failed to fetch SOs", error);
            setOrders([
                { id: '1', soNumber: 'SO-2023-101', customerName: 'Tech Corp', orderDate: '2023-10-18', totalAmount: 12000, status: 'Shipped' },
                { id: '2', soNumber: 'SO-2023-102', customerName: 'Retail Inc', orderDate: '2023-10-22', totalAmount: 4500, status: 'Confirmed' },
            ]);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Sales</h1>
                    <p className="text-gray-500">Manage Sales Orders and Customers.</p>
                </div>
                <button className="btn btn-primary">
                    <Plus size={16} className="mr-2" /> Create Order
                </button>
            </div>

            <div className="bg-white rounded-lg border border-gray-200 shadow-sm overflow-hidden">
                <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                        <tr>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">SO Number</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Customer</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
                            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Total</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Action</th>
                        </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                        {orders.map(so => (
                            <tr key={so.id} className="hover:bg-gray-50">
                                <td className="px-6 py-4 text-sm font-medium text-blue-600">{so.soNumber}</td>
                                <td className="px-6 py-4 text-sm text-gray-900">{so.customerName}</td>
                                <td className="px-6 py-4 text-sm text-gray-500">{new Date(so.orderDate).toLocaleDateString()}</td>
                                <td className="px-6 py-4 text-right text-sm font-medium">${so.totalAmount.toLocaleString()}</td>
                                <td className="px-6 py-4 text-sm">
                                    <span className={`px-2 py-1 rounded-full text-xs font-semibold ${so.status === 'Shipped' ? 'bg-green-100 text-green-800' :
                                            so.status === 'Confirmed' ? 'bg-blue-100 text-blue-800' : 'bg-gray-100 text-gray-800'
                                        }`}>
                                        {so.status}
                                    </span>
                                </td>
                                <td className="px-6 py-4 text-right text-sm text-blue-600 cursor-pointer">View</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
};
