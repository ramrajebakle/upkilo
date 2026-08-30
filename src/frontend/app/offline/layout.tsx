import type { ReactNode } from 'react';
import '../globals.css';
import { RootHtml } from '@/components/layout/RootHtml';
import { themedViewport } from '../viewport';

export const viewport = themedViewport;

export default function OfflineLayout({ children }: { children: ReactNode }) {
  return (
    <RootHtml lang="en" bodyStyle={{ margin: 0 }}>
      {children}
    </RootHtml>
  );
}
