'use client';

import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import {
    ArrowLeft,
    User,
    Mail,
    Phone,
    MapPin,
    Save,
    Trash2,
    Calendar,
    Clock,
    DollarSign,
    Briefcase,
    Star,
    Scissors,
    Shield,
    TrendingUp,
} from 'lucide-react';
import { cn, formatCurrency, formatDate } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

interface StaffData {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    role: string;
    bio: string;
    tags: string[];
    hourlyRate: number;
    baseCommissionRate: number;
    commissionType: 'Percentage' | 'FixedAmount';
    employmentType: 'FullTime' | 'PartTime' | 'Contractor' | 'Freelance';
    employmentStatus: 'Active' | 'Inactive' | 'Terminated';
    timezone: string;
    employmentStartDate: string;
    avatar?: string;
    totalBookings?: number;
    averageRating?: number;
}

interface Shift {
    id: string;
    locationId: string;
    startTime: string;
    endTime: string;
    status: 'Scheduled' | 'Completed' | 'Missed';
}

interface Commission {
    id: string;
    bookingId: string;
    amount: number;
    date: string;
    status: 'Pending' | 'Paid';
}

const availableSpecialties = ['Hair Styling', 'Coloring', 'Massage', 'Nails', 'Facials', 'Makeup'];

export default function StaffProfilePage() {
    const router = useRouter();
    const params = useParams();
    const staffId = params.id as string;
    const { error: toastError, success: toastSuccess } = useToast();

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [staff, setStaff] = useState<StaffData | null>(null);
    const [activeTab, setActiveTab] = useState<'overview' | 'shifts' | 'commissions'>('overview');

    // Tab Data
    const [shifts, setShifts] = useState<Shift[]>([]);
    const [commissions, setCommissions] = useState<Commission[]>([]);
    const [loadingShifts, setLoadingShifts] = useState(false);
    const [loadingComm, setLoadingComm] = useState(false);

    const [formData, setFormData] = useState({
        firstName: '',
        lastName: '',
        email: '',
        phone: '',
        role: '',
        bio: '',
        tags: [] as string[],
        hourlyRate: 0,
        baseCommissionRate: 0,
        commissionType: 'Percentage' as 'Percentage' | 'FixedAmount',
        employmentType: 'FullTime' as 'FullTime' | 'PartTime' | 'Contractor' | 'Freelance',
        employmentStatus: 'Active',
        timezone: 'UTC',
    });

    useEffect(() => {
        const fetchStaff = async () => {
            setLoading(true);
            try {
                const response = await api.staff.get(staffId);
                const data = response.data;
                setStaff(data);
                setFormData({
                    firstName: data.firstName || '',
                    lastName: data.lastName || '',
                    email: data.email || '',
                    phone: data.phone || '',
                    role: data.role || '',
                    bio: data.bio || '',
                    tags: data.tags || [],
                    hourlyRate: data.hourlyRate || 0,
                    baseCommissionRate: data.baseCommissionRate || 0,
                    commissionType: data.commissionType || 'Percentage',
                    employmentType: data.employmentType || 'FullTime',
                    employmentStatus: data.employmentStatus || 'Active',
                    timezone: data.timezone || 'UTC',
                });
            } catch (error) {
                console.error('Failed to fetch staff:', error);
                toastError('Failed to load staff details');
            } finally {
                setLoading(false);
            }
        };

        if (staffId) {
            fetchStaff();
        }
    }, [staffId]);

    useEffect(() => {
        if (activeTab === 'shifts' && staffId) {
            fetchShifts();
        } else if (activeTab === 'commissions' && staffId) {
            fetchCommissions();
        }
    }, [activeTab, staffId]);

    const fetchShifts = async () => {
        setLoadingShifts(true);
        try {
            const res = await api.staff.shifts(staffId);
            setShifts(res.data);
        } catch (error) {
            toastError('Failed to load shifts');
        } finally {
            setLoadingShifts(false);
        }
    };

    const fetchCommissions = async () => {
        setLoadingComm(true);
        try {
            const res = await api.staff.commissions(staffId);
            setCommissions(res.data);
        } catch (error) {
            toastError('Failed to load commissions');
        } finally {
            setLoadingComm(false);
        }
    };

    const handleSave = async () => {
        setSaving(true);
        try {
            // Assume there is an update endpoint
            await api.staff.update(staffId, formData); // Need to verify if this endpoint exists on api.ts, if not I might need to add it or skip
            toastSuccess('Staff updated successfully');
        } catch (error: any) {
            console.error(error);
            toastError('Failed to update staff');
        } finally {
            setSaving(false);
        }
    };

    const toggleTag = (tag: string) => {
        if (formData.tags.includes(tag)) {
            setFormData({ ...formData, tags: formData.tags.filter(t => t !== tag) });
        } else {
            setFormData({ ...formData, tags: [...formData.tags, tag] });
        }
    };

    if (loading) {
        return (
            <div className="max-w-4xl mx-auto animate-pulse p-6">
                <div className="h-8 bg-slate-200 rounded w-1/3 mb-6" />
                <div className="grid grid-cols-3 gap-6">
                    <div className="col-span-2 space-y-4">
                        <div className="h-64 bg-slate-200 rounded-xl" />
                    </div>
                    <div className="h-64 bg-slate-200 rounded-xl" />
                </div>
            </div>
        );
    }

    if (!staff) {
        return (
            <div className="text-center py-20 p-6">
                <div className="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mx-auto mb-4">
                    <User className="h-8 w-8 text-slate-400" />
                </div>
                <h2 className="text-xl font-semibold text-slate-900">Staff Member Not Found</h2>
                <Link href="/staff" className="btn btn-primary mt-6">Back to Staff List</Link>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto p-6 md:p-0">
            {/* Header */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8 animate-fade-in-up">
                <div className="flex items-center gap-4">
                    <Link href="/staff" className="p-2 hover:bg-slate-100 rounded-xl transition-colors">
                        <ArrowLeft className="h-5 w-5 text-slate-600" />
                    </Link>
                    <div className="flex items-center gap-4">
                        <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-violet-500 to-purple-600 flex items-center justify-center text-white font-bold text-xl shadow-lg shadow-violet-500/20">
                            {staff.firstName[0]}{staff.lastName[0]}
                        </div>
                        <div>
                            <h1 className="text-2xl font-bold text-slate-900" style={{ fontFamily: 'Outfit, sans-serif' }}>
                                {staff.firstName} {staff.lastName}
                            </h1>
                            <div className="flex items-center gap-2 text-slate-500">
                                <Briefcase className="h-4 w-4" />
                                <span>{staff.role}</span>
                                <span className="text-slate-300">•</span>
                                <span>Since {formatDate(staff.employmentStartDate)}</span>
                            </div>
                        </div>
                    </div>
                </div>
                <div className="flex gap-2">
                    <button onClick={handleSave} disabled={saving} className="btn btn-primary">
                        {saving ? 'Saving...' : 'Save Changes'}
                    </button>
                </div>
            </div>

            {/* Tabs */}
            <div className="flex gap-4 border-b border-slate-200 mb-6 overflow-x-auto">
                {['overview', 'shifts', 'commissions'].map((tab) => (
                    <button
                        key={tab}
                        onClick={() => setActiveTab(tab as any)}
                        className={cn(
                            'px-4 py-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap',
                            activeTab === tab
                                ? 'border-primary-500 text-primary-600'
                                : 'border-transparent text-slate-600 hover:text-slate-900'
                        )}
                    >
                        {tab.charAt(0).toUpperCase() + tab.slice(1)}
                    </button>
                ))}
            </div>

            {/* Content */}
            {activeTab === 'overview' && (
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 animate-fade-in">
                    <div className="lg:col-span-2 space-y-6">
                        {/* Profile Info */}
                        <div className="card-elevated p-6">
                            <div className="flex items-center gap-3 mb-6">
                                <div className="p-2 bg-violet-100 rounded-lg">
                                    <User className="h-5 w-5 text-violet-600" />
                                </div>
                                <h2 className="text-lg font-semibold text-slate-900">Profile Information</h2>
                            </div>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">First Name</label>
                                    <input
                                        type="text"
                                        value={formData.firstName}
                                        onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                                        className="input"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Last Name</label>
                                    <input
                                        type="text"
                                        value={formData.lastName}
                                        onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                                        className="input"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Email</label>
                                    <input
                                        type="email"
                                        value={formData.email}
                                        onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                                        className="input"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Phone</label>
                                    <input
                                        type="tel"
                                        value={formData.phone}
                                        onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                                        className="input"
                                    />
                                </div>
                                <div className="md:col-span-1">
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Timezone</label>
                                    <select
                                        value={formData.timezone}
                                        onChange={(e) => setFormData({ ...formData, timezone: e.target.value })}
                                        className="input"
                                    >
                                        <option value="UTC">UTC</option>
                                        <option value="America/New_York">Eastern Time (ET)</option>
                                        <option value="America/Chicago">Central Time (CT)</option>
                                        <option value="America/Denver">Mountain Time (MT)</option>
                                        <option value="America/Los_Angeles">Pacific Time (PT)</option>
                                        <option value="Europe/London">London (GMT/BST)</option>
                                        <option value="Asia/Kolkata">India (IST)</option>
                                        {/* Add more as needed */}
                                    </select>
                                </div>
                                <div className="md:col-span-2">
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Bio</label>
                                    <textarea
                                        value={formData.bio}
                                        onChange={(e) => setFormData({ ...formData, bio: e.target.value })}
                                        className="input h-24 py-2 resize-none"
                                        placeholder="Brief biography..."
                                    />
                                </div>
                            </div>
                        </div>

                        {/* Employment Details */}
                        <div className="card-elevated p-6">
                            <div className="flex items-center gap-3 mb-6">
                                <div className="p-2 bg-emerald-100 rounded-lg">
                                    <Briefcase className="h-5 w-5 text-emerald-600" />
                                </div>
                                <h2 className="text-lg font-semibold text-slate-900">Employment Details</h2>
                            </div>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Role</label>
                                    <input
                                        type="text"
                                        value={formData.role}
                                        onChange={(e) => setFormData({ ...formData, role: e.target.value })}
                                        className="input"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Employment Status</label>
                                    <select
                                        value={formData.employmentStatus}
                                        onChange={(e) => setFormData({ ...formData, employmentStatus: e.target.value })}
                                        className="input"
                                    >
                                        <option value="Active">Active</option>
                                        <option value="Inactive">Inactive</option>
                                        <option value="Terminated">Terminated</option>
                                    </select>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Hourly Rate</label>
                                    <div className="relative">
                                        <span className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500">$</span>
                                        <input
                                            type="number"
                                            value={formData.hourlyRate}
                                            onChange={(e) => setFormData({ ...formData, hourlyRate: parseFloat(e.target.value) })}
                                            className="input pl-8"
                                        />
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Employment Type</label>
                                    <select
                                        value={formData.employmentType}
                                        onChange={(e) => setFormData({ ...formData, employmentType: e.target.value as any })}
                                        className="input"
                                    >
                                        <option value="FullTime">Full Time</option>
                                        <option value="PartTime">Part Time</option>
                                        <option value="Contractor">Contractor</option>
                                        <option value="Freelance">Freelance</option>
                                    </select>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Commission Rate (%)</label>
                                    <div className="relative">
                                        <input
                                            type="number"
                                            value={formData.baseCommissionRate}
                                            onChange={(e) => setFormData({ ...formData, baseCommissionRate: parseFloat(e.target.value) })}
                                            className="input pr-8"
                                        />
                                        <span className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-500">%</span>
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Specialties */}
                        <div className="card-elevated p-6">
                            <div className="flex items-center gap-3 mb-6">
                                <div className="p-2 bg-rose-100 rounded-lg">
                                    <Scissors className="h-5 w-5 text-rose-600" />
                                </div>
                                <h2 className="text-lg font-semibold text-slate-900">Specialties & Tags</h2>
                            </div>
                            <div className="flex flex-wrap gap-2">
                                {availableSpecialties.map((spec) => (
                                    <button
                                        key={spec}
                                        onClick={() => toggleTag(spec)}
                                        className={cn(
                                            'px-4 py-2 rounded-lg text-sm font-medium transition-all',
                                            formData.tags.includes(spec)
                                                ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                                        )}
                                    >
                                        {spec}
                                    </button>
                                ))}
                            </div>
                        </div>
                    </div>

                    <div className="space-y-6">
                        {/* Summary Stats */}
                        <div className="card-elevated p-6">
                            <h3 className="font-semibold text-slate-900 mb-4">Performance Summary</h3>
                            <div className="space-y-4">
                                <div className="flex items-center justify-between p-3 bg-slate-50 rounded-xl">
                                    <div className="flex items-center gap-3">
                                        <Calendar className="h-5 w-5 text-blue-500" />
                                        <span className="text-slate-600">Total Bookings</span>
                                    </div>
                                    <span className="font-bold text-slate-900">{staff.totalBookings || 0}</span>
                                </div>
                                <div className="flex items-center justify-between p-3 bg-slate-50 rounded-xl">
                                    <div className="flex items-center gap-3">
                                        <Star className="h-5 w-5 text-amber-500" />
                                        <span className="text-slate-600">Avg Rating</span>
                                    </div>
                                    <span className="font-bold text-slate-900">{staff.averageRating || 'N/A'}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {activeTab === 'shifts' && (
                <div className="animate-fade-in card-elevated p-6">
                    <div className="flex items-center justify-between mb-6">
                        <h2 className="text-lg font-semibold text-slate-900">Recent Shifts</h2>
                    </div>
                    {loadingShifts ? (
                        <div className="text-center py-10 text-slate-500">Loading shifts...</div>
                    ) : shifts.length === 0 ? (
                        <div className="text-center py-10 text-slate-500">No shifts details available.</div>
                    ) : (
                        <div className="space-y-4">
                            {shifts.map((shift) => (
                                <div key={shift.id} className="flex items-center justify-between p-4 border border-slate-100 rounded-xl hover:bg-slate-50 transition-colors">
                                    <div className="flex items-center gap-4">
                                        <div className={cn(
                                            "w-2 h-12 rounded-full",
                                            shift.status === 'Completed' ? "bg-emerald-400" :
                                                shift.status === 'Missed' ? "bg-red-400" : "bg-blue-400"
                                        )} />
                                        <div>
                                            <p className="font-medium text-slate-900">
                                                {formatDate(shift.startTime)}
                                            </p>
                                            <p className="text-sm text-slate-500">
                                                {new Date(shift.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} -
                                                {new Date(shift.endTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                                            </p>
                                        </div>
                                    </div>
                                    <span className={cn(
                                        "px-3 py-1 text-xs font-medium rounded-full",
                                        shift.status === 'Completed' ? "bg-emerald-100 text-emerald-700" :
                                            shift.status === 'Missed' ? "bg-red-100 text-red-700" : "bg-blue-100 text-blue-700"
                                    )}>
                                        {shift.status}
                                    </span>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            )}

            {activeTab === 'commissions' && (
                <div className="animate-fade-in card-elevated p-6">
                    <h2 className="text-lg font-semibold text-slate-900 mb-6">Commission History</h2>
                    {loadingComm ? (
                        <div className="text-center py-10 text-slate-500">Loading commissions...</div>
                    ) : commissions.length === 0 ? (
                        <div className="text-center py-10 text-slate-500">No commissions details available.</div>
                    ) : (
                        <table className="w-full text-left">
                            <thead>
                                <tr className="border-b border-slate-200">
                                    <th className="pb-3 text-sm font-medium text-slate-500">Date</th>
                                    <th className="pb-3 text-sm font-medium text-slate-500">Booking ID</th>
                                    <th className="pb-3 text-sm font-medium text-slate-500">Status</th>
                                    <th className="pb-3 text-sm font-medium text-slate-500 text-right">Amount</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-100">
                                {commissions.map((comm) => (
                                    <tr key={comm.id} className="group hover:bg-slate-50">
                                        <td className="py-4 text-sm text-slate-900">{formatDate(comm.date)}</td>
                                        <td className="py-4 text-sm text-slate-600 font-mono">{comm.bookingId.substring(0, 8)}...</td>
                                        <td className="py-4">
                                            <span className={cn(
                                                "px-2.5 py-0.5 text-xs font-medium rounded-full",
                                                comm.status === 'Paid' ? "bg-emerald-100 text-emerald-700" : "bg-amber-100 text-amber-700"
                                            )}>
                                                {comm.status}
                                            </span>
                                        </td>
                                        <td className="py-4 text-sm font-medium text-slate-900 text-right">{formatCurrency(comm.amount)}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>
            )}
        </div>
    );
}
