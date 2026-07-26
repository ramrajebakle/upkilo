'use client';

import { locationsApi, LocationRequest } from '@/lib/api.locations';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import LocationForm from '@/components/locations/LocationForm';

export default function NewLocationPage() {
    const router = useRouter();

    const handleSubmit = async (data: LocationRequest) => {
        try {
            await locationsApi.create(data);
            toast.success('Location created successfully');
            router.push('/dashboard/locations');
        } catch (error) {
            console.error('Failed to create location', error);
            toast.error('Failed to create location');
        }
    };

    return <LocationForm onSubmit={handleSubmit} />;
}
