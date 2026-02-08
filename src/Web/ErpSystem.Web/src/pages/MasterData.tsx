import React, { useState, useEffect } from 'react';
import { api } from '../services/api';
import { Search, Plus, Layers, Users, Box, FileText } from 'lucide-react';

interface Material {
    id: string;
    code: string;
    name: string;
    description: string;
    baseUnit: string;
}

interface Partner {
    id: string;
    name: string;
    code: string;
    role: string; // Customer/Supplier
}

interface BOM {
    id: string;
    parentMaterialId: string;
    bomName: string;
    version: string;
    status: string;
}

export const MasterData = () => {
    const [activeTab, setActiveTab] = useState<'materials' | 'boms' | 'partners'>('materials');
    const [materials, setMaterials] = useState<Material[]>([]);
    const [partners, setPartners] = useState<Partner[]>([]);
    const [boms, setBoms] = useState<BOM[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        fetchData();
    }, [activeTab]);

    const fetchData = async () => {
        setLoading(true);
        try {
            if (activeTab === 'materials') {
                const { data } = await api.get('/master-data/materials');
                setMaterials(data);
            } else if (activeTab === 'boms') {
                const { data } = await api.get('/master-data/boms');
                setBoms(data);
            } else if (activeTab === 'partners') {
                const { data } = await api.get('/master-data/partners'); // Assuming shared endpoint or separate calls
                setPartners(data);
            }
        } catch (error) {
            console.error("Failed to fetch master data", error);
            // Dummy Data
            if (activeTab === 'materials') {
                setMaterials([
                    { id: '1', code: 'STEEL-5MM', name: 'Steel Sheet 5mm', description: 'Standard Steel Sheet', baseUnit: 'm2' },
                    { id: '2', code: 'SCREW-M5', name: 'M5 Screw', description: 'Stainless Steel M5', baseUnit: 'pcs' },
                ]);
            } else if (activeTab === 'boms') {
                setBoms([
                    { id: '1', parentMaterialId: '3', bomName: 'Car Chassis BOM', version: 'v1.0', status: 'Active' },
                ]);
            } else {
                setPartners([
                    { id: '1', code: 'Cust-001', name: 'Acme Corp', role: 'Customer' },
                    { id: '2', code: 'Supp-002', name: 'Steel Works Ltd', role: 'Supplier' },
                ]);
            }
        } finally {
            setLoading(false);
        }
    };

    const TabButton = ({ id, label, icon: Icon }: any) => (
        <button
            onClick={() => setActiveTab(id)}
            className={`flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-md transition-colors ${activeTab === id
                    ? 'bg-blue-100 text-blue-700'
                    : 'text-gray-600 hover:bg-gray-100'
                }`}
        >
            <Icon size={16} />
            {label}
        </button>
    );

    return (
        <div>
            <div className="flex justify-between items-center mb-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Master Data</h1>
                    <p className="text-gray-500">Manage Materials, BOMs, and Business Partners.</p>
                </div>
                <button className="btn btn-primary">
                    <Plus size={16} className="mr-2" />
                    Add {activeTab === 'materials' ? 'Material' : activeTab === 'boms' ? 'BOM' : 'Partner'}
                </button>
            </div>

            <div className="bg-white border-b border-gray-200 mb-6 px-4 pt-4 flex gap-4">
                <TabButton id="materials" label="Materials" icon={Box} />
                <TabButton id="boms" label="Bill of Materials" icon={Layers} />
                <TabButton id="partners" label="Partners" icon={Users} />
            </div>

            <div className="bg-white rounded-lg border border-gray-200 shadow-sm overflow-hidden">
                {activeTab === 'materials' && (
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Code</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Unit</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Action</th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {materials.map(m => (
                                <tr key={m.id} className="hover:bg-gray-50">
                                    <td className="px-6 py-4 text-sm font-medium text-gray-900">{m.code}</td>
                                    <td className="px-6 py-4 text-sm text-gray-500">{m.name}</td>
                                    <td className="px-6 py-4 text-sm text-gray-500">{m.baseUnit}</td>
                                    <td className="px-6 py-4 text-right text-sm text-blue-600 cursor-pointer">Edit</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}

                {activeTab === 'boms' && (
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">BOM Name</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Version</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Action</th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {boms.map(b => (
                                <tr key={b.id} className="hover:bg-gray-50">
                                    <td className="px-6 py-4 text-sm font-medium text-gray-900">{b.bomName}</td>
                                    <td className="px-6 py-4 text-sm text-gray-500">{b.version}</td>
                                    <td className="px-6 py-4 text-sm">
                                        <span className={`px-2 py-1 rounded-full text-xs font-semibold ${b.status === 'Active' ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
                                            {b.status}
                                        </span>
                                    </td>
                                    <td className="px-6 py-4 text-right text-sm text-blue-600 cursor-pointer">View Components</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}

                {activeTab === 'partners' && (
                    <table className="min-w-full divide-y divide-gray-200">
                        <thead className="bg-gray-50">
                            <tr>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Code</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Role</th>
                                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Action</th>
                            </tr>
                        </thead>
                        <tbody className="bg-white divide-y divide-gray-200">
                            {partners.map(p => (
                                <tr key={p.id} className="hover:bg-gray-50">
                                    <td className="px-6 py-4 text-sm font-medium text-gray-900">{p.code}</td>
                                    <td className="px-6 py-4 text-sm text-gray-500">{p.name}</td>
                                    <td className="px-6 py-4 text-sm text-gray-500">{p.role}</td>
                                    <td className="px-6 py-4 text-right text-sm text-blue-600 cursor-pointer">Edit</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}
            </div>
        </div>
    );
};
