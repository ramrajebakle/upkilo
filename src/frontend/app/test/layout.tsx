import type { ReactNode } from 'react';
import '../globals.css';
import { RootHtml } from '@/components/layout/RootHtml';
import { themedViewport } from '../viewport';

export const viewport = themedViewport;

export const metadata = {
  title: 'Upkilo — Test Route',
  description: 'Internal test route.',
  robots: { index: false, follow: false },
};

export default function TestLayout({ children }: { children: ReactNode }) {
  return <RootHtml lang="en">{children}</RootHtml>;
}
