import type { ReactNode } from 'react';
import '../globals.css';

export default function UaeLayout({ children }: { children: ReactNode }) {
    return (
        <html lang="en-AE">
            <body>{children}</body>
        </html>
    );
}
