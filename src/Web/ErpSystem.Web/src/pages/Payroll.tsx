import { useState, useEffect } from 'react';
import { api } from '../services/api';
import {
    Wallet,
    Users,
    Calendar,
    CheckCircle2,
    Clock,
    FileText,
    TrendingUp,
    DollarSign,
    Plus,
    ChevronRight,
    AlertCircle,
    Calculator,
    BadgeCheck,
} from 'lucide-react';

// Types
interface SalaryStructure {
    id: string;
    name: string;
    description?: string;
    baseSalary: number;
    currency: string;
    isActive: boolean;
    totalEarnings: number;
    componentCount: number;
    deductionCount: number;
    createdAt: string;
}

interface PayrollRun {
    id: string;
    runNumber: string;
    year: number;
    month: number;
    paymentDate: string;
    description?: string;
    status: string;
    employeeCount: number;
    paidCount: number;
    totalGrossAmount: number;
    totalDeductions: number;
    totalNetAmount: number;
    createdAt: string;
    approvedAt?: string;
}

interface Payslip {
    id: string;
    payrollRunId: string;
    payslipNumber: string;
    employeeId: string;
    employeeName: string;
    year: number;
    month: number;
    grossAmount: number;
    totalDeductions: number;
    netAmount: number;
    status: string;
    paidAt?: string;
}

// Status badge
const StatusBadge = ({ status }: { status: string }) => {
    const getStyle = () => {
        switch (status) {
            case 'Draft': return 'bg-slate-500/20 text-slate-400 border border-slate-500/30';
            case 'Processing': return 'bg-blue-500/20 text-blue-400 border border-blue-500/30';
            case 'PendingApproval': return 'bg-amber-500/20 text-amber-400 border border-amber-500/30';
            case 'Approved': return 'bg-green-500/20 text-green-400 border border-green-500/30';
            case 'Paid': return 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30';
            case 'Finalized': return 'bg-cyan-500/20 text-cyan-400 border border-cyan-500/30';
            default: return 'bg-slate-500/20 text-slate-400 border border-slate-500/30';
        }
    };
    return <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${getStyle()}`}>{status}</span>;
};

// Stat card
const StatCard = ({ icon: Icon, label, value, subValue, color = 'blue' }: {
    icon: React.ElementType; label: string; value: string | number; subValue?: string; color?: 'blue' | 'green' | 'amber' | 'purple';
}) => {
    const colorClasses = {
        blue: 'from-blue-500/20 to-cyan-500/20 border-blue-500/30',
        green: 'from-green-500/20 to-emerald-500/20 border-green-500/30',
        amber: 'from-amber-500/20 to-orange-500/20 border-amber-500/30',
        purple: 'from-purple-500/20 to-pink-500/20 border-purple-500/30',
    };
    const iconColors = { blue: 'text-blue-400', green: 'text-green-400', amber: 'text-amber-400', purple: 'text-purple-400' };

    return (
        <div className={`bg-gradient-to-br ${colorClasses[color]} border rounded-xl p-4`}>
            <div className="flex items-center justify-between">
                <Icon className={`w-5 h-5 ${iconColors[color]}`} />
            </div>
            <div className="mt-3">
                <div className="text-2xl font-bold text-white">{value}</div>
                <div className="text-sm text-slate-400">{label}</div>
                {subValue && <div className="text-xs text-slate-500 mt-1">{subValue}</div>}
            </div>
        </div>
    );
};

// Mock data
const mockStructures: SalaryStructure[] = [
    { id: 's1', name: 'Senior Engineer', description: 'For senior development roles', baseSalary: 25000, currency: 'CNY', isActive: true, totalEarnings: 32000, componentCount: 4, deductionCount: 3, createdAt: '2026-01-01' },
    { id: 's2', name: 'Junior Developer', description: 'Entry level positions', baseSalary: 12000, currency: 'CNY', isActive: true, totalEarnings: 14500, componentCount: 3, deductionCount: 3, createdAt: '2026-01-01' },
    { id: 's3', name: 'Manager', description: 'Management positions', baseSalary: 35000, currency: 'CNY', isActive: true, totalEarnings: 45000, componentCount: 5, deductionCount: 3, createdAt: '2026-01-01' },
];

const mockRuns: PayrollRun[] = [
    { id: 'r1', runNumber: 'PAY-202602-ABC123', year: 2026, month: 2, paymentDate: '2026-02-28', status: 'PendingApproval', employeeCount: 45, paidCount: 0, totalGrossAmount: 1250000, totalDeductions: 312500, totalNetAmount: 937500, createdAt: '2026-02-20' },
    { id: 'r2', runNumber: 'PAY-202601-DEF456', year: 2026, month: 1, paymentDate: '2026-01-31', status: 'Paid', employeeCount: 43, paidCount: 43, totalGrossAmount: 1180000, totalDeductions: 295000, totalNetAmount: 885000, createdAt: '2026-01-20', approvedAt: '2026-01-25' },
];

const mockPayslips: Payslip[] = [
    { id: 'p1', payrollRunId: 'r1', payslipNumber: 'PS-202602-001', employeeId: 'e1', employeeName: '张明', year: 2026, month: 2, grossAmount: 32000, totalDeductions: 8000, netAmount: 24000, status: 'Finalized' },
    { id: 'p2', payrollRunId: 'r1', payslipNumber: 'PS-202602-002', employeeId: 'e2', employeeName: '李华', year: 2026, month: 2, grossAmount: 28000, totalDeductions: 7000, netAmount: 21000, status: 'Finalized' },
    { id: 'p3', payrollRunId: 'r1', payslipNumber: 'PS-202602-003', employeeId: 'e3', employeeName: '王芳', year: 2026, month: 2, grossAmount: 45000, totalDeductions: 11250, netAmount: 33750, status: 'Finalized' },
];

export default function Payroll() {
    const [activeTab, setActiveTab] = useState<'runs' | 'payslips' | 'structures'>('runs');
    const [structures, setStructures] = useState<SalaryStructure[]>(mockStructures);
    const [runs, setRuns] = useState<PayrollRun[]>(mockRuns);
    const [payslips, setPayslips] = useState<Payslip[]>(mockPayslips);

    const stats = {
        totalRuns: runs.length,
        pendingApproval: runs.filter(r => r.status === 'PendingApproval').length,
        totalPaid: runs.filter(r => r.status === 'Paid').reduce((sum, r) => sum + r.totalNetAmount, 0),
        employees: runs.length > 0 ? runs[0].employeeCount : 0
    };

    useEffect(() => {
        const fetchData = async () => {
            try {
                const [runsRes, payslipsRes] = await Promise.all([
                    api.get('/payroll/payroll-runs'),
                    api.get('/payroll/payslips')
                ]);
                if (runsRes.data?.items) setRuns(runsRes.data.items);
                if (payslipsRes.data?.items) setPayslips(payslipsRes.data.items);
            } catch (error) {
                console.log('Using mock data');
            }
        };
        fetchData();
    }, []);

    return (
        <div className="p-6 space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-white flex items-center gap-3">
                        <Wallet className="w-7 h-7 text-emerald-400" />
                        Payroll
                    </h1>
                    <p className="text-slate-400 mt-1">Manage salary structures, payroll runs, and payslips</p>
                </div>
                <button className="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg transition-colors">
                    <Plus className="w-4 h-4" /> New Payroll Run
                </button>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
                <StatCard icon={Calendar} label="Payroll Runs" value={stats.totalRuns} subValue="This year" color="blue" />
                <StatCard icon={AlertCircle} label="Pending Approval" value={stats.pendingApproval} color="amber" />
                <StatCard icon={DollarSign} label="Total Paid" value={`¥${(stats.totalPaid / 10000).toFixed(0)}万`} color="green" />
                <StatCard icon={Users} label="Employees" value={stats.employees} color="purple" />
            </div>

            {/* Tabs */}
            <div className="border-b border-slate-700">
                <nav className="flex gap-6">
                    {(['runs', 'payslips', 'structures'] as const).map((tab) => (
                        <button
                            key={tab}
                            onClick={() => setActiveTab(tab)}
                            className={`pb-3 px-1 border-b-2 transition-colors capitalize ${activeTab === tab ? 'border-emerald-500 text-white' : 'border-transparent text-slate-400 hover:text-slate-300'
                                }`}
                        >
                            {tab === 'runs' && <Calendar className="w-4 h-4 inline mr-2" />}
                            {tab === 'payslips' && <FileText className="w-4 h-4 inline mr-2" />}
                            {tab === 'structures' && <Calculator className="w-4 h-4 inline mr-2" />}
                            {tab === 'runs' ? 'Payroll Runs' : tab === 'payslips' ? 'Payslips' : 'Salary Structures'}
                        </button>
                    ))}
                </nav>
            </div>

            {/* Payroll Runs Tab */}
            {activeTab === 'runs' && (
                <div className="space-y-4">
                    {runs.map((run) => (
                        <div key={run.id} className="bg-slate-800/50 border border-slate-700/50 rounded-xl p-5 hover:border-emerald-500/50 transition-all">
                            <div className="flex items-center justify-between">
                                <div className="flex-1">
                                    <div className="flex items-center gap-3">
                                        <span className="font-mono text-sm text-slate-500">{run.runNumber}</span>
                                        <h3 className="text-lg font-semibold text-white">{run.year}年{run.month}月工资</h3>
                                        <StatusBadge status={run.status} />
                                    </div>
                                    <div className="flex items-center gap-6 mt-3 text-sm text-slate-400">
                                        <span className="flex items-center gap-2"><Users className="w-4 h-4" /> {run.employeeCount} employees</span>
                                        <span className="flex items-center gap-2"><Calendar className="w-4 h-4" /> Payment: {new Date(run.paymentDate).toLocaleDateString()}</span>
                                        {run.status === 'Paid' && <span className="flex items-center gap-2"><BadgeCheck className="w-4 h-4 text-green-400" /> {run.paidCount}/{run.employeeCount} paid</span>}
                                    </div>
                                </div>
                                <div className="text-right">
                                    <div className="text-2xl font-bold text-white">¥{(run.totalNetAmount / 10000).toFixed(1)}万</div>
                                    <div className="text-sm text-slate-400">Net Amount</div>
                                    <div className="text-xs text-slate-500 mt-1">Gross: ¥{(run.totalGrossAmount / 10000).toFixed(1)}万 | Ded: ¥{(run.totalDeductions / 10000).toFixed(1)}万</div>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Payslips Tab */}
            {activeTab === 'payslips' && (
                <div className="bg-slate-800/50 border border-slate-700/50 rounded-xl overflow-hidden">
                    <table className="w-full">
                        <thead className="bg-slate-900/50 border-b border-slate-700">
                            <tr>
                                <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Payslip</th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Employee</th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Period</th>
                                <th className="px-4 py-3 text-right text-xs font-medium text-slate-400 uppercase">Gross</th>
                                <th className="px-4 py-3 text-right text-xs font-medium text-slate-400 uppercase">Deductions</th>
                                <th className="px-4 py-3 text-right text-xs font-medium text-slate-400 uppercase">Net</th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Status</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-700/50">
                            {payslips.map((ps) => (
                                <tr key={ps.id} className="hover:bg-slate-700/30 transition-colors cursor-pointer">
                                    <td className="px-4 py-3"><span className="font-mono text-sm text-slate-300">{ps.payslipNumber}</span></td>
                                    <td className="px-4 py-3 text-white">{ps.employeeName}</td>
                                    <td className="px-4 py-3 text-slate-400">{ps.year}-{ps.month.toString().padStart(2, '0')}</td>
                                    <td className="px-4 py-3 text-right text-white">¥{ps.grossAmount.toLocaleString()}</td>
                                    <td className="px-4 py-3 text-right text-red-400">-¥{ps.totalDeductions.toLocaleString()}</td>
                                    <td className="px-4 py-3 text-right font-medium text-emerald-400">¥{ps.netAmount.toLocaleString()}</td>
                                    <td className="px-4 py-3"><StatusBadge status={ps.status} /></td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            {/* Salary Structures Tab */}
            {activeTab === 'structures' && (
                <div className="grid grid-cols-3 gap-4">
                    {structures.map((structure) => (
                        <div key={structure.id} className="bg-slate-800/50 border border-slate-700/50 rounded-xl p-5 hover:border-emerald-500/50 transition-all cursor-pointer">
                            <div className="flex items-center justify-between mb-3">
                                <h3 className="font-semibold text-white">{structure.name}</h3>
                                {structure.isActive && <span className="px-2 py-0.5 bg-green-500/20 text-green-400 text-xs rounded-full">Active</span>}
                            </div>
                            <p className="text-sm text-slate-400 mb-4">{structure.description}</p>
                            <div className="space-y-2">
                                <div className="flex justify-between text-sm">
                                    <span className="text-slate-400">Base Salary</span>
                                    <span className="text-white font-medium">¥{structure.baseSalary.toLocaleString()}</span>
                                </div>
                                <div className="flex justify-between text-sm">
                                    <span className="text-slate-400">Total Earnings</span>
                                    <span className="text-emerald-400 font-medium">¥{structure.totalEarnings.toLocaleString()}</span>
                                </div>
                                <div className="flex justify-between text-sm">
                                    <span className="text-slate-400">Components</span>
                                    <span className="text-white">{structure.componentCount} earning, {structure.deductionCount} deduction</span>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
