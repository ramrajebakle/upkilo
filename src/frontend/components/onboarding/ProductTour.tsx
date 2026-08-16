'use client';

import { useEffect, useRef } from 'react';
import Shepherd from 'shepherd.js';
import 'shepherd.js/dist/css/shepherd.css';

export function ProductTour() {
    const tourRef = useRef<any>(null);

    useEffect(() => {
        // Only run tour if it hasn't been completed/skipped
        const hasSeenTour = localStorage.getItem('upkilo_tour_seen');
        if (hasSeenTour) return;

        // Do not open on top of the cookie banner. A new tenant used to get three competing
        // layers on first paint — consent banner, onboarding checklist, and this tour's modal
        // overlay — with the tour dimming the checklist it was pointing at. The banner is a
        // legal blocker and cannot be deferred, so the tour yields to it and starts only once
        // consent has been answered. CookieConsent fires 'consent-updated' when that happens.
        let cancelled = false;
        let timer: ReturnType<typeof setTimeout> | undefined;

        const consentAnswered = () => localStorage.getItem('upkilo-consent') !== null;

        const startWhenClear = () => {
            if (cancelled || !consentAnswered()) return;
            window.removeEventListener('consent-updated', startWhenClear);
            // Long enough for the checklist above to settle, short enough to still read as a
            // response to arriving rather than an unrelated interruption.
            timer = setTimeout(() => { if (!cancelled) tour.start(); }, 1200);
        };

        tourRef.current = new Shepherd.Tour({
            useModalOverlay: true,
            defaultStepOptions: {
                // 'upkilo-tour' is the hook globals.css uses to override Shepherd's stock theme.
                // Shepherd puts .shepherd-button on every button itself, which has the same
                // specificity as .btn-primary — so whichever stylesheet loads last wins, and
                // shepherd.css (imported here, inside the component) loaded after globals.css.
                // That is why the buttons rendered in Shepherd's default #3288e6 blue instead
                // of the brand. The override is scoped and more specific so order stops mattering.
                classes: 'upkilo-tour shadow-md rounded-xl border-none p-4',
                scrollTo: { behavior: 'smooth', block: 'center' },
                cancelIcon: {
                    enabled: true
                }
            }
        });

        const tour = tourRef.current;

        // Any dismissal is final, not just reaching the last step. Previously only the Finish
        // button wrote the flag, so anyone who pressed Skip — or the X, or Escape — was shown
        // the whole tour again on the next page load, forever.
        const remember = () => localStorage.setItem('upkilo_tour_seen', 'true');
        tour.on('cancel', remember);
        tour.on('complete', remember);

        tour.addStep({
            id: 'welcome',
            text: 'Welcome to Upkilo! Let us show you around your new command center.',
            // No attachTo: this step introduces the product rather than pointing at one control,
            // so it is deliberately centred. It previously read `.group > Sparkles`, which is not
            // a valid selector — `Sparkles` is a React component name, and CSS has no way to match
            // one. Shepherd found nothing, silently fell back to an unattached step, and the
            // result looked like a bug rather than a decision. Now it is the decision.
            buttons: [
                {
                    text: 'Skip',
                    action: tour.cancel,
                    classes: 'btn btn-secondary text-xs mr-2'
                },
                {
                    text: 'Next',
                    action: tour.next,
                    classes: 'btn btn-primary text-xs'
                }
            ]
        });

        tour.addStep({
            id: 'navigation',
            text: 'This is your main navigation. Access all your business tools from here.',
            attachTo: {
                element: 'aside nav',
                on: 'right'
            },
            buttons: [
                {
                    text: 'Back',
                    action: tour.back,
                    classes: 'btn btn-secondary text-xs mr-2'
                },
                {
                    text: 'Next',
                    action: tour.next,
                    classes: 'btn btn-primary text-xs'
                }
            ]
        });

        tour.addStep({
            id: 'search',
            text: 'Quickly find anything—bookings, clients, or settings—using the global search.',
            attachTo: {
                element: '.GlobalSearch_trigger', // Assuming this class exists or I'll add it
                on: 'bottom'
            },
            buttons: [
                {
                    text: 'Back',
                    action: tour.back,
                    classes: 'btn btn-secondary text-xs mr-2'
                },
                {
                    text: 'Next',
                    action: tour.next,
                    classes: 'btn btn-primary text-xs'
                }
            ]
        });

        tour.addStep({
            id: 'notifications',
            text: 'Stay updated with real-time alerts for new bookings and system messages.',
            attachTo: {
                element: 'button[aria-label="Notifications"]',
                on: 'bottom'
            },
            buttons: [
                {
                    text: 'Back',
                    action: tour.back,
                    classes: 'btn btn-secondary text-xs mr-2'
                },
                {
                    text: 'Finish',
                    // The flag is written by the 'complete' handler above, which also covers
                    // Skip, the X and Escape. Setting it here as well would only re-introduce
                    // the assumption that finishing is the sole way out.
                    action: tour.complete,
                    classes: 'btn btn-primary text-xs'
                }
            ]
        });

        if (consentAnswered()) {
            startWhenClear();
        } else {
            window.addEventListener('consent-updated', startWhenClear);
        }

        return () => {
            cancelled = true;
            if (timer) clearTimeout(timer);
            window.removeEventListener('consent-updated', startWhenClear);
            // cancel() fires the 'cancel' handler, which would mark the tour as seen. Unmounting
            // on navigation is not the user dismissing anything, so detach first and let the
            // tour reappear on their next visit.
            if (tourRef.current) {
                tourRef.current.off('cancel', remember);
                tourRef.current.cancel();
            }
        };
    }, []);

    return null;
}
