import type { ReactNode } from 'react';
import '../globals.css';

export default function CaLayout({ children }: { children: ReactNode }) {
    return (
        <html lang="en-CA">
            <body>{children}</body>
        </html>
    );
}
