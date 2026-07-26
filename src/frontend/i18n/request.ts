import {notFound} from 'next/navigation';
import {getRequestConfig} from 'next-intl/server';
import {cookies} from 'next/headers';

// Supported locales
export const locales = ['en', 'hi', 'es', 'fr', 'de', 'ar', 'ja', 'pt', 'it', 'ru', 'nl', 'tr', 'zh', 'ko', 'he'] as const;
export type Locale = (typeof locales)[number];

export default getRequestConfig(async ({requestLocale}) => {
  // In next-intl v4, requestLocale is a promise
  const locale = await requestLocale;
  const validLocale = (!locale || !locales.includes(locale as any)) ? 'en' : (locale as string);

  const cookieStore = cookies();
  const timeZone = (await cookieStore).get('timezone')?.value || 'UTC';

  return {
    locale: validLocale,
    timeZone,
    messages: (await import(`../messages/${validLocale}.json`)).default,
    formats: {
      dateTime: {
        short: {
          day: 'numeric',
          month: 'short',
          year: 'numeric'
        },
        long: {
          day: 'numeric',
          month: 'long',
          year: 'numeric',
          hour: 'numeric',
          minute: 'numeric'
        },
        timeOnly: {
          hour: 'numeric',
          minute: 'numeric'
        }
      },
      number: {
        currency: {
          style: 'currency',
          currency: 'USD' // Default, can be overridden per call
        },
        precise: {
          style: 'decimal',
          maximumFractionDigits: 2
        },
        percent: {
          style: 'percent',
          maximumFractionDigits: 1
        }
      },
      list: {
        enumeration: {
          style: 'long',
          type: 'conjunction'
        }
      }
    }
  };
});
