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

        tourRef.current = new Shepherd.Tour({
            useModalOverlay: true,
            defaultStepOptions: {
                classes: 'shadow-md bg-slate-900 text-white rounded-xl border-none p-4',
                scrollTo: { behavior: 'smooth', block: 'center' },
                cancelIcon: {
                    enabled: true
                }
            }
        });

        const tour = tourRef.current;

        tour.addStep({
            id: 'welcome',
            text: 'Welcome to Upkilo! Let us show you around your new command center.',
            attachTo: {
                element: '.group > Sparkles', // Logo area
                on: 'bottom'
            },
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
                    action: () => {
                        localStorage.setItem('upkilo_tour_seen', 'true');
                        tour.complete();
                    },
                    classes: 'btn btn-primary text-xs'
                }
            ]
        });

        // Start tour after a short delay
        const timer = setTimeout(() => {
            tour.start();
        }, 2000);

        return () => {
            clearTimeout(timer);
            if (tourRef.current) {
                tourRef.current.cancel();
            }
        };
    }, []);

    return null;
}
