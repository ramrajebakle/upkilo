'use client';

import { useEffect, useState, use } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { locationsApi, Location, LocationRequest } from '@/lib/api.locations';
import LocationForm from '@/components/locations/LocationForm';
import { Loader2 } from 'lucide-react';

export default function EditLocationPage({ params }: { params: Promise<{ id: string }> }) {
    const router = useRouter();
    const [location, setLocation] = useState<Location | null>(null);
    const [loading, setLoading] = useState(true);
    const resolvedParams = use(params);

    useEffect(() => {
        const fetchLocation = async () => {
            try {
                const response = await locationsApi.get(resolvedParams.id);
                setLocation(response.data);
            } catch (error) {
                console.error('Failed to fetch location', error);
                toast.error('Failed to load location');
                router.push('/dashboard/locations');
            } finally {
                setLoading(false);
            }
        };

        fetchLocation();
    }, [resolvedParams.id, router]);

    const handleSubmit = async (data: LocationRequest) => {
        try {
            await locationsApi.update(resolvedParams.id, data);
            toast.success('Location updated successfully');
            router.push('/dashboard/locations');
        } catch (error) {
            console.error('Failed to update location', error);
            toast.error('Failed to update location');
        }
    };

    if (loading) {
        return (
            <div className="flex items-center justify-center h-64">
                <Loader2 className="w-8 h-8 animate-spin text-primary-500" />
            </div>
        );
    }

    if (!location) return null;

    // Transform Location to LocationRequest (match types if needed, or just pass as initialData)
    // The form handles matching fields. BusinessHours is string in both.
    const initialData: LocationRequest = {
        name: location.name,
        description: location.description || '',
        addressLine1: location.addressLine1 || '',
        addressLine2: location.addressLine2 || '',
        city: location.city || '',
        state: location.state || '',
        postalCode: location.postalCode || '',
        country: location.country || '',
        phone: location.phone || '',
        email: location.email || '',
        timezone: location.timezone,
        businessHours: location.businessHours || '',
        holidays: location.holidays || '',
        isActive: location.isActive
    };

    return (
        <LocationForm
            initialData={{ ...initialData, id: location.id }}
            onSubmit={handleSubmit}
            isEditing
        />
    );
}
