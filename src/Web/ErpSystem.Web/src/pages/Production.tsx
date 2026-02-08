import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Plus, Factory } from 'lucide-react';

interface ProductionOrder {
    id: string;
    moNumber: string;
    itemCode: string;
    itemName: string;
    quantity: number;
    startDate: string;
    dueDate: string;
    status: string; // Planned, Released, Completed
}

export const Production = () => {
    const [orders, setOrders] = useState<ProductionOrder[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        fetchOrders();
    }, []);

    const fetchOrders = async () => {
        setLoading(true);
        try {
            const { data } = await api.get('/production/orders');
            setOrders(data);
        } catch (error) {
            console.error("Failed to fetch MOs", error);
            setOrders([
                { id: '1', moNumber: 'MO-2023-001', itemCode: 'PROD-X', itemName: 'Widget X', quantity: 100, startDate: '2023-11-01', dueDate: '2023-11-10', status: 'Released' },
                { id: '2', moNumber: 'MO-2023-002', itemCode: 'PROD-Y', itemName: 'Widget Y', quantity: 50, startDate: '2023-11-05', dueDate: '2023-11-15', status: 'Planned' },
            ]);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Production</h1>
                    <p className="text-gray-500">Manage Manufacturing Orders and Work Centers.</p>
                </div>
                <button className="btn btn-primary">
                    <Plus size={16} className="mr-2" /> Create Order
                </button>
            </div>

            <div className="bg-white rounded-lg border border-gray-200 shadow-sm overflow-hidden">
                <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                        <tr>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">MO Number</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Product</th>
                            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Qty</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Due Date</th>
                            <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                            <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Action</th>
                        </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                        {orders.map(mo => (
                            <tr key={mo.id} className="hover:bg-gray-50">
                                <td className="px-6 py-4 text-sm font-medium text-blue-600">{mo.moNumber}</td>
                                <td className="px-6 py-4 text-sm text-gray-900">
                                    <div className="font-medium">{mo.itemName}</div>
                                    <div className="text-xs text-gray-500">{mo.itemCode}</div>
                                </td>
                                <td className="px-6 py-4 text-right text-sm text-gray-900">{mo.quantity}</td>
                                <td className="px-6 py-4 text-sm text-gray-500">{new Date(mo.dueDate).toLocaleDateString()}</td>
                                <td className="px-6 py-4 text-sm">
                                    <span className={`px-2 py-1 rounded-full text-xs font-semibold ${mo.status === 'Completed' ? 'bg-green-100 text-green-800' :
                                            mo.status === 'Released' ? 'bg-blue-100 text-blue-800' : 'bg-gray-100 text-gray-800'
                                        }`}>
                                        {mo.status}
                                    </span>
                                </td>
                                <td className="px-6 py-4 text-right text-sm text-blue-600 cursor-pointer">Manage</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
};
