import type { ReactNode } from 'react';
import '../globals.css';

export default function UkLayout({ children }: { children: ReactNode }) {
    return (
        <html lang="en-GB">
            <body>{children}</body>
        </html>
    );
}
