'use client';

import { useLocale, useTranslations } from 'next-intl';
import { useRouter, usePathname } from 'next/navigation';
import { Globe, ChevronDown } from 'lucide-react';
import { useState, useRef, useEffect } from 'react';
import { cn } from '@/lib/utils';

const locales = [
  { code: 'en', name: 'English', flag: '🇺🇸' },
  { code: 'es', name: 'Español', flag: '🇪🇸' },
  { code: 'hi', name: 'हिन्दी', flag: '🇮🇳' },
  { code: 'fr', name: 'Français', flag: '🇫🇷' },
  { code: 'de', name: 'Deutsch', flag: '🇩🇪' },
  { code: 'ar', name: 'العربية', flag: '🇸🇦' },
  { code: 'ja', name: '日本語', flag: '🇯🇵' },
  { code: 'pt', name: 'Português', flag: '🇧🇷' },
  { code: 'it', name: 'Italiano', flag: '🇮🇹' },
  { code: 'ru', name: 'Русский', flag: '🇷🇺' }
];

export function LanguageSwitcher() {
  const locale = useLocale();
  const router = useRouter();
  const pathname = usePathname();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const currentLocale = locales.find(l => l.code === locale) || locales[0];

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLocaleChange = (newLocale: string) => {
    // Replace the locale in the current path
    const pathParts = pathname.split('/');
    pathParts[1] = newLocale; 
    const newPath = pathParts.join('/');
    
    setIsOpen(false);
    router.push(newPath);
  };

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-gray-200 bg-white hover:bg-gray-50 transition-colors text-sm font-medium text-gray-700 shadow-sm"
      >
        <Globe className="h-4 w-4 text-gray-500" />
        <span className="hidden sm:inline">{currentLocale.name}</span>
        <span className="sm:hidden">{currentLocale.flag}</span>
        <ChevronDown className={cn("h-3 w-3 text-gray-400 transition-transform", isOpen && "rotate-180")} />
      </button>

      {isOpen && (
        <div className="absolute right-0 bottom-full mb-2 w-48 bg-white border border-gray-200 rounded-xl shadow-xl z-50 overflow-hidden animate-in fade-in slide-in-from-bottom-2 duration-200">
          <div className="py-1">
            {locales.map((l) => (
              <button
                key={l.code}
                onClick={() => handleLocaleChange(l.code)}
                className={cn(
                  "w-full flex items-center justify-between px-4 py-2 text-sm text-left hover:bg-gray-50 transition-colors",
                  locale === l.code ? "bg-primary-50 text-primary-700 font-bold" : "text-gray-700 font-medium"
                )}
              >
                <div className="flex items-center gap-3">
                  <span className="text-lg">{l.flag}</span>
                  <span>{l.name}</span>
                </div>
                {locale === l.code && <div className="w-1.5 h-1.5 rounded-full bg-primary-600" />}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
