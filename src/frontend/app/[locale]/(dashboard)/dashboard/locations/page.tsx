'use client';

import { useEffect, useState } from 'react';
import {
    Plus, Search, MapPin, Phone, Mail, Clock,
    MoreVertical, Edit, Trash2, Users, Briefcase, Loader2
} from 'lucide-react';
import Link from 'next/link';
import { locationsApi, Location } from '@/lib/api.locations';
import { toast } from 'sonner';

export default function LocationsPage() {
    const [locations, setLocations] = useState<Location[]>([]);
    const [loading, setLoading] = useState(true);
    const [searchQuery, setSearchQuery] = useState('');

    useEffect(() => {
        fetchLocations();
    }, []);

    const fetchLocations = async () => {
        try {
            setLoading(true);
            const response = await locationsApi.getAll();
            setLocations(response.data);
        } catch (error) {
            console.error('Failed to fetch locations', error);
            toast.error('Failed to load locations');
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Are you sure you want to delete this location?')) return;

        try {
            await locationsApi.delete(id);
            toast.success('Location deleted');
            fetchLocations(); // Refresh list
        } catch (error) {
            console.error('Failed to delete location', error);
            toast.error('Failed to delete location');
        }
    };

    const filteredLocations = locations.filter(
        (location) =>
            location.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
            (location.city && location.city.toLowerCase().includes(searchQuery.toLowerCase()))
    );

    if (loading) {
        return (
            <div className="flex items-center justify-center h-64">
                <Loader2 className="w-8 h-8 animate-spin text-primary" />
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-foreground">Locations</h1>
                    <p className="text-foreground-secondary mt-1">Manage your business locations</p>
                </div>
                <Link
                    href="/dashboard/locations/new"
                    className="inline-flex items-center gap-2 bg-primary-500 hover:bg-primary-600 text-white px-4 py-2 rounded-lg font-medium transition-colors"
                >
                    <Plus className="h-5 w-5" />
                    Add Location
                </Link>
            </div>

            {/* Search */}
            <div className="relative max-w-md">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-foreground-muted" />
                <input
                    type="text"
                    placeholder="Search locations..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="w-full pl-10 pr-4 py-2 border border-border-strong rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500"
                />
            </div>

            {/* Locations grid */}
            <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
                {filteredLocations.map((location) => (
                    <div
                        key={location.id}
                        className="bg-card rounded-xl shadow-sm border border-border overflow-hidden"
                    >
                        {/* Color bar - using a default color or deriving from ID/Name if not present */}
                        <div className="h-2" style={{ backgroundColor: '#3B82F6' }} />

                        <div className="p-6">
                            {/* Header */}
                            <div className="flex items-start justify-between mb-4">
                                <div>
                                    <div className="flex items-center gap-2">
                                        <h3 className="font-semibold text-foreground">{location.name}</h3>
                                        {location.isPrimary && (
                                            <span className="text-xs bg-brand-subtle text-primary px-2 py-0.5 rounded-full">
                                                Primary
                                            </span>
                                        )}
                                    </div>
                                    <span
                                        className={`text-xs px-2 py-0.5 rounded-full ${location.isActive
                                            ? 'bg-green-100 text-green-700'
                                            : 'bg-muted text-foreground-secondary'
                                            }`}
                                    >
                                        {location.isActive ? 'Active' : 'Inactive'}
                                    </span>
                                </div>
                                <button className="p-1 hover:bg-accent rounded">
                                    <MoreVertical className="h-5 w-5 text-foreground-muted" />
                                </button>
                            </div>

                            {/* Details */}
                            <div className="space-y-3 text-sm">
                                <div className="flex items-start gap-3">
                                    <MapPin className="h-4 w-4 text-foreground-muted mt-0.5" />
                                    <div>
                                        <p className="text-foreground-secondary">{location.addressLine1}</p>
                                        {location.city && (
                                            <p className="text-foreground-secondary">
                                                {location.city}, {location.state}
                                            </p>
                                        )}
                                    </div>
                                </div>
                                {location.phone && (
                                    <div className="flex items-center gap-3">
                                        <Phone className="h-4 w-4 text-foreground-muted" />
                                        <p className="text-foreground-secondary">{location.phone}</p>
                                    </div>
                                )}
                                {location.email && (
                                    <div className="flex items-center gap-3">
                                        <Mail className="h-4 w-4 text-foreground-muted" />
                                        <p className="text-foreground-secondary">{location.email}</p>
                                    </div>
                                )}
                                <div className="flex items-center gap-3">
                                    <Clock className="h-4 w-4 text-foreground-muted" />
                                    <p className="text-foreground-secondary">{location.timezone}</p>
                                </div>
                            </div>

                            {/* Actions */}
                            <div className="flex gap-2 mt-4 pt-4 border-t border-border-subtle">
                                <Link
                                    href={`/dashboard/locations/${location.id}`}
                                    className="flex-1 text-center py-2 text-sm font-medium text-primary hover:bg-brand-subtle rounded-lg transition-colors"
                                >
                                    <Edit className="h-4 w-4 inline mr-1" />
                                    Edit
                                </Link>
                                <button
                                    onClick={() => handleDelete(location.id)}
                                    className="flex-1 text-center py-2 text-sm font-medium text-danger-fg hover:bg-red-50 rounded-lg transition-colors"
                                >
                                    <Trash2 className="h-4 w-4 inline mr-1" />
                                    Delete
                                </button>
                            </div>
                        </div>
                    </div>
                ))}
            </div>

            {/* Empty state */}
            {!loading && filteredLocations.length === 0 && (
                <div className="text-center py-12">
                    <MapPin className="h-12 w-12 text-gray-300 mx-auto mb-4" />
                    <h3 className="text-lg font-medium text-foreground mb-2">No locations found</h3>
                    <p className="text-foreground-secondary mb-4">
                        {searchQuery
                            ? 'Try adjusting your search'
                            : 'Add your first location to get started'}
                    </p>
                    <Link
                        href="/dashboard/locations/new"
                        className="inline-flex items-center gap-2 text-primary hover:text-primary font-medium"
                    >
                        <Plus className="h-5 w-5" />
                        Add Location
                    </Link>
                </div>
            )}
        </div>
    );
}
