'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Label } from '@/components/ui/Label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/Select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/Table';
import { Trash2, Plus, GripVertical } from 'lucide-react';
import { toast } from 'sonner';
import { apiClient } from '@/lib/api';

interface CustomField {
    id: string;
    name: string;
    key: string;
    type: string;
    targetEntity: string;
    isRequired: boolean;
    options?: string;
}

export default function CustomFieldsPage() {
    const [fields, setFields] = useState<CustomField[]>([]);
    const [loading, setLoading] = useState(true);
    const [newField, setNewField] = useState<Partial<CustomField>>({
        name: '',
        type: 'text',
        targetEntity: 'client',
        isRequired: false
    });

    useEffect(() => {
        fetchFields();
    }, []);

    const fetchFields = async () => {
        try {
            const response = await apiClient.get('/api/v1/custom-fields');
            setFields(response.data);
        } catch (error) {
            toast.error('Failed to load custom fields');
        } finally {
            setLoading(false);
        }
    };

    const handleCreate = async () => {
        if (!newField.name) return toast.error('Name is required');

        try {
            const key = newField.name.toLowerCase().replace(/\\s+/g, '_');
            await apiClient.post('/api/v1/custom-fields', {
                ...newField,
                key: key
            });
            toast.success('Field created');
            setNewField({ name: '', type: 'text', targetEntity: 'client', isRequired: false });
            fetchFields();
        } catch (error) {
            toast.error('Failed to create field');
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Are you sure? Data for this field may be lost.')) return;
        try {
            await apiClient.delete(`/api/v1/custom-fields/${id}`);
            toast.success('Field deleted');
            setFields(fields.filter(f => f.id !== id));
        } catch (error) {
            toast.error('Failed to delete field');
        }
    };

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-3xl font-bold tracking-tight">Custom Fields</h2>
                    <p className="text-muted-foreground">Capture extra information for your business.</p>
                </div>
            </div>

            <div className="grid gap-6 md:grid-cols-3">
                {/* Create Field Form */}
                <Card className="md:col-span-1 h-fit">
                    <CardHeader>
                        <CardTitle>Add New Field</CardTitle>
                        <CardDescription>Define a new custom attribute.</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="space-y-2">
                            <Label>Field Name</Label>
                            <Input
                                value={newField.name}
                                onChange={e => setNewField({ ...newField, name: e.target.value })}
                                placeholder="e.g., Insurance Provider"
                            />
                        </div>

                        <div className="space-y-2">
                            <Label>Applies To</Label>
                            <Select
                                value={newField.targetEntity}
                                onValueChange={v => setNewField({ ...newField, targetEntity: v })}
                            >
                                <SelectTrigger><SelectValue /></SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="client">Client</SelectItem>
                                    <SelectItem value="booking">Booking</SelectItem>
                                    <SelectItem value="location">Location</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>

                        <div className="space-y-2">
                            <Label>Data Type</Label>
                            <Select
                                value={newField.type}
                                onValueChange={v => setNewField({ ...newField, type: v })}
                            >
                                <SelectTrigger><SelectValue /></SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="text">Text</SelectItem>
                                    <SelectItem value="number">Number</SelectItem>
                                    <SelectItem value="date">Date</SelectItem>
                                    <SelectItem value="checkbox">Yes/No</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>

                        <Button className="w-full" onClick={handleCreate}>
                            <Plus className="mr-2 h-4 w-4" /> Add Field
                        </Button>
                    </CardContent>
                </Card>

                {/* Fields List */}
                <Card className="md:col-span-2">
                    <CardHeader>
                        <CardTitle>Active Fields</CardTitle>
                    </CardHeader>
                    <CardContent>
                        {loading ? (
                            <div className="text-center py-8">Loading...</div>
                        ) : fields.length === 0 ? (
                            <div className="text-center py-8 text-muted-foreground">No custom fields defined yet.</div>
                        ) : (
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead>Field Name</TableHead>
                                        <TableHead>Target</TableHead>
                                        <TableHead>Type</TableHead>
                                        <TableHead className="w-[50px]"></TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {fields.map((field) => (
                                        <TableRow key={field.id}>
                                            <TableCell className="font-medium">{field.name}</TableCell>
                                            <TableCell className="capitalize">{field.targetEntity}</TableCell>
                                            <TableCell className="capitalize">{field.type}</TableCell>
                                            <TableCell>
                                                <Button variant="ghost" size="sm" onClick={() => handleDelete(field.id)}>
                                                    <Trash2 className="h-4 w-4 text-red-500" />
                                                </Button>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        )}
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
