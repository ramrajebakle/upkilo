import type { ReactNode } from 'react';
import '../globals.css';

export default function AuLayout({ children }: { children: ReactNode }) {
    return (
        <html lang="en-AU">
            <body>{children}</body>
        </html>
    );
}
