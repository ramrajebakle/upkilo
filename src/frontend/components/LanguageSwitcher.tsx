'use client';

import React, { useState, useRef, useEffect } from 'react';
import { useLocale } from 'next-intl';
import { usePathname, useRouter } from '@/navigation';
import { SUPPORTED_LOCALES } from '@/config/i18n';

/**
 * Language switcher dropdown component.
 * Uses next-intl navigation for route-based locale switching.
 */
export function LanguageSwitcher() {
  const locale = useLocale();
  const router = useRouter();
  const pathname = usePathname();
  
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState('');
  const dropdownRef = useRef<HTMLDivElement>(null);

  const currentLocale = SUPPORTED_LOCALES[locale] || SUPPORTED_LOCALES['en'];
  const localeList = Object.values(SUPPORTED_LOCALES);

  const filtered = search
    ? localeList.filter(l =>
        l.name.toLowerCase().includes(search.toLowerCase()) ||
        l.nativeName.toLowerCase().includes(search.toLowerCase()) ||
        l.code.toLowerCase().includes(search.toLowerCase())
      )
    : localeList;

  // Close on outside click
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setIsOpen(false);
        setSearch('');
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSelect = (code: string) => {
    // next-intl's router.replace handles transition while preserving path and params
    router.replace(pathname, { locale: code });
    setIsOpen(false);
    setSearch('');
  };

  return (
    <div ref={dropdownRef} style={{ position: 'relative', display: 'inline-block' }}>
      <button
        id="language-switcher-btn"
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        aria-label="Select language"
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '8px',
          padding: '8px 16px',
          borderRadius: '12px',
          border: '1px solid rgba(255,255,255,0.1)',
          background: 'rgba(255,255,255,0.05)',
          backdropFilter: 'blur(10px)',
          color: '#f8fafc',
          cursor: 'pointer',
          fontSize: '14px',
          fontWeight: 500,
          transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
          boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
        }}
        onMouseEnter={(e) => {
          (e.currentTarget as HTMLElement).style.background = 'rgba(255,255,255,0.1)';
          (e.currentTarget as HTMLElement).style.borderColor = 'rgba(255,255,255,0.2)';
        }}
        onMouseLeave={(e) => {
          (e.currentTarget as HTMLElement).style.background = 'rgba(255,255,255,0.05)';
          (e.currentTarget as HTMLElement).style.borderColor = 'rgba(255,255,255,0.1)';
        }}
      >
        <span style={{ fontSize: '18px' }}>🌐</span>
        <span>{currentLocale.nativeName}</span>
        <svg 
            width="12" 
            height="12" 
            viewBox="0 0 24 24" 
            fill="none" 
            stroke="currentColor" 
            strokeWidth="2" 
            strokeLinecap="round" 
            strokeLinejoin="round"
            style={{ 
                transform: isOpen ? 'rotate(180deg)' : 'rotate(0)',
                transition: 'transform 0.3s ease',
                opacity: 0.7
            }}
        >
            <path d="m6 9 6 6 6-6"/>
        </svg>
      </button>

      {isOpen && (
        <div
          role="listbox"
          aria-label="Available languages"
          style={{
            position: 'absolute',
            top: 'calc(100% + 8px)',
            right: 0,
            width: '280px',
            maxHeight: '400px',
            overflowY: 'auto',
            background: 'rgba(15, 23, 42, 0.95)',
            backdropFilter: 'blur(20px)',
            border: '1px solid rgba(255,255,255,0.1)',
            borderRadius: '16px',
            boxShadow: '0 20px 50px rgba(0,0,0,0.5)',
            zIndex: 9999,
            padding: '8px',
            animation: 'dropdownIn 0.3s ease-out forwards',
          }}
        >
          <style dangerouslySetInnerHTML={{ __html: `
            @keyframes dropdownIn {
              from { opacity: 0; transform: translateY(-10px); }
              to { opacity: 1; transform: translateY(0); }
            }
            .lang-option:hover { background: rgba(99, 102, 241, 0.1) !important; }
            .lang-option.active { background: rgba(99, 102, 241, 0.2) !important; color: #818cf8 !important; }
          `}} />

          {/* Search input */}
          <div style={{ padding: '8px', position: 'sticky', top: 0, zIndex: 1, background: 'rgba(15, 23, 42, 0.95)' }}>
            <div style={{ position: 'relative' }}>
                <input
                type="text"
                placeholder="Search language..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                autoFocus
                style={{
                    width: '100%',
                    padding: '10px 12px',
                    borderRadius: '10px',
                    border: '1px solid rgba(255,255,255,0.1)',
                    background: 'rgba(255,255,255,0.05)',
                    color: '#f8fafc',
                    fontSize: '13px',
                    outline: 'none',
                    transition: 'border-color 0.2s ease',
                }}
                onFocus={(e) => e.target.style.borderColor = 'rgba(99, 102, 241, 0.5)'}
                onBlur={(e) => e.target.style.borderColor = 'rgba(255,255,255,0.1)'}
                />
            </div>
          </div>

          <div style={{ marginTop: '4px' }}>
            {filtered.map((l) => (
                <button
                key={l.code}
                role="option"
                aria-selected={l.code === locale}
                onClick={() => handleSelect(l.code)}
                className={`lang-option ${l.code === locale ? 'active' : ''}`}
                style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    width: '100%',
                    padding: '10px 12px',
                    border: 'none',
                    background: 'transparent',
                    color: '#cbd5e1',
                    cursor: 'pointer',
                    borderRadius: '10px',
                    fontSize: '14px',
                    textAlign: 'left',
                    transition: 'all 0.2s ease',
                }}
                >
                <div style={{ display: 'flex', flexDirection: 'column' }}>
                    <span style={{ fontWeight: 600 }}>{l.nativeName}</span>
                    <span style={{ fontSize: '11px', opacity: 0.5 }}>{l.name}</span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    {l.dir === 'rtl' && (
                        <span style={{ 
                            fontSize: '9px', 
                            padding: '2px 6px', 
                            borderRadius: '4px', 
                            background: 'rgba(255,255,255,0.1)',
                            opacity: 0.6
                        }}>RTL</span>
                    )}
                    {l.code === locale && <span style={{ color: '#818cf8', fontWeight: 'bold' }}>✓</span>}
                </div>
                </button>
            ))}
          </div>

          {filtered.length === 0 && (
            <div style={{ padding: '24px 12px', textAlign: 'center', opacity: 0.5, fontSize: '13px' }}>
              No languages matching "{search}"
            </div>
          )}
        </div>
      )}
    </div>
  );
}
