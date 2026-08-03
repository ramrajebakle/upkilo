import {notFound} from 'next/navigation';
import {getRequestConfig} from 'next-intl/server';
import {cookies} from 'next/headers';

// Supported locales
export const locales = ['en', 'hi', 'es', 'fr', 'de', 'ar', 'ja', 'pt', 'it', 'ru', 'nl', 'tr', 'zh', 'ko', 'he'] as const;
export type Locale = (typeof locales)[number];

type MessageTree = {[key: string]: string | MessageTree};

/**
 * Layers a locale's messages over the English set.
 *
 * Every non-English locale currently carries 5 of en.json's 14 top-level sections. All 15
 * locales are routable, so without a fallback a visitor on /de or /ja hits next-intl's
 * missing-message path and sees the raw key ("dashboard.title") rendered as body text.
 * Merging English underneath keeps whatever has genuinely been translated and quietly
 * falls back to English for the rest, which is the standard next-intl pattern for a
 * partially translated app.
 *
 * This is a presentation safety net, not a substitute for translation — ci.yml still
 * reports the per-locale gaps as warnings.
 */
function mergeWithFallback(base: MessageTree, override: MessageTree): MessageTree {
  const merged: MessageTree = {...base};

  for (const key of Object.keys(override)) {
    const overrideValue = override[key];
    const baseValue = merged[key];

    if (
      typeof overrideValue === 'object' && overrideValue !== null &&
      typeof baseValue === 'object' && baseValue !== null
    ) {
      merged[key] = mergeWithFallback(baseValue, overrideValue);
    } else if (overrideValue !== undefined) {
      merged[key] = overrideValue;
    }
  }

  return merged;
}

export default getRequestConfig(async ({requestLocale}) => {
  // In next-intl v4, requestLocale is a promise
  const locale = await requestLocale;
  const validLocale = (!locale || !locales.includes(locale as any)) ? 'en' : (locale as string);

  const cookieStore = cookies();
  const timeZone = (await cookieStore).get('timezone')?.value || 'UTC';

  const localeMessages = (await import(`../messages/${validLocale}.json`)).default;
  const messages = validLocale === 'en'
    ? localeMessages
    : mergeWithFallback(
        (await import('../messages/en.json')).default as MessageTree,
        localeMessages as MessageTree
      );

  return {
    locale: validLocale,
    timeZone,
    messages,
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
