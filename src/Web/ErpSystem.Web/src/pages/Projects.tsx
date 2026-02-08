import { useState, useEffect } from 'react';
import { api } from '../services/api';
import {
    FolderKanban,
    Calendar,
    Clock,
    CheckCircle2,
    AlertCircle,
    Users,
    TrendingUp,
    BarChart2,
    Layers,
    Plus,
    ChevronRight,
    Play,
    Pause,
    Flag,
    Target,
} from 'lucide-react';

// Types
interface Project {
    id: string;
    projectNumber: string;
    name: string;
    description?: string;
    type: string;
    status: string;
    startDate: string;
    endDate: string;
    plannedBudget: number;
    actualCost: number;
    currency: string;
    customerId?: string;
    projectManagerId: string;
    totalTasks: number;
    completedTasks: number;
    progressPercent: number;
    createdAt: string;
}

interface Task {
    id: string;
    projectId: string;
    taskNumber: string;
    title: string;
    description?: string;
    status: string;
    priority: string;
    dueDate?: string;
    assigneeId?: string;
    estimatedHours: number;
    actualHours: number;
    progressPercent: number;
    parentTaskId?: string;
    createdAt: string;
    completedAt?: string;
}

interface Timesheet {
    id: string;
    timesheetNumber: string;
    projectId: string;
    userId: string;
    weekStartDate: string;
    weekEndDate: string;
    status: string;
    totalHours: number;
    submittedAt?: string;
    approvedAt?: string;
}

// Status badge component
const StatusBadge = ({ status, type = 'project' }: { status: string; type?: string }) => {
    const getStyle = () => {
        if (type === 'project') {
            switch (status) {
                case 'Planning': return 'bg-blue-500/20 text-blue-400 border border-blue-500/30';
                case 'InProgress': return 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30';
                case 'Completed': return 'bg-green-500/20 text-green-400 border border-green-500/30';
                case 'OnHold': return 'bg-amber-500/20 text-amber-400 border border-amber-500/30';
                case 'Cancelled': return 'bg-red-500/20 text-red-400 border border-red-500/30';
                default: return 'bg-slate-500/20 text-slate-400 border border-slate-500/30';
            }
        } else if (type === 'task') {
            switch (status) {
                case 'Open': return 'bg-slate-500/20 text-slate-400 border border-slate-500/30';
                case 'InProgress': return 'bg-blue-500/20 text-blue-400 border border-blue-500/30';
                case 'InReview': return 'bg-purple-500/20 text-purple-400 border border-purple-500/30';
                case 'Completed': return 'bg-green-500/20 text-green-400 border border-green-500/30';
                default: return 'bg-slate-500/20 text-slate-400 border border-slate-500/30';
            }
        } else {
            switch (status) {
                case 'Draft': return 'bg-slate-500/20 text-slate-400 border border-slate-500/30';
                case 'Submitted': return 'bg-blue-500/20 text-blue-400 border border-blue-500/30';
                case 'Approved': return 'bg-green-500/20 text-green-400 border border-green-500/30';
                case 'Rejected': return 'bg-red-500/20 text-red-400 border border-red-500/30';
                default: return 'bg-slate-500/20 text-slate-400 border border-slate-500/30';
            }
        }
    };

    return (
        <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${getStyle()}`}>
            {status}
        </span>
    );
};

// Priority badge component
const PriorityBadge = ({ priority }: { priority: string }) => {
    const getStyle = () => {
        switch (priority) {
            case 'Critical': return 'bg-red-500/20 text-red-400';
            case 'High': return 'bg-orange-500/20 text-orange-400';
            case 'Medium': return 'bg-yellow-500/20 text-yellow-400';
            case 'Low': return 'bg-green-500/20 text-green-400';
            default: return 'bg-slate-500/20 text-slate-400';
        }
    };

    return (
        <span className={`px-2 py-0.5 rounded text-xs font-medium ${getStyle()}`}>
            {priority}
        </span>
    );
};

// Progress bar component
const ProgressBar = ({ percent, size = 'sm' }: { percent: number; size?: 'sm' | 'md' }) => {
    const height = size === 'sm' ? 'h-1.5' : 'h-2.5';

    return (
        <div className={`w-full bg-slate-700 rounded-full ${height} overflow-hidden`}>
            <div
                className={`${height} rounded-full transition-all duration-300 ${percent === 100 ? 'bg-green-500' : percent >= 50 ? 'bg-blue-500' : 'bg-amber-500'
                    }`}
                style={{ width: `${Math.min(percent, 100)}%` }}
            />
        </div>
    );
};

// Stat card component
const StatCard = ({
    icon: Icon,
    label,
    value,
    subValue,
    trend,
    color = 'blue'
}: {
    icon: React.ElementType;
    label: string;
    value: string | number;
    subValue?: string;
    trend?: string;
    color?: 'blue' | 'green' | 'amber' | 'purple';
}) => {
    const colorClasses = {
        blue: 'from-blue-500/20 to-cyan-500/20 border-blue-500/30',
        green: 'from-green-500/20 to-emerald-500/20 border-green-500/30',
        amber: 'from-amber-500/20 to-orange-500/20 border-amber-500/30',
        purple: 'from-purple-500/20 to-pink-500/20 border-purple-500/30',
    };

    const iconColors = {
        blue: 'text-blue-400',
        green: 'text-green-400',
        amber: 'text-amber-400',
        purple: 'text-purple-400',
    };

    return (
        <div className={`bg-gradient-to-br ${colorClasses[color]} border rounded-xl p-4`}>
            <div className="flex items-center justify-between">
                <Icon className={`w-5 h-5 ${iconColors[color]}`} />
                {trend && (
                    <span className="text-xs text-green-400 flex items-center gap-1">
                        <TrendingUp className="w-3 h-3" /> {trend}
                    </span>
                )}
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
const mockProjects: Project[] = [
    {
        id: '1', projectNumber: 'PRJ-20260208-001', name: 'ERP System Upgrade',
        description: 'Complete system upgrade to v2.0', type: 'Internal', status: 'InProgress',
        startDate: '2026-01-15', endDate: '2026-06-30', plannedBudget: 500000, actualCost: 180000,
        currency: 'CNY', projectManagerId: 'pm1', totalTasks: 45, completedTasks: 18, progressPercent: 40,
        createdAt: '2026-01-15'
    },
    {
        id: '2', projectNumber: 'PRJ-20260208-002', name: 'Customer Portal Development',
        description: 'Build customer-facing portal', type: 'External', status: 'Planning',
        startDate: '2026-03-01', endDate: '2026-08-31', plannedBudget: 800000, actualCost: 0,
        currency: 'CNY', customerId: 'c1', projectManagerId: 'pm2', totalTasks: 0, completedTasks: 0,
        progressPercent: 0, createdAt: '2026-02-01'
    },
    {
        id: '3', projectNumber: 'PRJ-20260208-003', name: 'Mobile App v3.0',
        description: 'Major mobile app redesign', type: 'Internal', status: 'InProgress',
        startDate: '2026-01-01', endDate: '2026-04-30', plannedBudget: 300000, actualCost: 210000,
        currency: 'CNY', projectManagerId: 'pm1', totalTasks: 32, completedTasks: 25, progressPercent: 78,
        createdAt: '2026-01-01'
    },
    {
        id: '4', projectNumber: 'PRJ-20260208-004', name: 'Infrastructure Maintenance',
        description: 'Quarterly maintenance cycle', type: 'Maintenance', status: 'OnHold',
        startDate: '2026-02-01', endDate: '2026-02-28', plannedBudget: 50000, actualCost: 15000,
        currency: 'CNY', projectManagerId: 'pm3', totalTasks: 12, completedTasks: 4, progressPercent: 33,
        createdAt: '2026-02-01'
    },
];

const mockTasks: Task[] = [
    { id: 't1', projectId: '1', taskNumber: 'TASK-001', title: 'Database Migration', status: 'Completed', priority: 'Critical', estimatedHours: 40, actualHours: 38, progressPercent: 100, createdAt: '2026-01-15', completedAt: '2026-01-25' },
    { id: 't2', projectId: '1', taskNumber: 'TASK-002', title: 'API Refactoring', status: 'InProgress', priority: 'High', dueDate: '2026-02-20', assigneeId: 'dev1', estimatedHours: 60, actualHours: 35, progressPercent: 65, createdAt: '2026-01-20' },
    { id: 't3', projectId: '1', taskNumber: 'TASK-003', title: 'UI Components Update', status: 'InReview', priority: 'Medium', dueDate: '2026-02-15', assigneeId: 'dev2', estimatedHours: 30, actualHours: 28, progressPercent: 95, createdAt: '2026-01-22' },
    { id: 't4', projectId: '1', taskNumber: 'TASK-004', title: 'Security Audit', status: 'Open', priority: 'High', dueDate: '2026-03-01', estimatedHours: 20, actualHours: 0, progressPercent: 0, createdAt: '2026-02-01' },
    { id: 't5', projectId: '3', taskNumber: 'TASK-005', title: 'App Store Submission', status: 'InProgress', priority: 'Critical', dueDate: '2026-04-25', assigneeId: 'dev3', estimatedHours: 8, actualHours: 3, progressPercent: 40, createdAt: '2026-04-15' },
];

const mockTimesheets: Timesheet[] = [
    { id: 'ts1', timesheetNumber: 'TS-20260203-001', projectId: '1', userId: 'dev1', weekStartDate: '2026-02-03', weekEndDate: '2026-02-09', status: 'Approved', totalHours: 40, submittedAt: '2026-02-09', approvedAt: '2026-02-10' },
    { id: 'ts2', timesheetNumber: 'TS-20260203-002', projectId: '1', userId: 'dev2', weekStartDate: '2026-02-03', weekEndDate: '2026-02-09', status: 'Approved', totalHours: 38, submittedAt: '2026-02-09', approvedAt: '2026-02-10' },
    { id: 'ts3', timesheetNumber: 'TS-20260210-001', projectId: '1', userId: 'dev1', weekStartDate: '2026-02-10', weekEndDate: '2026-02-16', status: 'Submitted', totalHours: 42, submittedAt: '2026-02-16' },
    { id: 'ts4', timesheetNumber: 'TS-20260210-002', projectId: '3', userId: 'dev3', weekStartDate: '2026-02-10', weekEndDate: '2026-02-16', status: 'Draft', totalHours: 25 },
];

export default function Projects() {
    const [activeTab, setActiveTab] = useState<'projects' | 'tasks' | 'timesheets'>('projects');
    const [projects, setProjects] = useState<Project[]>(mockProjects);
    const [tasks, setTasks] = useState<Task[]>(mockTasks);
    const [timesheets, setTimesheets] = useState<Timesheet[]>(mockTimesheets);
    const [selectedProject, setSelectedProject] = useState<string | null>(null);

    // Stats calculation
    const projectStats = {
        total: projects.length,
        inProgress: projects.filter(p => p.status === 'InProgress').length,
        completed: projects.filter(p => p.status === 'Completed').length,
        totalBudget: projects.reduce((sum, p) => sum + p.plannedBudget, 0),
        avgProgress: projects.length > 0 ? Math.round(projects.reduce((sum, p) => sum + p.progressPercent, 0) / projects.length) : 0
    };

    useEffect(() => {
        const fetchData = async () => {
            try {
                const [projectsRes, tasksRes, timesheetsRes] = await Promise.all([
                    api.get('/projects/projects'),
                    api.get('/projects/tasks'),
                    api.get('/projects/timesheets')
                ]);
                if (projectsRes.data?.items) setProjects(projectsRes.data.items);
                if (tasksRes.data?.items) setTasks(tasksRes.data.items);
                if (timesheetsRes.data?.items) setTimesheets(timesheetsRes.data.items);
            } catch (error) {
                console.log('Using mock data');
            }
        };
        fetchData();
    }, []);

    const getStatusIcon = (status: string) => {
        switch (status) {
            case 'Planning': return <Target className="w-4 h-4 text-blue-400" />;
            case 'InProgress': return <Play className="w-4 h-4 text-emerald-400" />;
            case 'Completed': return <CheckCircle2 className="w-4 h-4 text-green-400" />;
            case 'OnHold': return <Pause className="w-4 h-4 text-amber-400" />;
            default: return <AlertCircle className="w-4 h-4 text-slate-400" />;
        }
    };

    // Kanban columns for tasks
    const kanbanColumns = ['Open', 'InProgress', 'InReview', 'Completed'];

    return (
        <div className="p-6 space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-white flex items-center gap-3">
                        <FolderKanban className="w-7 h-7 text-indigo-400" />
                        Projects
                    </h1>
                    <p className="text-slate-400 mt-1">Manage projects, tasks, and timesheets</p>
                </div>
                <button className="flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg transition-colors">
                    <Plus className="w-4 h-4" /> New Project
                </button>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
                <StatCard
                    icon={FolderKanban}
                    label="Total Projects"
                    value={projectStats.total}
                    subValue={`${projectStats.inProgress} in progress`}
                    color="blue"
                />
                <StatCard
                    icon={CheckCircle2}
                    label="Completed"
                    value={projectStats.completed}
                    trend="+12%"
                    color="green"
                />
                <StatCard
                    icon={BarChart2}
                    label="Avg Progress"
                    value={`${projectStats.avgProgress}%`}
                    color="amber"
                />
                <StatCard
                    icon={Layers}
                    label="Total Budget"
                    value={`¥${(projectStats.totalBudget / 10000).toFixed(0)}万`}
                    color="purple"
                />
            </div>

            {/* Tabs */}
            <div className="border-b border-slate-700">
                <nav className="flex gap-6">
                    {(['projects', 'tasks', 'timesheets'] as const).map((tab) => (
                        <button
                            key={tab}
                            onClick={() => setActiveTab(tab)}
                            className={`pb-3 px-1 border-b-2 transition-colors capitalize ${activeTab === tab
                                    ? 'border-indigo-500 text-white'
                                    : 'border-transparent text-slate-400 hover:text-slate-300'
                                }`}
                        >
                            {tab === 'projects' && <FolderKanban className="w-4 h-4 inline mr-2" />}
                            {tab === 'tasks' && <CheckCircle2 className="w-4 h-4 inline mr-2" />}
                            {tab === 'timesheets' && <Clock className="w-4 h-4 inline mr-2" />}
                            {tab}
                        </button>
                    ))}
                </nav>
            </div>

            {/* Projects Tab */}
            {activeTab === 'projects' && (
                <div className="grid gap-4">
                    {projects.map((project) => (
                        <div
                            key={project.id}
                            className="bg-slate-800/50 border border-slate-700/50 rounded-xl p-5 hover:border-indigo-500/50 transition-all cursor-pointer"
                            onClick={() => setSelectedProject(selectedProject === project.id ? null : project.id)}
                        >
                            <div className="flex items-start justify-between">
                                <div className="flex-1">
                                    <div className="flex items-center gap-3">
                                        {getStatusIcon(project.status)}
                                        <h3 className="text-lg font-semibold text-white">{project.name}</h3>
                                        <StatusBadge status={project.status} />
                                        <span className="text-xs text-slate-500 font-mono">{project.projectNumber}</span>
                                    </div>
                                    <p className="text-slate-400 text-sm mt-2">{project.description}</p>

                                    <div className="flex items-center gap-6 mt-4 text-sm text-slate-400">
                                        <span className="flex items-center gap-2">
                                            <Calendar className="w-4 h-4" />
                                            {new Date(project.startDate).toLocaleDateString()} - {new Date(project.endDate).toLocaleDateString()}
                                        </span>
                                        <span className="flex items-center gap-2">
                                            <Users className="w-4 h-4" />
                                            {project.type}
                                        </span>
                                        <span className="flex items-center gap-2">
                                            <Flag className="w-4 h-4" />
                                            {project.completedTasks}/{project.totalTasks} tasks
                                        </span>
                                    </div>
                                </div>

                                <div className="text-right">
                                    <div className="text-2xl font-bold text-white">{project.progressPercent}%</div>
                                    <div className="w-32 mt-2">
                                        <ProgressBar percent={project.progressPercent} size="md" />
                                    </div>
                                    <div className="text-sm text-slate-400 mt-2">
                                        ¥{(project.actualCost / 10000).toFixed(1)}万 / ¥{(project.plannedBudget / 10000).toFixed(0)}万
                                    </div>
                                </div>
                            </div>

                            {selectedProject === project.id && (
                                <div className="mt-4 pt-4 border-t border-slate-700">
                                    <h4 className="text-sm font-medium text-slate-300 mb-3">Recent Tasks</h4>
                                    <div className="grid gap-2">
                                        {tasks.filter(t => t.projectId === project.id).slice(0, 3).map(task => (
                                            <div key={task.id} className="flex items-center justify-between bg-slate-900/50 rounded-lg p-3">
                                                <div className="flex items-center gap-3">
                                                    <span className="text-xs font-mono text-slate-500">{task.taskNumber}</span>
                                                    <span className="text-slate-300">{task.title}</span>
                                                    <PriorityBadge priority={task.priority} />
                                                </div>
                                                <div className="flex items-center gap-3">
                                                    <div className="w-20">
                                                        <ProgressBar percent={task.progressPercent} />
                                                    </div>
                                                    <StatusBadge status={task.status} type="task" />
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}

            {/* Tasks Tab - Kanban View */}
            {activeTab === 'tasks' && (
                <div className="grid grid-cols-4 gap-4">
                    {kanbanColumns.map((column) => (
                        <div key={column} className="bg-slate-800/30 rounded-xl p-4">
                            <div className="flex items-center justify-between mb-4">
                                <h3 className="font-medium text-white flex items-center gap-2">
                                    {column === 'Open' && <AlertCircle className="w-4 h-4 text-slate-400" />}
                                    {column === 'InProgress' && <Play className="w-4 h-4 text-blue-400" />}
                                    {column === 'InReview' && <Clock className="w-4 h-4 text-purple-400" />}
                                    {column === 'Completed' && <CheckCircle2 className="w-4 h-4 text-green-400" />}
                                    {column.replace(/([A-Z])/g, ' $1').trim()}
                                </h3>
                                <span className="text-xs bg-slate-700 text-slate-300 px-2 py-0.5 rounded-full">
                                    {tasks.filter(t => t.status === column).length}
                                </span>
                            </div>

                            <div className="space-y-3">
                                {tasks.filter(t => t.status === column).map((task) => (
                                    <div key={task.id} className="bg-slate-800 border border-slate-700 rounded-lg p-3 hover:border-indigo-500/50 transition-all cursor-pointer">
                                        <div className="flex items-center justify-between mb-2">
                                            <span className="text-xs font-mono text-slate-500">{task.taskNumber}</span>
                                            <PriorityBadge priority={task.priority} />
                                        </div>
                                        <h4 className="text-sm font-medium text-white mb-2">{task.title}</h4>
                                        <div className="flex items-center justify-between text-xs text-slate-400">
                                            <span className="flex items-center gap-1">
                                                <Clock className="w-3 h-3" />
                                                {task.estimatedHours}h
                                            </span>
                                            {task.dueDate && (
                                                <span className="flex items-center gap-1">
                                                    <Calendar className="w-3 h-3" />
                                                    {new Date(task.dueDate).toLocaleDateString()}
                                                </span>
                                            )}
                                        </div>
                                        {task.progressPercent > 0 && (
                                            <div className="mt-2">
                                                <ProgressBar percent={task.progressPercent} />
                                            </div>
                                        )}
                                    </div>
                                ))}
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {/* Timesheets Tab */}
            {activeTab === 'timesheets' && (
                <div className="space-y-4">
                    <div className="flex items-center justify-between">
                        <div className="flex gap-2">
                            {['All', 'Draft', 'Submitted', 'Approved', 'Rejected'].map((filter) => (
                                <button
                                    key={filter}
                                    className="px-3 py-1.5 text-sm rounded-lg bg-slate-800 text-slate-300 hover:bg-slate-700 transition-colors"
                                >
                                    {filter}
                                </button>
                            ))}
                        </div>
                        <button className="flex items-center gap-2 px-3 py-1.5 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-sm transition-colors">
                            <Plus className="w-4 h-4" /> New Timesheet
                        </button>
                    </div>

                    <div className="bg-slate-800/50 border border-slate-700/50 rounded-xl overflow-hidden">
                        <table className="w-full">
                            <thead className="bg-slate-900/50 border-b border-slate-700">
                                <tr>
                                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Timesheet</th>
                                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Project</th>
                                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Week</th>
                                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Hours</th>
                                    <th className="px-4 py-3 text-left text-xs font-medium text-slate-400 uppercase">Status</th>
                                    <th className="px-4 py-3 text-right text-xs font-medium text-slate-400 uppercase">Actions</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-700/50">
                                {timesheets.map((ts) => {
                                    const project = projects.find(p => p.id === ts.projectId);
                                    return (
                                        <tr key={ts.id} className="hover:bg-slate-700/30 transition-colors">
                                            <td className="px-4 py-3">
                                                <span className="font-mono text-sm text-slate-300">{ts.timesheetNumber}</span>
                                            </td>
                                            <td className="px-4 py-3">
                                                <span className="text-white">{project?.name || 'Unknown'}</span>
                                            </td>
                                            <td className="px-4 py-3">
                                                <span className="text-slate-400">
                                                    {new Date(ts.weekStartDate).toLocaleDateString()} - {new Date(ts.weekEndDate).toLocaleDateString()}
                                                </span>
                                            </td>
                                            <td className="px-4 py-3">
                                                <span className="text-white font-medium">{ts.totalHours}h</span>
                                            </td>
                                            <td className="px-4 py-3">
                                                <StatusBadge status={ts.status} type="timesheet" />
                                            </td>
                                            <td className="px-4 py-3 text-right">
                                                <button className="text-slate-400 hover:text-white transition-colors">
                                                    <ChevronRight className="w-5 h-5" />
                                                </button>
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}
        </div>
    );
}
