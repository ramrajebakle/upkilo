"use client";

import React, { useState, useEffect } from "react";
import { Check, CheckCircle2, Calendar, Clock, User, ArrowRight, ArrowLeft, Loader2, AlertTriangle, CreditCard } from "lucide-react";
import { Card, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { loadRazorpayCheckout } from "@/lib/razorpay";
import { currencySymbol } from "@/lib/currency";

interface Service {
  id: string;
  name: string;
  duration: number;
  price: number;
}

interface Business {
  name: string;
  primaryColor?: string;
}

export function BookingWizard({
  tenantSlug,
  preselectServiceId,
  business,
}: {
  tenantSlug: string;
  preselectServiceId?: string;
  business?: Business;
}) {
  const [step, setStep] = useState(1);
  const [services, setServices] = useState<Service[]>([]);
  const [loadingServices, setLoadingServices] = useState(true);
  const [selectedService, setSelectedService] = useState<Service | null>(null);
  const [selectedDate, setSelectedDate] = useState<string | null>(null);
  const [selectedTime, setSelectedTime] = useState<string | null>(null);
  const [availableSlots, setAvailableSlots] = useState<any[]>([]);
  const [loadingSlots, setLoadingSlots] = useState(false);

  const [contact, setContact] = useState({ firstName: "", lastName: "", email: "", phone: "", notes: "" });
  const [bookingResult, setBookingResult] = useState<any>(null);
  const [submitting, setSubmitting] = useState(false);
  const [conflicts, setConflicts] = useState<{ hasConflict: boolean; message: string } | null>(null);
  const [checkingConflict, setCheckingConflict] = useState(false);

  // Stripe activation is blocked (unregistered business entity), so the booking response can
  // come back with requiresPayment=true and clientSecret=null — see PublicBookingController's
  // CreateBooking. awaitingPayment renders a Razorpay "Pay Now" panel INSIDE the existing step-4
  // content instead of introducing a numbered step 5: the stepper header, nextStep/prevStep
  // clamp, and footer nav all hardcode `step === 4` as "final step" in several places, and a
  // boolean flag branching within that same step avoids touching any of them.
  const [awaitingPayment, setAwaitingPayment] = useState(false);
  const [payingNow, setPayingNow] = useState(false);
  const [paymentError, setPaymentError] = useState<string | null>(null);

  const handleConfirmBooking = async () => {
    setSubmitting(true);
    try {
      const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
      const res = await fetch(`${API_URL}/api/booking/${tenantSlug}/book`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          serviceId: selectedService?.id,
          date: selectedDate,
          time: selectedTime,
          ...contact
        })
      });
      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || 'Booking failed');
      }
      const data = await res.json();
      setBookingResult(data);
      // data.clientSecret is only ever populated by a configured Stripe — this frontend has no
      // Stripe SDK today, so that combination (requiresPayment && clientSecret) intentionally
      // falls through unchanged, exactly as it did before this file had any payment UI at all.
      if (data.requiresPayment && !data.clientSecret) {
        setAwaitingPayment(true);
      }
      nextStep();
    } catch (err: any) {
      alert(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const handlePayNow = async () => {
    if (!bookingResult?.id) return;
    setPayingNow(true);
    setPaymentError(null);
    try {
      await loadRazorpayCheckout();

      const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
      const orderRes = await fetch(`${API_URL}/api/booking/${tenantSlug}/razorpay/order`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ bookingId: bookingResult.id })
      });
      if (!orderRes.ok) {
        const err = await orderRes.json().catch(() => ({}));
        throw new Error(err.error || 'Could not start payment. Please try again.');
      }
      const order = await orderRes.json(); // { orderId, keyId, amount, currency } — amount in minor units

      const rzp = new (window as any).Razorpay({
        key: order.keyId,
        amount: order.amount,
        currency: order.currency,
        order_id: order.orderId,
        name: business?.name || 'Upkilo',
        description: selectedService?.name,
        prefill: {
          name: `${contact.firstName} ${contact.lastName}`.trim(),
          email: contact.email,
          contact: contact.phone,
        },
        theme: { color: business?.primaryColor || '#06B6D4' },
        handler: (response: any) => {
          verifyPayment(response);
        },
        modal: {
          // Checkout.js's own handler already covers success; this only fires when the user
          // closes the modal without paying, so re-enable the button rather than treat it as
          // an error.
          ondismiss: () => setPayingNow(false),
        },
      });

      rzp.on('payment.failed', (resp: any) => {
        setPaymentError(resp?.error?.description || 'Payment failed. Please try again.');
        setPayingNow(false);
      });

      rzp.open();
    } catch (err: any) {
      setPaymentError(err.message || 'Could not start payment. Please try again.');
      setPayingNow(false);
    }
  };

  const verifyPayment = async (response: any) => {
    try {
      const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
      const verifyRes = await fetch(`${API_URL}/api/booking/${tenantSlug}/razorpay/verify`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          bookingId: bookingResult.id,
          orderId: response.razorpay_order_id,
          paymentId: response.razorpay_payment_id,
          signature: response.razorpay_signature,
        })
      });
      if (!verifyRes.ok) {
        const err = await verifyRes.json().catch(() => ({}));
        throw new Error(err.error || 'Payment verification failed. Please contact support.');
      }
      // Falls through to the existing "Booking Confirmed" panel below. bookingResult.message
      // still holds the original "Please complete payment..." text from booking creation, so
      // it's corrected here rather than left stale now that payment has actually completed.
      setBookingResult((prev: any) => ({ ...prev, message: 'Your booking has been confirmed.' }));
      setAwaitingPayment(false);
    } catch (err: any) {
      setPaymentError(err.message || 'Payment verification failed. Please contact support.');
    } finally {
      setPayingNow(false);
    }
  };

  useEffect(() => {
    const fetchServices = async () => {
      try {
        const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
        const res = await fetch(`${API_URL}/api/booking/${tenantSlug}/services`);
        if (!res.ok) throw new Error('Failed to fetch services');
        const data = await res.json();
        setServices(data);

        // Deep-link: if the URL pre-selects a service, auto-select it and skip to the Time step.
        if (preselectServiceId) {
          const match = data.find((s: Service) => s.id === preselectServiceId);
          if (match) {
            setSelectedService(match);
            setStep(2);
          }
        }
      } catch (err) {
        console.error(err);
      } finally {
        setLoadingServices(false);
      }
    };
    fetchServices();
  }, [tenantSlug, preselectServiceId]);

  const fetchAvailability = async (date: string) => {
    if (!selectedService) return;
    setLoadingSlots(true);
    try {
      const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
      const res = await fetch(`${API_URL}/api/booking/${tenantSlug}/availability?serviceId=${selectedService.id}&date=${date}`);
      if (!res.ok) throw new Error('Failed to fetch availability');
      const data = await res.json();
      setAvailableSlots(data.slots || []);
    } catch (err) {
      console.error(err);
    } finally {
      setLoadingSlots(false);
    }
  };

  const checkConflicts = async (date: string, time: string) => {
    if (!selectedService) return;
    setCheckingConflict(true);
    setConflicts(null);
    try {
      const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
      const res = await fetch(
        `${API_URL}/api/booking/${tenantSlug}/conflicts?serviceId=${selectedService.id}&date=${date}&time=${encodeURIComponent(time)}`
      );
      if (res.ok) {
        const data = await res.json();
        setConflicts({
          hasConflict: data.hasConflict ?? false,
          message: data.message ?? 'This slot has a scheduling conflict.',
        });
      }
    } catch {
      // Conflict check is best-effort; don't block booking on network failure
    } finally {
      setCheckingConflict(false);
    }
  };

  const nextStep = () => setStep(s => Math.min(s + 1, 4));
  const prevStep = () => setStep(s => Math.max(s - 1, 1));

  const handleServiceSelect = (svc: Service) => {
    setSelectedService(svc);
    nextStep();
  };

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      {/* Steps Header */}
      <div className="flex justify-between items-center mb-8 relative">
        <div className="absolute top-1/2 left-0 w-full h-0.5 bg-gray-200 -z-10 -translate-y-1/2" />
        {["Service", "Time", "Details", "Confirm"].map((label, i) => (
          <div key={label} className="bg-white px-2 flex flex-col items-center gap-2">
            <div className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-sm border-2 transition-colors ${
              step > i + 1 ? 'bg-[var(--primary-color)] text-white border-[var(--primary-color)]' : 
              step === i + 1 ? 'border-[var(--primary-color)] text-[var(--primary-color)]' : 'border-gray-300 text-gray-400 bg-white'
            }`}>
              {step > i + 1 ? <Check className="w-5 h-5" /> : i + 1}
            </div>
            <span className={`text-xs font-semibold ${step >= i + 1 ? 'text-gray-900 border-[var(--primary-color)]' : 'text-gray-400'}`}>{label}</span>
          </div>
        ))}
      </div>

      <Card className="border-0 shadow-lg ring-1 ring-gray-900/5 min-h-[400px] flex flex-col overflow-hidden">
        {step === 1 && (
          <CardContent className="p-6 md:p-8 flex-1 space-y-6">
            <h2 className="text-2xl font-bold text-slate-900">Select a Service</h2>
            {loadingServices ? (
              <div className="flex justify-center p-12"><Loader2 className="w-8 h-8 animate-spin text-[var(--primary-color)]" /></div>
            ) : (
              <div className="space-y-3">
                {services.map(svc => (
                  <button
                    key={svc.id}
                    onClick={() => handleServiceSelect(svc)}
                    className={`w-full text-left p-4 rounded-xl border-2 transition-all flex justify-between items-center group ${selectedService?.id === svc.id ? 'border-[var(--primary-color)] bg-[var(--primary-color-light)]' : 'border-slate-100 hover:border-[var(--primary-color)] hover:bg-slate-50'}`}
                  >
                    <div>
                      <h3 className="font-semibold text-slate-800 group-hover:text-[var(--primary-color)] transition-colors">{svc.name}</h3>
                      <p className="text-sm text-slate-500">{svc.duration} minutes</p>
                    </div>
                    <div className="font-bold text-slate-900">
                      {svc.price === 0 ? "Free" : `$${svc.price}`}
                    </div>
                  </button>
                ))}
              </div>
            )}
          </CardContent>
        )}

        {step === 2 && (
          <CardContent className="p-6 md:p-8 flex-1 space-y-6">
            <h2 className="text-2xl font-bold text-slate-900">Choose a Time</h2>
            <div className="grid md:grid-cols-2 gap-8">
              {/* Simple Date Selector Mock */}
              <div className="bg-slate-50 rounded-2xl p-6 border border-slate-100 space-y-4">
                <h3 className="font-semibold text-sm text-slate-900 uppercase tracking-wider">Select Date</h3>
                <div className="grid grid-cols-7 gap-1">
                  {[...Array(14)].map((_, i) => {
                    const d = new Date();
                    d.setDate(d.getDate() + i);
                    const dateStr = d.toISOString().split('T')[0];
                    const isSelected = selectedDate === dateStr;
                    return (
                      <button
                        key={dateStr}
                        onClick={() => { setSelectedDate(dateStr); fetchAvailability(dateStr); }}
                        className={`aspect-square rounded-lg flex flex-col items-center justify-center text-xs transition-all ${isSelected ? 'bg-[var(--primary-color)] text-white shadow-md scale-105' : 'hover:bg-slate-200 text-slate-600'}`}
                      >
                        <span className="opacity-70">{d.toLocaleDateString('en-US', { weekday: 'short' })}</span>
                        <span className="font-bold text-sm">{d.getDate()}</span>
                      </button>
                    );
                  })}
                </div>
              </div>
              
              <div className="space-y-4">
                <h3 className="font-semibold text-sm text-slate-900 uppercase tracking-wider flex items-center gap-2">
                  <Clock className="w-4 h-4 text-[var(--primary-color)]" /> Available Times
                </h3>
                {loadingSlots ? (
                  <div className="flex justify-center p-12"><Loader2 className="w-8 h-8 animate-spin text-[var(--primary-color)]" /></div>
                ) : !selectedDate ? (
                  <div className="p-12 text-center text-slate-400 text-sm">Please select a date first</div>
                ) : availableSlots.length === 0 ? (
                  <div className="p-12 text-center text-slate-400 text-sm">No available slots for this date</div>
                ) : (
                  <div className="grid grid-cols-2 gap-2 max-h-[300px] overflow-y-auto pr-2">
                    {availableSlots.map(slot => (
                      <button
                        key={slot.time}
                        onClick={() => {
                          setSelectedTime(slot.time);
                          if (selectedDate) checkConflicts(selectedDate, slot.time);
                        }}
                        className={`py-3 text-sm font-medium text-center rounded-xl border-2 transition-all ${selectedTime === slot.time ? 'border-[var(--primary-color)] bg-[var(--primary-color-light)] text-[var(--primary-color)]' : 'bg-white border-slate-100 hover:border-[var(--primary-color)] text-slate-700'}`}
                      >
                        {slot.time}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            </div>

            {/* Conflict preview — shown when a time is selected */}
            {selectedTime && (
              <div className="mt-4">
                {checkingConflict ? (
                  <div className="flex items-center gap-2 text-sm text-slate-500 bg-slate-50 rounded-xl px-4 py-3">
                    <Loader2 className="w-4 h-4 animate-spin" aria-hidden="true" />
                    <span>Checking availability...</span>
                  </div>
                ) : conflicts?.hasConflict ? (
                  <div role="alert" className="flex items-start gap-3 bg-amber-50 border border-amber-200 rounded-xl px-4 py-3">
                    <AlertTriangle className="w-4 h-4 text-amber-500 mt-0.5 shrink-0" aria-hidden="true" />
                    <div>
                      <p className="text-sm font-semibold text-amber-800">Scheduling conflict detected</p>
                      <p className="text-xs text-amber-700 mt-0.5">{conflicts.message}</p>
                    </div>
                  </div>
                ) : conflicts && !conflicts.hasConflict ? (
                  <div className="flex items-center gap-2 text-sm text-emerald-700 bg-emerald-50 border border-emerald-200 rounded-xl px-4 py-3">
                    <Check className="w-4 h-4 text-emerald-500 shrink-0" aria-hidden="true" />
                    <span>Time slot is available — no conflicts</span>
                  </div>
                ) : null}

                {/* Proceed button — disabled if conflict present */}
                <button
                  onClick={nextStep}
                  disabled={conflicts?.hasConflict === true}
                  className="mt-3 w-full py-3 rounded-xl text-sm font-semibold transition-all flex items-center justify-center gap-2 bg-[var(--primary-color)] text-white hover:opacity-90 disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  Continue with {selectedTime}
                  <ArrowRight className="w-4 h-4" aria-hidden="true" />
                </button>
              </div>
            )}
          </CardContent>
        )}

        {step === 3 && (
          <CardContent className="p-6 md:p-8 flex-1 space-y-6">
            <h2 className="text-2xl font-bold text-slate-900">Your Details</h2>
            <div className="space-y-4 max-w-md">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <label className="text-sm font-semibold text-slate-700">First Name</label>
                  <Input className="rounded-xl border-slate-200 focus:ring-[var(--primary-color)] focus:border-[var(--primary-color)]" value={contact.firstName} onChange={e => setContact({...contact, firstName: e.target.value})} />
                </div>
                <div className="space-y-1.5">
                  <label className="text-sm font-semibold text-slate-700">Last Name</label>
                  <Input className="rounded-xl border-slate-200 focus:ring-[var(--primary-color)] focus:border-[var(--primary-color)]" value={contact.lastName} onChange={e => setContact({...contact, lastName: e.target.value})} />
                </div>
              </div>
              <div className="space-y-1.5">
                <label className="text-sm font-semibold text-slate-700">Email Address</label>
                <Input type="email" className="rounded-xl border-slate-200 focus:ring-[var(--primary-color)] focus:border-[var(--primary-color)]" value={contact.email} onChange={e => setContact({...contact, email: e.target.value})} />
              </div>
              <div className="space-y-1.5">
                <label className="text-sm font-semibold text-slate-700">Phone Number</label>
                <Input type="tel" className="rounded-xl border-slate-200 focus:ring-[var(--primary-color)] focus:border-[var(--primary-color)]" value={contact.phone} onChange={e => setContact({...contact, phone: e.target.value})} />
              </div>
              <div className="space-y-1.5">
                <label className="text-sm font-semibold text-slate-700">Appointment Notes (Optional)</label>
                <textarea 
                  className="w-full rounded-xl border border-slate-200 p-3 text-sm min-h-[100px] focus:ring-2 focus:ring-[var(--primary-color)] focus:border-[var(--primary-color)] transition-all outline-none" 
                  value={contact.notes}
                  onChange={e => setContact({...contact, notes: e.target.value})}
                />
              </div>
            </div>
          </CardContent>
        )}

        {step === 4 && awaitingPayment && (
          <CardContent className="p-6 md:p-8 flex-1 flex flex-col items-center justify-center text-center space-y-6">
            <div className="w-20 h-20 rounded-full bg-[var(--primary-color-light)] flex items-center justify-center mb-2">
              <CreditCard className="w-10 h-10 text-[var(--primary-color)]" />
            </div>
            <div>
              <h2 className="text-2xl font-bold text-slate-900 mb-2">Reserve Your Spot</h2>
              <p className="text-slate-500 max-w-xs mx-auto">
                Your time is held — pay the deposit below to confirm {selectedService?.name}.
              </p>
            </div>

            <div className="bg-slate-50 rounded-2xl p-6 w-full max-w-sm text-left border border-slate-100 space-y-1">
              <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Deposit due</div>
              {/* INR-only for now, matching the backend's currency whitelist in
                  PublicBookingController.CreateRazorpayOrder — the initial booking response
                  doesn't include a currency field to read here. */}
              <div className="text-3xl font-bold text-slate-900">
                {currencySymbol('INR')}{bookingResult?.booking?.depositAmount}
              </div>
            </div>

            {paymentError && (
              <div role="alert" className="flex items-start gap-3 bg-red-50 border border-red-200 rounded-xl px-4 py-3 w-full max-w-sm text-left">
                <AlertTriangle className="w-4 h-4 text-red-500 mt-0.5 shrink-0" aria-hidden="true" />
                <p className="text-sm text-red-700">{paymentError}</p>
              </div>
            )}

            <Button
              onClick={handlePayNow}
              disabled={payingNow}
              className="rounded-xl px-8 bg-[var(--primary-color)] hover:bg-[var(--primary-color-hover)] text-white shadow-lg transition-all w-full max-w-sm"
            >
              {payingNow ? <Loader2 className="w-4 h-4 mr-2 animate-spin" /> : <CreditCard className="w-4 h-4 mr-2" />}
              Pay Now
            </Button>
          </CardContent>
        )}

        {step === 4 && !awaitingPayment && (
          <CardContent className="p-6 md:p-8 flex-1 flex flex-col items-center justify-center text-center space-y-6">
            <div className="w-20 h-20 rounded-full bg-emerald-50 flex items-center justify-center mb-2">
              <CheckCircle2 className="w-12 h-12 text-emerald-500" />
            </div>
            <div>
              <h2 className="text-2xl font-bold text-slate-900 mb-2">Booking Confirmed!</h2>
              <p className="text-slate-500 max-w-xs mx-auto">
                {bookingResult?.message || `We've sent a confirmation email to ${contact.email}.`}
              </p>
              {bookingResult?.confirmationNumber && (
                <div className="mt-4 px-4 py-2 bg-slate-100 rounded-lg font-mono text-sm font-bold text-slate-700 inline-block uppercase tracking-widest">
                  #{bookingResult.confirmationNumber}
                </div>
              )}
            </div>

            <div className="bg-slate-50 rounded-2xl p-6 w-full max-w-sm text-left border border-slate-100 space-y-4">
              <div className="flex gap-4">
                <div className="w-10 h-10 rounded-xl bg-white border border-slate-100 flex items-center justify-center text-[var(--primary-color)] shadow-sm">
                  <BookmarkIcon className="w-5 h-5" />
                </div>
                <div>
                  <div className="font-bold text-slate-900">{selectedService?.name}</div>
                  <div className="text-sm text-slate-500">{selectedService?.duration} mins • ${selectedService?.price}</div>
                </div>
              </div>
              <div className="flex gap-4">
                <div className="w-10 h-10 rounded-xl bg-white border border-slate-100 flex items-center justify-center text-[var(--primary-color)] shadow-sm">
                  <Calendar className="w-5 h-5" />
                </div>
                <div>
                  <div className="font-bold text-slate-900">{selectedDate ? new Date(selectedDate).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' }) : 'Date'}</div>
                  <div className="text-sm text-slate-500">{selectedTime}</div>
                </div>
              </div>
            </div>
          </CardContent>
        )}

        {/* Footer Navigation */}
        <div className="border-t p-4 md:p-6 bg-slate-50 flex justify-between rounded-b-xl">
          <Button variant="outline" onClick={prevStep} className={`rounded-xl border-slate-200 text-slate-600 hover:bg-white ${step === 1 || step === 4 ? "invisible" : ""}`}>
            <ArrowLeft className="w-4 h-4 mr-2" /> Back
          </Button>
          <Button 
            onClick={step === 3 ? handleConfirmBooking : nextStep} 
            disabled={
              submitting ||
              (step === 1 && !selectedService) || 
              (step === 2 && !selectedTime) || 
              (step === 3 && (!contact.firstName || !contact.email))
            }
            className={`rounded-xl px-8 bg-[var(--primary-color)] hover:bg-[var(--primary-color-hover)] text-white shadow-lg transition-all ${step === 4 ? "hidden" : ""}`}
          >
            {submitting ? <Loader2 className="w-4 h-4 mr-2 animate-spin" /> : null}
            {step === 3 ? "Confirm Booking" : "Continue"} <ArrowRight className="w-4 h-4 ml-2" />
          </Button>
        </div>
      </Card>
    </div>
  );
}

function BookmarkIcon(props: any) {
  return <svg {...props} fill="none" strokeWidth="2" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z"></path></svg>;
}
