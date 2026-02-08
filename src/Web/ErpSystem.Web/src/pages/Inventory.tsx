import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Search, Filter, Plus, ArrowLeftRight } from 'lucide-react';

interface InventoryItem {
    id: string;
    warehouseId: string;
    binId: string;
    materialId: string;
    materialCode: string;
    materialName: string;
    onHandQuantity: number;
    reservedQuantity: number;
    availableQuantity: number;
    unitCost: number;
    totalValue: number;
}

export const Inventory = () => {
    const [items, setItems] = useState<InventoryItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [filter, setFilter] = useState({ warehouseId: '', materialCode: '' });

    useEffect(() => {
        fetchItems();
    }, [filter]);

    const fetchItems = async () => {
        setLoading(true);
        try {
            const { data } = await api.get('/inventory/items', { params: filter });
            setItems(data);
        } catch (error) {
            console.error("Failed to fetch inventory", error);
            // Dummy data for demo if backend not running
            setItems([
                { id: '1', warehouseId: 'WH-01', binId: 'A-01', materialId: 'M-101', materialCode: 'STEEL-5MM', materialName: 'Steel Sheet 5mm', onHandQuantity: 100, reservedQuantity: 10, availableQuantity: 90, unitCost: 50, totalValue: 5000 },
                { id: '2', warehouseId: 'WH-01', binId: 'A-02', materialId: 'M-102', materialCode: 'ALUM-ROD', materialName: 'Aluminum Rod', onHandQuantity: 500, reservedQuantity: 0, availableQuantity: 500, unitCost: 12.5, totalValue: 6250 },
            ]);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Inventory Management</h1>
                    <p className="text-gray-500">Track stock levels across warehouses and bins.</p>
                </div>
                <div className="flex gap-3">
                    <button className="btn btn-ghost border border-gray-300">
                        <ArrowLeftRight size={16} className="mr-2" /> Transfer
                    </button>
                    <button className="btn btn-primary">
                        <Plus size={16} className="mr-2" /> Receive Stock
                    </button>
                </div>
            </div>

            {/* Filters */}
            <div className="bg-white p-4 rounded-lg border border-gray-200 mb-6 flex gap-4">
                <div className="relative flex-1">
                    <Search className="absolute left-3 top-2.5 text-gray-400" size={20} />
                    <input
                        type="text"
                        placeholder="Search by material code..."
                        className="pl-10 input w-full bg-gray-50"
                        value={filter.materialCode}
                        onChange={e => setFilter({ ...filter, materialCode: e.target.value })}
                    />
                </div>
                <div className="w-48">
                    <select
                        className="input w-full bg-gray-50"
                        value={filter.warehouseId}
                        onChange={e => setFilter({ ...filter, warehouseId: e.target.value })}
                    >
                        <option value="">All Warehouses</option>
                        <option value="WH-01">Main Warehouse</option>
                        <option value="WH-02">Remote Warehouse</option>
                    </select>
                </div>
            </div>

            {/* Table */}
            <div className="bg-white rounded-lg border border-gray-200 overflow-hidden shadow-sm">
                <div className="overflow-x-auto">
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Material</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Location</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">On Hand</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Available</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Value</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {items.map((item) => (
                                <tr key={item.id} className="hover:bg-gray-50 transition-colors">
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <div className="flex items-center">
                                            <div className="h-10 w-10 flex-shrink-0 bg-blue-100 rounded-lg flex items-center justify-center text-blue-600 font-bold">
                                                {item.materialCode.substring(0, 2)}
                                            </div>
                                            <div className="ml-4">
                                                <div className="text-sm font-medium text-gray-900">{item.materialName}</div>
                                                <div className="text-xs text-gray-500">{item.materialCode}</div>
                                            </div>
                                        </div>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap">
                                        <div className="text-sm text-gray-900">{item.warehouseId}</div>
                                        <div className="text-xs text-gray-500">Bin: {item.binId || 'N/A'}</div>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                        {item.onHandQuantity}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm">
                                        <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${item.availableQuantity > 10 ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                                            {item.availableQuantity}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm text-gray-500">
                                        ${item.totalValue.toLocaleString()}
                                    </td>
                                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                                        <button className="text-blue-600 hover:text-blue-900">Details</button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
                {items.length === 0 && !loading && (
                    <div className="p-8 text-center text-gray-500">No inventory items found matching your criteria.</div>
                )}
            </div>
        </div>
    );
};

export default Inventory; // Default export for lazy loading if needed, but named export preferred
