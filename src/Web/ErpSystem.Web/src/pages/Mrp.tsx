import React from 'react';
import { Binary, CheckCircle2, AlertCircle, ArrowRight } from 'lucide-react';

export const Mrp = () => {
    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 font-display">MRP Management</h1>
                    <p className="text-gray-500">Material Requirements Planning & Reordering Rules</p>
                </div>
                <button className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors shadow-sm">
                    Run MRP Engine
                </button>
            </div>

            {/* Stats Overview */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm flex items-center gap-4">
                    <div className="w-12 h-12 bg-blue-50 rounded-lg flex items-center justify-center text-blue-600">
                        <Binary size={24} />
                    </div>
                    <div>
                        <p className="text-sm text-gray-500">Active Rules</p>
                        <p className="text-2xl font-bold text-gray-900">124</p>
                    </div>
                </div>
                <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm flex items-center gap-4">
                    <div className="w-12 h-12 bg-orange-50 rounded-lg flex items-center justify-center text-orange-600">
                        <AlertCircle size={24} />
                    </div>
                    <div>
                        <p className="text-sm text-gray-500">Pending Suggestions</p>
                        <p className="text-2xl font-bold text-gray-900">12</p>
                    </div>
                </div>
                <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm flex items-center gap-4">
                    <div className="w-12 h-12 bg-green-50 rounded-lg flex items-center justify-center text-green-600">
                        <CheckCircle2 size={24} />
                    </div>
                    <div>
                        <p className="text-sm text-gray-500">Approved Today</p>
                        <p className="text-2xl font-bold text-gray-900">8</p>
                    </div>
                </div>
            </div>

            {/* Procurement Suggestions */}
            <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
                <div className="p-6 border-b border-gray-100 flex justify-between items-center">
                    <h2 className="font-bold text-gray-900">Procurement Suggestions</h2>
                    <span className="text-sm text-blue-600 font-medium cursor-pointer hover:underline">View All</span>
                </div>
                <div className="overflow-x-auto">
                    <table className="w-full text-left">
                        <thead>
                            <tr className="bg-gray-50 text-gray-500 text-xs uppercase tracking-wider">
                                <th className="px-6 py-3 font-medium">Material</th>
                                <th className="px-6 py-3 font-medium">Quantity</th>
                                <th className="px-6 py-3 font-medium">Suggested Date</th>
                                <th className="px-6 py-3 font-medium">Reason</th>
                                <th className="px-6 py-3 font-medium">Action</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100 text-sm">
                            <tr>
                                <td className="px-6 py-4">
                                    <p className="font-medium text-gray-900">Steel Sheet 2mm</p>
                                    <p className="text-xs text-gray-500">MAT-ST-002</p>
                                </td>
                                <td className="px-6 py-4 text-gray-600 font-medium">500 units</td>
                                <td className="px-6 py-4 text-gray-600">Feb 15, 2026</td>
                                <td className="px-6 py-4">
                                    <span className="px-2 py-1 bg-red-50 text-red-600 text-xs rounded-full font-medium">Below Minimum</span>
                                </td>
                                <td className="px-6 py-4">
                                    <button className="text-blue-600 font-medium flex items-center gap-1 hover:text-blue-800 transition-colors">
                                        Approve <ArrowRight size={14} />
                                    </button>
                                </td>
                            </tr>
                            <tr>
                                <td className="px-6 py-4">
                                    <p className="font-medium text-gray-900">Electronic Component IC-7</p>
                                    <p className="text-xs text-gray-500">MAT-EL-IC7</p>
                                </td>
                                <td className="px-6 py-4 text-gray-600 font-medium">1,200 units</td>
                                <td className="px-6 py-4 text-gray-600">Feb 12, 2026</td>
                                <td className="px-6 py-4">
                                    <span className="px-2 py-1 bg-orange-50 text-orange-600 text-xs rounded-full font-medium">Forecast Demand</span>
                                </td>
                                <td className="px-6 py-4">
                                    <button className="text-blue-600 font-medium flex items-center gap-1 hover:text-blue-800 transition-colors">
                                        Approve <ArrowRight size={14} />
                                    </button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Reordering Rules */}
            <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
                <div className="p-6 border-b border-gray-100 flex justify-between items-center">
                    <h2 className="font-bold text-gray-900">Reordering Rules</h2>
                    <button className="text-sm bg-gray-50 text-gray-600 px-3 py-1 rounded border border-gray-200 hover:bg-gray-100">Add Rule</button>
                </div>
                <div className="overflow-x-auto">
                    <table className="w-full text-left">
                        <thead>
                            <tr className="bg-gray-50 text-gray-500 text-xs uppercase tracking-wider">
                                <th className="px-6 py-3 font-medium">Material</th>
                                <th className="px-6 py-3 font-medium">Warehouse</th>
                                <th className="px-6 py-3 font-medium">Min / Max</th>
                                <th className="px-6 py-3 font-medium">Multiple</th>
                                <th className="px-6 py-3 font-medium">Forecast</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100 text-sm">
                            <tr>
                                <td className="px-6 py-4 font-medium text-gray-900">Steel Sheet 2mm</td>
                                <td className="px-6 py-4 text-gray-600">WH-Main</td>
                                <td className="px-6 py-4 text-gray-600">100 / 1000</td>
                                <td className="px-6 py-4 text-gray-600">100</td>
                                <td className="px-6 py-4">
                                    <div className="flex items-center gap-2">
                                        <div className="w-24 h-2 bg-gray-100 rounded-full overflow-hidden">
                                            <div className="h-full bg-blue-500" style={{ width: '45%' }}></div>
                                        </div>
                                        <span className="text-xs text-gray-500 text-nowrap">Down 12%</span>
                                    </div>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
};
