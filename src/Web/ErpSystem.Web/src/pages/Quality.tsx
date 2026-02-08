import React from 'react';
import { ShieldCheck, CheckSquare, AlertTriangle, FileSearch, Timer } from 'lucide-react';

export const Quality = () => {
    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900 font-display">Quality Control</h1>
                    <p className="text-gray-500">Manage Quality Points, Inspections, and Alerts</p>
                </div>
                <div className="flex gap-3">
                    <button className="bg-white border border-gray-200 text-gray-600 px-4 py-2 rounded-lg font-medium hover:bg-gray-50 transition-colors">
                        Quality Alerts
                    </button>
                    <button className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors shadow-sm">
                        Define Quality Point
                    </button>
                </div>
            </div>

            {/* Stats Overview */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
                <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm">
                    <div className="flex justify-between items-start mb-4">
                        <div className="w-10 h-10 bg-blue-50 rounded-lg flex items-center justify-center text-blue-600">
                            <FileSearch size={20} />
                        </div>
                        <span className="text-xs font-medium text-blue-600 bg-blue-50 px-2 py-0.5 rounded-full">Weekly</span>
                    </div>
                    <p className="text-sm text-gray-500">Pending Checks</p>
                    <p className="text-2xl font-bold text-gray-900">24</p>
                </div>
                <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm">
                    <div className="flex justify-between items-start mb-4">
                        <div className="w-10 h-10 bg-green-50 rounded-lg flex items-center justify-center text-green-600">
                            <CheckSquare size={20} />
                        </div>
                        <span className="text-xs font-medium text-green-600 bg-green-50 px-2 py-0.5 rounded-full">+12%</span>
                    </div>
                    <p className="text-sm text-gray-500">Pass Rate</p>
                    <p className="text-2xl font-bold text-gray-900">98.4%</p>
                </div>
                <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm">
                    <div className="flex justify-between items-start mb-4">
                        <div className="w-10 h-10 bg-red-50 rounded-lg flex items-center justify-center text-red-600">
                            <AlertTriangle size={20} />
                        </div>
                        <span className="text-xs font-medium text-red-600 bg-red-50 px-2 py-0.5 rounded-full">Critical</span>
                    </div>
                    <p className="text-sm text-gray-500">Open Alerts</p>
                    <p className="text-2xl font-bold text-gray-900">3</p>
                </div>
                <div className="bg-white p-6 rounded-xl border border-gray-100 shadow-sm">
                    <div className="flex justify-between items-start mb-4">
                        <div className="w-10 h-10 bg-gray-50 rounded-lg flex items-center justify-center text-gray-600">
                            <Timer size={20} />
                        </div>
                        <span className="text-xs font-medium text-gray-600 bg-gray-50 px-2 py-0.5 rounded-full">-5m</span>
                    </div>
                    <p className="text-sm text-gray-500">Avg. Check Time</p>
                    <p className="text-2xl font-bold text-gray-900">12m 40s</p>
                </div>
            </div>

            {/* Quality Checks Table */}
            <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
                <div className="p-6 border-b border-gray-100">
                    <h2 className="font-bold text-gray-900">Active Quality Checks</h2>
                </div>
                <div className="overflow-x-auto">
                    <table className="w-full text-left font-display">
                        <thead>
                            <tr className="bg-gray-50 text-gray-500 text-xs uppercase tracking-wider font-display">
                                <th className="px-6 py-3 font-medium">Source Document</th>
                                <th className="px-6 py-3 font-medium">Material</th>
                                <th className="px-6 py-3 font-medium">Type</th>
                                <th className="px-6 py-3 font-medium">Created At</th>
                                <th className="px-6 py-3 font-medium">Action</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-100 text-sm">
                            <tr>
                                <td className="px-6 py-4">
                                    <span className="font-medium text-blue-600">GR-2026-042</span>
                                    <p className="text-xs text-gray-500 mt-1 uppercase">Goods Receipt</p>
                                </td>
                                <td className="px-6 py-4">
                                    <p className="font-medium text-gray-900 font-display">Aluminum Casting Hub</p>
                                    <p className="text-xs text-gray-500">MAT-AL-HUB</p>
                                </td>
                                <td className="px-6 py-4">
                                    <span className="px-2 py-1 bg-purple-50 text-purple-600 text-[10px] font-bold rounded uppercase tracking-tight">Dimensional</span>
                                </td>
                                <td className="px-6 py-4 text-gray-600">2 hours ago</td>
                                <td className="px-6 py-4">
                                    <button className="bg-blue-600 text-white text-xs px-3 py-1.5 rounded-md hover:bg-blue-700 transition-colors shadow-sm">
                                        Execute Check
                                    </button>
                                </td>
                            </tr>
                            <tr>
                                <td className="px-6 py-4">
                                    <span className="font-medium text-blue-600">MO-9001-B</span>
                                    <p className="text-xs text-gray-500 mt-1 uppercase">Manufacturing Order</p>
                                </td>
                                <td className="px-6 py-4">
                                    <p className="font-medium text-gray-900">Main Bearing Assembly</p>
                                    <p className="text-xs text-gray-500">ASSY-BRG-M1</p>
                                </td>
                                <td className="px-6 py-4">
                                    <span className="px-2 py-1 bg-yellow-50 text-yellow-600 text-[10px] font-bold rounded uppercase tracking-tight font-display">Functional Test</span>
                                </td>
                                <td className="px-6 py-4 text-gray-600">45 minutes ago</td>
                                <td className="px-6 py-4">
                                    <button className="bg-blue-600 text-white text-xs px-3 py-1.5 rounded-md hover:bg-blue-700 transition-colors shadow-sm font-display">
                                        Execute Check
                                    </button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Quality Alerts */}
            <div className="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
                <div className="p-6 border-b border-gray-100 flex justify-between items-center">
                    <h2 className="font-bold text-gray-900">Recent Quality Alerts</h2>
                    <span className="text-sm text-red-600 font-medium cursor-pointer hover:underline">3 Active Critical Alerts</span>
                </div>
                <div className="p-6 space-y-4">
                    <div className="flex items-start gap-4 p-4 bg-red-50 rounded-lg border border-red-100">
                        <div className="w-10 h-10 bg-red-100 rounded-full flex items-center justify-center text-red-600 flex-shrink-0">
                            <AlertTriangle size={20} />
                        </div>
                        <div className="flex-1">
                            <div className="flex justify-between items-start">
                                <h3 className="font-bold text-red-900 uppercase tracking-tight text-sm">Structural Defect Detected</h3>
                                <span className="text-xs bg-red-200 text-red-800 px-2 py-0.5 rounded font-bold uppercase tracking-tight">Urgent</span>
                            </div>
                            <p className="text-sm text-red-800 mt-1">Cracks found in 15% of Aluminum Casting Hub (GR-2026-042). Supplier QC notified.</p>
                            <div className="mt-3 flex gap-4 text-xs font-medium text-red-700 uppercase tracking-tighter">
                                <span>Reported: 2h ago</span>
                                <span>Assigned: Quality Team A</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};
