'use client';

import { useLocale } from 'next-intl';
import { locales, usePathname, useRouter } from '../navigation';
import { Globe, ChevronDown } from 'lucide-react';
import { useState, useRef, useEffect } from 'react';
import { cn } from '@/lib/utils';
import { SUPPORTED_LOCALES } from '@/config/i18n';

const FLAGS: Record<string, string> = {
  en: '🇺🇸',
  es: '🇪🇸',
  hi: '🇮🇳',
  fr: '🇫🇷',
  de: '🇩🇪',
  ar: '🇸🇦',
  ja: '🇯🇵',
  pt: '🇧🇷',
  it: '🇮🇹',
  ru: '🇷🇺',
  he: '🇮🇱',
  nl: '🇳🇱',
  tr: '🇹🇷',
  zh: '🇨🇳',
  ko: '🇰🇷'
};

export default function LocaleSwitcher() {
  const locale = useLocale();
  const router = useRouter();
  const pathname = usePathname();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const activeLocaleConfig = SUPPORTED_LOCALES[locale] || SUPPORTED_LOCALES['en'];
  const currentLocale = {
    code: activeLocaleConfig.code,
    name: activeLocaleConfig.nativeName,
    flag: FLAGS[activeLocaleConfig.code] || '🌐'
  };

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
    setIsOpen(false);
    router.replace(pathname, { locale: newLocale as any });
  };

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-slate-200 dark:border-white/5 bg-white dark:bg-slate-900 hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors text-sm font-medium text-slate-700 dark:text-slate-300 shadow-sm"
      >
        <Globe className="h-4 w-4 text-slate-500 dark:text-slate-400" />
        <span className="hidden sm:inline">{currentLocale.name}</span>
        <span className="sm:hidden">{currentLocale.flag}</span>
        <ChevronDown className={cn("h-3 w-3 text-foreground-muted transition-transform", isOpen && "rotate-180")} />
      </button>

      {isOpen && (
        <div className="absolute right-0 top-full mt-2 w-48 bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/10 rounded-xl shadow-xl z-50 overflow-hidden animate-in fade-in slide-in-from-top-2 duration-200">
          <div className="py-1 max-h-80 overflow-y-auto">
            {locales.map((locCode) => {
              const locConfig = SUPPORTED_LOCALES[locCode];
              if (!locConfig) return null;
              
              return (
                <button
                  key={locCode}
                  onClick={() => handleLocaleChange(locCode)}
                  className={cn(
                    "w-full flex items-center justify-between px-4 py-2 text-sm text-left hover:bg-slate-50 dark:hover:bg-white/5 transition-colors",
                    locale === locCode ? "bg-primary-50 dark:bg-primary-500/10 text-primary-700 dark:text-primary-400 font-bold" : "text-slate-700 dark:text-slate-300 font-medium"
                  )}
                >
                  <div className="flex items-center gap-3">
                    <span className="text-lg">{FLAGS[locCode] || '🌐'}</span>
                    <span>{locConfig.nativeName}</span>
                  </div>
                  {locale === locCode && <div className="w-1.5 h-1.5 rounded-full bg-primary-600" />}
                </button>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
