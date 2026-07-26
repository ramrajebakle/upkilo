import { useEffect, useRef, useState } from 'react';

type SaveStatus = 'idle' | 'saving' | 'saved' | 'error';

/**
 * Debounced auto-save hook for long settings forms.
 * Calls `saveFn` after `delay` ms of no changes.
 * Returns `{ status, lastSaved }` for display in UI.
 */
export function useAutoSave<T>(
    value: T,
    saveFn: (value: T) => Promise<void>,
    options: { delay?: number; enabled?: boolean } = {}
): { status: SaveStatus; lastSaved: Date | null } {
    const { delay = 1500, enabled = true } = options;
    const [status, setStatus] = useState<SaveStatus>('idle');
    const [lastSaved, setLastSaved] = useState<Date | null>(null);
    const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
    const isFirstRender = useRef(true);

    useEffect(() => {
        if (isFirstRender.current) {
            isFirstRender.current = false;
            return;
        }
        if (!enabled) return;

        if (timerRef.current) clearTimeout(timerRef.current);
        setStatus('idle');

        timerRef.current = setTimeout(async () => {
            setStatus('saving');
            try {
                await saveFn(value);
                setStatus('saved');
                setLastSaved(new Date());
                // Reset to idle after 3s so UI doesn't stay "Saved" forever
                setTimeout(() => setStatus('idle'), 3000);
            } catch {
                setStatus('error');
            }
        }, delay);

        return () => {
            if (timerRef.current) clearTimeout(timerRef.current);
        };
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [value, delay, enabled]);

    return { status, lastSaved };
}
