/**
 * Behaviour tests for the global support widget.
 *
 * The widget shipped as a mockup: a styled panel whose input had no state and no request behind
 * it, so every one of these assertions would have failed. They are written against what a visitor
 * can observe — type, press Enter, see a reply — rather than against implementation details, so
 * they keep holding if the internals change.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import React from 'react';

vi.mock('lucide-react', () => ({
  MessageCircle: () => null,
  X: () => null,
  Send: () => null,
  RotateCcw: () => null,
  Loader2: () => null,
}));

const sendMessage = vi.fn();
const supportChat = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    chatbot: { sendMessage: (...a: unknown[]) => sendMessage(...a) },
    support: { chat: (...a: unknown[]) => supportChat(...a) },
  },
}));

// Authentication decides which assistant the widget talks to, so it is a per-test input.
let authed = false;
vi.mock('@/store/authStore', () => ({
  useAuthStore: (selector: (s: { isAuthenticated: boolean }) => unknown) =>
    selector({ isAuthenticated: authed }),
}));

import { LiveChatWidget } from '@/components/LiveChatWidget';

/** Opens the panel and returns the input, which is what nearly every test needs first. */
async function openWidget() {
  const user = userEvent.setup();
  render(<LiveChatWidget />);
  await user.click(screen.getByRole('button', { name: /open support chat/i }));
  return { user, input: screen.getByLabelText(/type your message/i) };
}

beforeEach(() => {
  authed = false;
  sendMessage.mockReset();
  supportChat.mockReset();
  supportChat.mockResolvedValue({ data: { reply: 'Upkilo is a booking platform.' } });
  sendMessage.mockResolvedValue({ data: { response: 'Your next booking is Tuesday.' } });
  try {
    sessionStorage.clear();
  } catch {
    /* jsdom always provides it; guarded to match the component */
  }
});

describe('LiveChatWidget — sending', () => {
  it('is closed until the launcher is clicked', () => {
    render(<LiveChatWidget />);
    expect(screen.queryByLabelText(/type your message/i)).not.toBeInTheDocument();
  });

  it('sends on Enter and shows both the question and the reply', async () => {
    const { user, input } = await openWidget();

    await user.type(input, 'What is Upkilo?');
    await user.keyboard('{Enter}');

    // The visitor's own message must appear immediately, not only after the reply lands.
    expect(await screen.findByText('What is Upkilo?')).toBeInTheDocument();
    expect(await screen.findByText('Upkilo is a booking platform.')).toBeInTheDocument();
    expect(supportChat).toHaveBeenCalledTimes(1);
    expect(supportChat).toHaveBeenCalledWith('What is Upkilo?', null);
  });

  it('clears the input after sending, so the message cannot be sent twice', async () => {
    const { user, input } = await openWidget();

    await user.type(input, 'Hello{Enter}');

    await waitFor(() => expect(input).toHaveValue(''));
  });

  it('treats Shift+Enter as a newline rather than a send', async () => {
    const { user, input } = await openWidget();

    await user.type(input, 'first line');
    await user.keyboard('{Shift>}{Enter}{/Shift}');
    await user.type(input, 'second line');

    expect(supportChat).not.toHaveBeenCalled();
    expect(input).toHaveValue('first line\nsecond line');
  });

  it('refuses a whitespace-only message', async () => {
    const { user, input } = await openWidget();

    await user.type(input, '   ');
    await user.keyboard('{Enter}');

    expect(supportChat).not.toHaveBeenCalled();
  });

  it('disables the send button while the box is empty', async () => {
    const { user, input } = await openWidget();

    expect(screen.getByRole('button', { name: /send message/i })).toBeDisabled();

    await user.type(input, 'hi');
    expect(screen.getByRole('button', { name: /send message/i })).toBeEnabled();
  });

  it('does not fire a second request while one is in flight', async () => {
    let release!: (v: unknown) => void;
    supportChat.mockReturnValue(new Promise((r) => { release = r; }));

    const { user, input } = await openWidget();

    await user.type(input, 'one');
    await user.keyboard('{Enter}');
    await user.type(input, 'two');
    await user.keyboard('{Enter}');

    expect(supportChat).toHaveBeenCalledTimes(1);

    release({ data: { reply: 'done' } });
    await screen.findByText('done');
  });
});

describe('LiveChatWidget — routing', () => {
  it('uses the anonymous Upkilo assistant when signed out', async () => {
    const { user, input } = await openWidget();

    await user.type(input, 'What is Upkilo?{Enter}');

    await waitFor(() => expect(supportChat).toHaveBeenCalled());
    expect(sendMessage).not.toHaveBeenCalled();
  });

  it("uses the tenant's own assistant when signed in", async () => {
    authed = true;
    const { user, input } = await openWidget();

    await user.type(input, 'When is my next booking?{Enter}');

    expect(await screen.findByText('Your next booking is Tuesday.')).toBeInTheDocument();
    expect(sendMessage).toHaveBeenCalledWith('When is my next booking?');
    expect(supportChat).not.toHaveBeenCalled();
  });

  it('echoes the server-issued session token back on the next turn', async () => {
    supportChat.mockResolvedValueOnce({ data: { reply: 'first', sessionToken: 'sess.sig' } });
    supportChat.mockResolvedValueOnce({ data: { reply: 'second' } });

    const { user, input } = await openWidget();

    await user.type(input, 'one{Enter}');
    await screen.findByText('first');

    await user.type(input, 'two{Enter}');
    await screen.findByText('second');

    // The session is server-issued and opaque; continuity depends on returning it verbatim.
    expect(supportChat).toHaveBeenNthCalledWith(1, 'one', null);
    expect(supportChat).toHaveBeenNthCalledWith(2, 'two', 'sess.sig');
  });
});

describe('LiveChatWidget — failure handling', () => {
  it('shows a recoverable error instead of failing silently', async () => {
    supportChat.mockRejectedValue(new Error('network down'));

    const { user, input } = await openWidget();
    await user.type(input, 'hello{Enter}');

    expect(await screen.findByText(/something went wrong/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
  });

  it('never shows the underlying error text to the visitor', async () => {
    supportChat.mockRejectedValue(new Error('ECONNREFUSED 10.0.0.4:5000'));

    const { user, input } = await openWidget();
    await user.type(input, 'hello{Enter}');

    await screen.findByText(/something went wrong/i);
    expect(screen.queryByText(/ECONNREFUSED/)).not.toBeInTheDocument();
  });

  it('retry re-sends the failed message and succeeds', async () => {
    supportChat.mockRejectedValueOnce(new Error('network down'));
    supportChat.mockResolvedValueOnce({ data: { reply: 'Recovered.' } });

    const { user, input } = await openWidget();
    await user.type(input, 'hello{Enter}');

    await user.click(await screen.findByRole('button', { name: /try again/i }));

    expect(await screen.findByText('Recovered.')).toBeInTheDocument();
    expect(supportChat).toHaveBeenCalledTimes(2);
  });

  it('treats an empty 200 as a failure rather than an empty bubble', async () => {
    supportChat.mockResolvedValue({ data: { reply: '' } });

    const { user, input } = await openWidget();
    await user.type(input, 'hello{Enter}');

    expect(await screen.findByRole('button', { name: /try again/i })).toBeInTheDocument();
  });
});

describe('LiveChatWidget — safety and accessibility', () => {
  it('renders assistant output as text, so markup in a reply cannot execute', async () => {
    const payload = '<img src=x onerror="alert(1)">';
    supportChat.mockResolvedValue({ data: { reply: payload } });

    const { user, input } = await openWidget();
    await user.type(input, 'hello{Enter}');

    // Present as literal text, and no element was created from it.
    expect(await screen.findByText(payload)).toBeInTheDocument();
    expect(document.querySelector('img')).toBeNull();
  });

  it('exposes the transcript as a live region and labels the input', async () => {
    await openWidget();

    const log = screen.getByRole('log', { name: /conversation/i });
    expect(log).toHaveAttribute('aria-live', 'polite');
    expect(screen.getByLabelText(/type your message/i)).toBeInTheDocument();
  });

  it('closes on Escape', async () => {
    const { user } = await openWidget();

    await user.keyboard('{Escape}');

    await waitFor(() =>
      expect(screen.queryByLabelText(/type your message/i)).not.toBeInTheDocument()
    );
  });
});
