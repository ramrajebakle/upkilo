export async function registerPushNotifications() {
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
    console.warn('Push notifications are not supported in this browser');
    return;
  }

  try {
    const registration = await navigator.serviceWorker.register('/sw.js');
    console.log('Service Worker registered:', registration);

    const subscription = await registration.pushManager.getSubscription();
    if (subscription) {
      return subscription;
    }

    // Get VAPID public key from API
    const response = await fetch('/api/v1/settings/push-key'); 
    const { publicKey } = await response.json();

    const newSubscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(publicKey)
    });

    // Send subscription to backend
    await saveSubscription(newSubscription);
    return newSubscription;
  } catch (error) {
    console.error('Error registering push notifications:', error);
  }
}

async function saveSubscription(subscription: PushSubscription) {
  const p256dh = btoa(String.fromCharCode.apply(null, Array.from(new Uint8Array(subscription.getKey('p256dh')!))));
  const auth = btoa(String.fromCharCode.apply(null, Array.from(new Uint8Array(subscription.getKey('auth')!))));

  await fetch('/api/v1/notification/browser/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      endpoint: subscription.endpoint,
      p256dh,
      auth,
      tag: navigator.userAgent
    })
  });
}

function urlBase64ToUint8Array(base64String: string) {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/\-/g, '+').replace(/_/g, '/');
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}
