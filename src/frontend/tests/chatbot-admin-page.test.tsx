/**
 * Regression tests for the chatbot admin page.
 *
 * Both cases here were shipped bugs that reported success while doing the wrong thing, which is
 * the failure mode worth a permanent test: "Save Configuration" fired a success toast and made no
 * request at all, and adding a knowledge base entry appended the server's `{ success: true }`
 * envelope to the list, rendering a blank card that vanished on the next refresh.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import React from 'react';

// () => null is a valid React component — no React import needed inside the hoisted factory.
vi.mock('lucide-react', () => ({
  Sparkles: () => null,
  MessageSquare: () => null,
  Settings: () => null,
  ShieldCheck: () => null,
  Bot: () => null,
  Plus: () => null,
  Trash2: () => null,
  RefreshCcw: () => null,
  AlertCircle: () => null,
  CheckCircle2: () => null,
  Users: () => null,
  Activity: () => null,
}));

const getSettings = vi.fn();
const updateSettings = vi.fn();
const getKnowledgeBase = vi.fn();
const addKnowledgeBase = vi.fn();
const deleteKnowledgeBase = vi.fn();
const getStats = vi.fn();

vi.mock('@/lib/api', () => ({
  api: {
    chatbot: {
      getSettings: () => getSettings(),
      updateSettings: (d: unknown) => updateSettings(d),
      getKnowledgeBase: () => getKnowledgeBase(),
      addKnowledgeBase: (d: unknown) => addKnowledgeBase(d),
      deleteKnowledgeBase: (id: string) => deleteKnowledgeBase(id),
      getStats: () => getStats(),
    },
  },
}));

const toastSuccess = vi.fn();
const toastError = vi.fn();
vi.mock('@/components/ui/Toast', () => ({
  useToast: () => ({ success: toastSuccess, error: toastError }),
}));

import ChatbotAdminPage from '@/app/[locale]/(dashboard)/ai/chatbot/page';

beforeEach(() => {
  vi.clearAllMocks();
  getSettings.mockResolvedValue({
    data: {
      isEnabled: true,
      botName: 'Upkilo Assistant',
      handoffEmail: '',
      welcomeMessage: 'Hello!',
    },
  });
  getKnowledgeBase.mockResolvedValue({ data: [] });
  getStats.mockResolvedValue({ data: { totalConversations: 0, resolutionRate: 0, activeHandoffs: 0 } });
  updateSettings.mockResolvedValue({ data: { isEnabled: true, botName: 'Front Desk', handoffEmail: '', welcomeMessage: 'Hello!' } });
});

/** Renders and waits for the initial load to settle. */
async function renderPage() {
  render(<ChatbotAdminPage />);
  await waitFor(() => expect(screen.queryByText(/loading ai chatbot/i)).not.toBeInTheDocument());
  return userEvent.setup();
}

describe('Chatbot admin — saving configuration', () => {
  it('actually persists the persona instead of only showing a toast', async () => {
    const user = await renderPage();

    const nameInput = screen.getByDisplayValue('Upkilo Assistant');
    await user.clear(nameInput);
    await user.type(nameInput, 'Front Desk');

    await user.click(screen.getByRole('button', { name: /save configuration/i }));

    // The assertion that matters: a request was made carrying the edit.
    await waitFor(() => expect(updateSettings).toHaveBeenCalledTimes(1));
    expect(updateSettings).toHaveBeenCalledWith(expect.objectContaining({ botName: 'Front Desk' }));
    expect(toastSuccess).toHaveBeenCalledWith('Settings saved');
  });

  it('reports a save failure rather than claiming success', async () => {
    updateSettings.mockRejectedValue({ response: { data: { error: 'Handoff email is not a valid email address' } } });
    const user = await renderPage();

    await user.click(screen.getByRole('button', { name: /save configuration/i }));

    await waitFor(() =>
      expect(toastError).toHaveBeenCalledWith('Handoff email is not a valid email address')
    );
    expect(toastSuccess).not.toHaveBeenCalled();
  });
});

describe('Chatbot admin — knowledge base', () => {
  it('renders the entry returned by the server after adding one', async () => {
    addKnowledgeBase.mockResolvedValue({
      data: { id: 'kb-1', category: 'General', question: 'Do you park?', answer: 'Yes, out back.' },
    });

    const user = await renderPage();

    await user.type(screen.getByPlaceholderText(/opening hours/i), 'Do you park?');
    await user.type(screen.getByPlaceholderText(/describe the answer/i), 'Yes, out back.');
    await user.click(screen.getByRole('button', { name: /add to knowledge base/i }));

    expect(await screen.findByText('Do you park?')).toBeInTheDocument();
    expect(screen.getByText(/Yes, out back\./)).toBeInTheDocument();
  });

  it('refetches instead of appending an entry with no id', async () => {
    // The old backend shape. Appending it produced an unkeyed, contentless card.
    addKnowledgeBase.mockResolvedValue({ data: { success: true } });
    getKnowledgeBase
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({ data: [{ id: 'kb-9', category: 'General', question: 'Recovered?', answer: 'Yes.' }] });

    const user = await renderPage();

    await user.type(screen.getByPlaceholderText(/opening hours/i), 'Recovered?');
    await user.type(screen.getByPlaceholderText(/describe the answer/i), 'Yes.');
    await user.click(screen.getByRole('button', { name: /add to knowledge base/i }));

    expect(await screen.findByText('Recovered?')).toBeInTheDocument();
  });

  it('gives the delete control an accessible name', async () => {
    getKnowledgeBase.mockResolvedValue({
      data: [{ id: 'kb-1', category: 'General', question: 'Do you park?', answer: 'Yes.' }],
    });

    await renderPage();

    // Was an unlabelled, hover-only button: unreachable by keyboard and invisible on touch.
    expect(
      await screen.findByRole('button', { name: /delete knowledge base entry: Do you park\?/i })
    ).toBeInTheDocument();
  });
});
