import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Plus, ShoppingCart } from 'lucide-react';

interface PurchaseOrder {
    id: string;
    poNumber: string;
    supplierName: string;
    orderDate: string;
    totalAmount: number;
    status: string; // Draft, Confirmed, Received
}

export const Procurement = () => {
    const [orders, setOrders] = useState<PurchaseOrder[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        fetchOrders();
    }, []);

    const fetchOrders = async () => {
        setLoading(true);
        try {
            const { data } = await api.get('/procurement/orders');
            setOrders(data);
        } catch (error) {
            console.error("Failed to fetch POs", error);
            setOrders([
                { id: '1', poNumber: 'PO-2023-001', supplierName: 'Steel Supply Co.', orderDate: '2023-10-15', totalAmount: 5000, status: 'Received' },
                { id: '2', poNumber: 'PO-2023-002', supplierName: 'Bolt Factory', orderDate: '2023-10-20', totalAmount: 200, status: 'Confirmed' },
            ]);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Procurement</h1>
                    <p className="text-gray-500">Manage Purchase Orders and Suppliers.</p>
                </div>
                <button className="btn btn-primary">
                    <Plus size={16} className="mr-2" /> Create PO
                </button>
            </div>

            <div className="bg-white rounded-lg border border-gray-200 shadow-sm overflow-hidden">
                <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                        <tr>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">PO Number</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Supplier</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Date</th>
                            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Total</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Action</th>
                        </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                        {orders.map(po => (
                            <tr key={po.id} className="hover:bg-gray-50">
                                <td className="px-6 py-4 text-sm font-medium text-blue-600">{po.poNumber}</td>
                                <td className="px-6 py-4 text-sm text-gray-900">{po.supplierName}</td>
                                <td className="px-6 py-4 text-sm text-gray-500">{new Date(po.orderDate).toLocaleDateString()}</td>
                                <td className="px-6 py-4 text-right text-sm font-medium">${po.totalAmount.toLocaleString()}</td>
                                <td className="px-6 py-4 text-sm">
                                    <span className={`px-2 py-1 rounded-full text-xs font-semibold ${po.status === 'Received' ? 'bg-green-100 text-green-800' :
                                            po.status === 'Confirmed' ? 'bg-blue-100 text-blue-800' : 'bg-gray-100 text-gray-800'
                                        }`}>
                                        {po.status}
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
