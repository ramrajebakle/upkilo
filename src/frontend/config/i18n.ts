export interface LocaleConfig {
  code: string;
  name: string;
  nativeName: string;
  dir: 'ltr' | 'rtl';
}

export const SUPPORTED_LOCALES: Record<string, LocaleConfig> = {
  en: { code: 'en', name: 'English', nativeName: 'English', dir: 'ltr' },
  es: { code: 'es', name: 'Spanish', nativeName: 'Español', dir: 'ltr' },
  fr: { code: 'fr', name: 'French', nativeName: 'Français', dir: 'ltr' },
  de: { code: 'de', name: 'German', nativeName: 'Deutsch', dir: 'ltr' },
  pt: { code: 'pt', name: 'Portuguese', nativeName: 'Português', dir: 'ltr' },
  ja: { code: 'ja', name: 'Japanese', nativeName: '日本語', dir: 'ltr' },
  zh: { code: 'zh', name: 'Chinese', nativeName: '中文', dir: 'ltr' },
  ko: { code: 'ko', name: 'Korean', nativeName: '한국어', dir: 'ltr' },
  ar: { code: 'ar', name: 'Arabic', nativeName: 'العربية', dir: 'rtl' },
  hi: { code: 'hi', name: 'Hindi', nativeName: 'हिन्दी', dir: 'ltr' },
  it: { code: 'it', name: 'Italian', nativeName: 'Italiano', dir: 'ltr' },
  nl: { code: 'nl', name: 'Dutch', nativeName: 'Nederlands', dir: 'ltr' },
  ru: { code: 'ru', name: 'Russian', nativeName: 'Русский', dir: 'ltr' },
  tr: { code: 'tr', name: 'Turkish', nativeName: 'Türkçe', dir: 'ltr' },
  he: { code: 'he', name: 'Hebrew', nativeName: 'עברית', dir: 'rtl' },
};

export const RTL_LOCALES = ['ar', 'he'];
