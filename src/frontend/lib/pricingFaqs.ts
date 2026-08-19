/**
 * Pricing FAQs — the single source for both the visible accordion on the pricing page and the
 * FAQPage JSON-LD in its layout.
 *
 * These MUST stay one array. Google requires FAQPage structured data to match content actually
 * visible on the page; maintaining the questions twice is how the two drift into disagreement,
 * and structured data that promises an answer the page does not show is a manual-action risk,
 * not just a quality issue. The layout is a Server Component and the page is a Client Component,
 * so neither can own the data — hence this module.
 *
 * Every answer here is checked against behaviour that actually exists:
 *  - prices: PricingSeeder.cs (Starter 149 / Growth 499 USD, Enterprise IsCustom)
 *  - trial: TrialDays = 14 on Free/Starter/Growth, 30 on Enterprise
 *  - plan changes: SubscriptionService uses Stripe ProrationBehavior = "create_prorations"
 *  - cancellation: SubscriptionService cancels with CancelAtPeriodEnd = true
 *  - add-ons: PricingSeeder.SeedAddOnsAsync
 * Do not add an answer here that the product does not do — this is the text answer engines
 * quote verbatim, with our name attached.
 */
export interface PricingFaq {
  question: string;
  answer: string;
}

export const PRICING_FAQS: PricingFaq[] = [
  {
    question: 'How much does Upkilo cost?',
    answer:
      'Starter is $149 per month and Growth is $499 per month, both billed in USD. Enterprise is custom-priced and sales-led. Paying annually costs ten months instead of twelve, so Starter works out to $124 per month and Growth to $416 per month.',
  },
  {
    question: 'Is there a free trial, and do I need a credit card?',
    answer:
      'Yes. Every paid plan includes a 14-day free trial and no credit card is required to start. Enterprise trials run 30 days. There is also a permanently free plan for a single staff member.',
  },
  {
    question: 'What currency is Upkilo billed in?',
    answer:
      'All Upkilo subscriptions are billed in US Dollars (USD), exclusive of VAT, GST, sales tax and other applicable taxes. This is separate from what you charge your own clients, which settles through your connected Stripe account in your own currency.',
  },
  {
    question: 'Can I change plans or cancel later?',
    answer:
      'Yes. You can upgrade or downgrade at any time and the difference is prorated automatically. If you cancel, your access continues to the end of the billing period you have already paid for. Refunds are not issued for partial periods.',
  },
  {
    question: 'What happens if I outgrow my plan limits?',
    answer:
      'You add only what you need instead of jumping a tier. Extra staff seats, extra locations, AI credits and SMS credits are sold as add-ons on top of your existing plan, billed in USD.',
  },
  {
    question: 'Does Upkilo charge commission on bookings?',
    answer:
      'No. Upkilo charges a flat monthly subscription with no per-booking commission and no setup fee. Payment processing fees are charged separately by your payment provider, such as Stripe.',
  },
];
