import {createNavigation} from 'next-intl/navigation';

export const locales = ['en', 'hi', 'es', 'fr', 'de', 'ar', 'ja', 'pt', 'it', 'ru', 'nl', 'tr', 'zh', 'ko', 'he'] as const;
export const localePrefix = 'always'; // Default could be 'as-needed'

export const {Link, redirect, usePathname, useRouter} =
  createNavigation({locales, localePrefix});
