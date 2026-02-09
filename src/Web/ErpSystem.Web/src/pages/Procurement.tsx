import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Plus, ShoppingCart, RotateCcw, X } from 'lucide-react';

interface PurchaseOrder {
    id: string;
    poNumber: string;
    supplierName: string;
    orderDate: string;
    totalAmount: number;
    status: number; // 0=Draft, 1=PendingApproval, 2=Approved, 3=SentToSupplier, 4=PartiallyReceived, 5=FullyReceived, 6=Closed, 7=Cancelled
}

interface PurchaseOrderLine {
    lineNumber: string;
    materialId: string;
    materialName: string;
    orderedQuantity: number;
    receivedQuantity: number;
    unitPrice: number;
}

interface PurchaseOrderDetails extends PurchaseOrder {
    lines: PurchaseOrderLine[];
}

export const Procurement = () => {
    const [orders, setOrders] = useState<PurchaseOrder[]>([]);
    const [loading, setLoading] = useState(false);
    const [selectedPo, setSelectedPo] = useState<PurchaseOrderDetails | null>(null);
    const [returnModalOpen, setReturnModalOpen] = useState(false);
    const [returnQuantities, setReturnQuantities] = useState<Record<string, number>>({});
    const [returnReason, setReturnReason] = useState('');

    useEffect(() => {
        fetchOrders();
    }, []);

    const fetchOrders = async () => {
        setLoading(true);
        try {
            const { data } = await api.get('/procurement/purchase-orders'); // Updated endpoint to match controller
            setOrders(data);
        } catch (error) {
            console.error("Failed to fetch POs", error);
        } finally {
            setLoading(false);
        }
    };

    const handleOpenReturnModal = async (poId: string) => {
        try {
            const { data } = await api.get(`/procurement/purchase-orders/${poId}`);
            setSelectedPo(data);
            setReturnQuantities({});
            setReturnReason('');
            setReturnModalOpen(true);
        } catch (error) {
            alert("Failed to load PO details");
        }
    };

    const handleReturnSubmit = async () => {
        if (!selectedPo) return;

        const lines = Object.entries(returnQuantities)
            .filter(([_, qty]) => qty > 0)
            .map(([lineNumber, quantity]) => ({
                lineNumber,
                quantity,
                reason: returnReason // simplified per-line reason sharing global reason for now if backend allowed per-line, but backend event has global reason too? backend command has global reason. backend event has global reason. 
                // Wait, Controller expects List<ReturnLine> where ReturnLine is (LineNumber, Quantity, Reason).
                // So I should map global reason to each line or allow per-line reason. 
                // Let's use global reason for all lines for simplicity in UI.
            }));

        if (lines.length === 0) {
            alert("Please enter quantity to return");
            return;
        }

        try {
            await api.post(`/procurement/purchase-orders/${selectedPo.id}/return`, {
                lines: lines.map(l => ({ ...l, reason: returnReason })),
                reason: returnReason
            });
            alert("Return processed successfully");
            setReturnModalOpen(false);
            fetchOrders(); // Refresh list
        } catch (error) {
            console.error("Return failed", error);
            alert("Failed to process return");
        }
    };

    const statusLabels: any = {
        0: 'Draft', 1: 'Pending', 2: 'Approved', 3: 'Sent', 4: 'Partial Recv', 5: 'Received', 6: 'Closed', 7: 'Cancelled'
    };

    const statusColors: any = {
        4: 'bg-yellow-100 text-yellow-800',
        5: 'bg-green-100 text-green-800',
        3: 'bg-blue-100 text-blue-800'
    };

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Procurement</h1>
                    <p className="text-gray-500">Manage Purchase Orders and Returns.</p>
                </div>
                <button className="btn bg-blue-600 text-white hover:bg-blue-700 px-4 py-2 rounded-lg flex items-center">
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
                                <td className="px-6 py-4 text-sm text-gray-900">{po.supplierName || 'N/A'}</td>
                                <td className="px-6 py-4 text-sm text-gray-500">{new Date(po.orderDate).toLocaleDateString()}</td>
                                <td className="px-6 py-4 text-right text-sm font-medium">${po.totalAmount?.toLocaleString()}</td>
                                <td className="px-6 py-4 text-sm">
                                    <span className={`px-2 py-1 rounded-full text-xs font-semibold ${statusColors[po.status] || 'bg-gray-100 text-gray-800'}`}>
                                        {statusLabels[po.status] || po.status}
                                    </span>
                                </td>
                                <td className="px-6 py-4 text-right text-sm space-x-2">
                                    {(po.status === 4 || po.status === 5) && (
                                        <button
                                            onClick={() => handleOpenReturnModal(po.id)}
                                            className="text-red-600 hover:text-red-900 flex items-center inline-flex"
                                            title="Return Goods"
                                        >
                                            <RotateCcw size={14} className="mr-1" /> Return
                                        </button>
                                    )}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {/* Return Modal */}
            {returnModalOpen && selectedPo && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
                    <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full">
                        <div className="p-6 border-b border-gray-200 flex justify-between items-center">
                            <h2 className="text-lg font-bold">Return Goods - {selectedPo.poNumber}</h2>
                            <button onClick={() => setReturnModalOpen(false)} className="text-gray-400 hover:text-gray-600">
                                <X size={24} />
                            </button>
                        </div>
                        <div className="p-6 space-y-4">
                            <div className="space-y-2">
                                <label className="block text-sm font-medium text-gray-700">Reason for Return</label>
                                <input
                                    type="text"
                                    className="w-full border border-gray-300 rounded-md px-3 py-2"
                                    value={returnReason}
                                    onChange={(e) => setReturnReason(e.target.value)}
                                    placeholder="Damaged, Wrong Item, etc."
                                />
                            </div>

                            <table className="min-w-full divide-y divide-gray-200">
                                <thead>
                                    <tr>
                                        <th className="px-2 py-2 text-left text-xs text-gray-500">Item</th>
                                        <th className="px-2 py-2 text-right text-xs text-gray-500">Ordered</th>
                                        <th className="px-2 py-2 text-right text-xs text-gray-500">Received</th>
                                        <th className="px-2 py-2 text-right text-xs text-gray-500">Return Qty</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {selectedPo.lines.map(line => (
                                        <tr key={line.lineNumber}>
                                            <td className="px-2 py-2 text-sm">{line.materialName}</td>
                                            <td className="px-2 py-2 text-right text-sm">{line.orderedQuantity}</td>
                                            <td className="px-2 py-2 text-right text-sm">{line.receivedQuantity}</td>
                                            <td className="px-2 py-2 text-right">
                                                <input
                                                    type="number"
                                                    max={line.receivedQuantity}
                                                    min="0"
                                                    className="w-20 border border-gray-300 rounded px-2 py-1 text-right"
                                                    value={returnQuantities[line.lineNumber] || ''}
                                                    onChange={(e) => {
                                                        const val = parseFloat(e.target.value);
                                                        if (val > line.receivedQuantity) return;
                                                        setReturnQuantities({ ...returnQuantities, [line.lineNumber]: val });
                                                    }}
                                                />
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                        <div className="p-6 border-t border-gray-200 flex justify-end gap-3">
                            <button onClick={() => setReturnModalOpen(false)} className="btn border border-gray-300 px-4 py-2 rounded text-gray-700">Cancel</button>
                            <button onClick={handleReturnSubmit} className="btn bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700">Confirm Return</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};
