import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { LayoutDashboard, Package, Calculator, FileText, Settings, Menu, ShoppingCart, TrendingUp, Factory, Zap, ShieldCheck, BarChart3, Binary, UserPlus, FolderKanban, Wallet, Building2 } from 'lucide-react';

const NavItem = ({ to, icon: Icon, label }: { to: string; icon: any; label: string }) => {
    const location = useLocation();
    const isActive = location.pathname.startsWith(to);

    return (
        <Link
            to={to}
            className={`flex items-center gap-3 px-4 py-3 rounded-md transition-colors ${isActive
                ? 'bg-blue-50 text-blue-600 font-medium'
                : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900'
                }`}
        >
            <Icon size={20} />
            <span>{label}</span>
            {isActive && <div className="ml-auto w-1.5 h-1.5 rounded-full bg-blue-600" />}
        </Link>
    );
};

export const MainLayout = ({ children }: { children: React.ReactNode }) => {
    return (
        <div className="flex min-h-screen bg-gray-50">
            {/* Sidebar */}
            <aside className="fixed left-0 top-0 bottom-0 w-[250px] bg-white border-r border-gray-200 z-20">
                <div className="h-16 flex items-center px-6 border-b border-gray-100">
                    <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center mr-3">
                        <span className="text-white font-bold text-lg">E</span>
                    </div>
                    <span className="font-bold text-xl text-gray-900">ErpSystem</span>
                </div>

                <nav className="p-4 space-y-1">
                    <NavItem to="/" icon={LayoutDashboard} label="Dashboard" />
                    <NavItem to="/inventory" icon={Package} label="Inventory" />
                    <NavItem to="/finance" icon={Calculator} label="Finance" />
                    <NavItem to="/master-data" icon={FileText} label="Master Data" />
                    <NavItem to="/procurement" icon={ShoppingCart} label="Procurement" />
                    <NavItem to="/sales" icon={TrendingUp} label="Sales" />
                    <NavItem to="/crm" icon={UserPlus} label="CRM" />
                    <NavItem to="/projects" icon={FolderKanban} label="Projects" />
                    <NavItem to="/payroll" icon={Wallet} label="Payroll" />
                    <NavItem to="/assets" icon={Building2} label="Assets" />
                    <NavItem to="/production" icon={Factory} label="Production" />
                    <NavItem to="/mrp" icon={Binary} label="MRP" />
                    <NavItem to="/quality" icon={ShieldCheck} label="Quality Control" />
                    <NavItem to="/automation" icon={Zap} label="Automation" />
                    <NavItem to="/analytics" icon={BarChart3} label="Analytics" />
                    <div className="pt-4 mt-4 border-t border-gray-100">
                        <NavItem to="/settings" icon={Settings} label="Settings" />
                    </div>
                </nav>
            </aside>

            {/* Main Content */}
            <div className="ml-[250px] flex-1 flex flex-col min-h-screen">
                {/* Header */}
                <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-8 sticky top-0 z-10">
                    <div className="flex items-center text-gray-400">
                        <Menu size={20} className="mr-4 lg:hidden" />
                        <span className="text-sm">Welcome back, User</span>
                    </div>
                    <div className="flex items-center gap-4">
                        <div className="w-8 h-8 rounded-full bg-blue-100 border border-blue-200 flex items-center justify-center text-blue-600 font-medium text-sm">
                            JD
                        </div>
                    </div>
                </header>

                {/* Page Content */}
                <main className="flex-1 p-8">
                    <div className="max-w-7xl mx-auto">
                        {children}
                    </div>
                </main>
            </div>
        </div>
    );
};
