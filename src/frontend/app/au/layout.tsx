import type { ReactNode } from 'react';
import '../globals.css';
import { RootHtml } from '@/components/layout/RootHtml';
import { themedViewport } from '../viewport';

export const viewport = themedViewport;

export default function AuLayout({ children }: { children: ReactNode }) {
    return <RootHtml lang="en-AU">{children}</RootHtml>;
}
