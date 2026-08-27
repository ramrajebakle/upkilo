# Frontend data layer

## Why this exists

169 dashboard pages fetch data. Before this change, 166 of them did it the same
way:

```tsx
const [rows, setRows] = useState([]);
const [loading, setLoading] = useState(true);
useEffect(() => {
  (async () => {
    try { setRows((await api.thing.list(filter)).data.data ?? []); }
    catch { setRows([]); }          // <- the problem
    finally { setLoading(false); }
  })();
}, [filter]);
```

Three defects are structural to that shape, so they appeared everywhere it did:

**1. A failed request renders as "you have no data."**
`catch { setRows([]) }` makes an outage indistinguishable from an empty
account. The customer is told, in confident product language, that they have no
bookings / no clients / no revenue. It is the failure mode most likely to make
someone believe a system lost their data. 166 of 169 pages had no error branch
at all.

**2. Stale responses overwrite fresh ones.**
Only 1 of 143 fetching files used an `AbortController`; 6 used an ignore flag.
Change a filter twice quickly and two requests race — whichever lands last
wins, even if it is the one that was abandoned. `tests/query-race.test.tsx`
reproduces this against the old shape and asserts the new one is immune.

**3. Every page re-implements caching, dedup and invalidation** — or, more
often, does without them. Navigating away and back refetches from scratch.

React Query was already a dependency and already mounted in
`app/[locale]/layout.tsx`. It was used in 3 files out of 143. Nothing new was
introduced here; the tool already in the tree was adopted.

## Layout

| File | Role |
|---|---|
| `lib/query/keys.ts` | Hierarchical key factory — one namespace per domain |
| `lib/query/unwrap.ts` | Normalises the `res.data` vs `res.data.data` envelopes |
| `lib/query/<domain>.ts` | Typed hooks for one domain |
| `components/ui/ErrorState.tsx` | The error branch `EmptyState` never had |

`lib/api.ts` is unchanged and stays the transport: it already carries the
circuit breaker, the deduplicated 401 refresh and the 429 retry. Hooks call it.

## Migrating a page

1. **Add hooks for the domain** in `lib/query/<domain>.ts`. Put every parameter
   that changes the response into the query key — that is what makes an
   abandoned response inert.

2. **Replace the effect** with the hook:

   ```tsx
   const { data = [], isPending, isError, error, refetch, isFetching } =
     useThings(filter);
   ```

   Use `placeholderData: (previous) => previous` on filtered lists so changing a
   filter does not blank the table.

3. **Render three branches, not two.** This is the part that was missing:

   ```tsx
   {isError ? (
     <ErrorState title="Couldn't load things" error={error}
                 onRetry={() => refetch()} isRetrying={isFetching} />
   ) : isPending ? (
     <SkeletonTable />
   ) : (
     <Table rows={data} />
   )}
   ```

   And gate the empty state on `!isError`, or a failure still renders as
   "nothing here".

4. **Mutations invalidate; they do not hand-patch local state.** Optimistic
   updates need a matching rollback in `onError` — otherwise a failed write
   leaves the row showing a result the server rejected.

5. **Never `Promise.all(ids.map(...)).catch(() => null)`.** It swallows every
   rejection, so a run in which all requests failed still reports success. Use
   `Promise.allSettled` and report the real count.

## Migrated so far

- `bookings/page.tsx`
- `clients/page.tsx`

The remaining pages still use the old shape. The three defects above are
present in each of them.
