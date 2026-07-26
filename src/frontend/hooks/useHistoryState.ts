import { useState, useCallback, useMemo, useRef, useEffect } from "react";

export interface HistoryActions<T> {
  undo: () => T | undefined;
  redo: () => T | undefined;
  canUndo: boolean;
  canRedo: boolean;
  clear: () => void;
}

export function useHistoryState<T>(
  initialState: T,
  options: { maxHistory?: number } = {}
): [T, (nextState: T | ((prev: T) => T)) => void, HistoryActions<T>] {
  const { maxHistory = 50 } = options;

  const [state, setState] = useState<T>(initialState);
  const historyRef = useRef<{
    past: T[];
    present: T;
    future: T[];
  }>({
    past: [],
    present: initialState,
    future: [],
  });

  const canUndo = historyRef.current.past.length > 0;
  const canRedo = historyRef.current.future.length > 0;

  const setHistoryState = useCallback(
    (nextStateOrUpdater: T | ((prev: T) => T)) => {
      const nextState =
        typeof nextStateOrUpdater === "function"
          ? (nextStateOrUpdater as (prev: T) => T)(historyRef.current.present)
          : nextStateOrUpdater;

      if (JSON.stringify(nextState) === JSON.stringify(historyRef.current.present)) {
        return;
      }

      historyRef.current = {
        past: [...historyRef.current.past, historyRef.current.present].slice(-maxHistory),
        present: nextState,
        future: [],
      };

      setState(nextState);
    },
    [maxHistory]
  );

  const undo = useCallback(() => {
    if (historyRef.current.past.length === 0) return undefined;
    const previous = historyRef.current.past[historyRef.current.past.length - 1];
    const newPast = historyRef.current.past.slice(0, historyRef.current.past.length - 1);
    historyRef.current = {
      past: newPast,
      present: previous,
      future: [historyRef.current.present, ...historyRef.current.future],
    };
    setState(previous);
    return previous;
  }, []);

  const redo = useCallback(() => {
    if (historyRef.current.future.length === 0) return undefined;
    const next = historyRef.current.future[0];
    const newFuture = historyRef.current.future.slice(1);
    historyRef.current = {
      past: [...historyRef.current.past, historyRef.current.present],
      present: next,
      future: newFuture,
    };
    setState(next);
    return next;
  }, []);

  const clear = useCallback(() => {
    historyRef.current = {
      past: [],
      present: initialState,
      future: [],
    };
    setState(initialState);
  }, [initialState]);

  const actions = useMemo(
    () => ({
      undo,
      redo,
      canUndo,
      canRedo,
      clear,
    }),
    [undo, redo, canUndo, canRedo, clear]
  );

  return [state, setHistoryState, actions];
}
