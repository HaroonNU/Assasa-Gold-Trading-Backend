# What I Did

## How I understood the assignment

The brief asks for a deployable, single-user demo of buying and selling gold
where a reviewer can, with no extra guidance: see starting balances, see a
live 24K PKR/gram rate with its source and freshness, lock in a quote for 75
seconds, confirm it once, and see a receipt with updated balances. The
emphasis throughout is on **trust and correctness under stress**, not on
features: the four explicit test cases (a price source going down, a quote
expiring, a balance running short, confirm being pressed twice) are really
asking "does this system tell the truth and stay consistent when things go
wrong," and the evaluation criteria weight technical judgment/safety and
correctness/attention-to-detail as heavily as completion and visual craft.
So I treated "no database, in-memory only" and "server owns every
calculation" as the two load-bearing constraints, and built everything else
— architecture, tests, demo controls — in service of making the stress
cases genuinely verifiable rather than just plausible.

## Assumptions I made

- **No real "PakGold" API exists.** `pakgold.com` is a parked domain for
  sale on HugeDomains, not a gold-price service. I treated "PakGold" as the
  brief's *label* for "primary pricing source" rather than a literal
  integration target, and substituted two verified, free, keyless real APIs
  underneath it (see Key Decisions). The UI still shows "Source: PakGold" so
  the demo matches the brief's diagram.
- **"24K PKR per gram" is the normalized unit everywhere**, regardless of
  what unit/purity a given price source natively reports in (troy ounces,
  USD, etc.) — all conversion happens once, at the provider boundary.
- **Single review session, not concurrent reviewers.** The brief describes a
  single-user demo; I optimized the in-memory design for one process
  serving one customer, not multi-tenant isolation.
- **"Repeated confirmation" means both the honest double-click case
  (idempotent success) and a genuine race (concurrent requests)** — I built
  and tested for both rather than just the simpler sequential case.
- Where the brief and my own earlier work disagreed on specific numbers
  (pricing multipliers, cache interval), I treated the **brief's numbers as
  authoritative** and corrected the code to match once I re-read it
  carefully (see Known Gaps for how this was caught).

## What I built

**Backend** — ASP.NET Core / .NET 8, Clean Architecture across
`GoldTrading.Domain / Application / Infrastructure / Api`, zero database.

- In-memory singleton repositories for account state, quotes, and trades.
- `PriceCache` fetches from a primary provider, falls back to a secondary
  one on failure, caches the trusted result for a configurable interval
  (5 minutes by default, per the brief), and reports `Unavailable` /
  untrusted if both fail — which in turn disables quote creation.
- Quote creation is entirely server-computed: locked market price, customer
  price (`max(market × 1.10, guardrail)` for buy, `market × 0.90` for sell),
  converted PKR/gold amounts, and a 75-second expiry, all server-owned.
- Confirmation goes through a dedicated `TradeSettlementLock`
  (`SemaphoreSlim`-based) so the whole load→validate→debit→save sequence is
  atomic; an already-confirmed quote returns the original trade instead of
  erroring, and this is proven under **real concurrent load** (10
  simultaneous confirm requests against one quote → exactly one trade, in
  the QA pass — see the transcript).
- A `DemoControlService` + `/api/demo/*` endpoints let a reviewer simulate
  primary-provider failure, total pricing outage, forced quote expiry, low
  balances (each of the three), and the guardrail scenario, plus reset
  everything to seed values — all without touching deployed code or
  configuration, per the brief's explicit requirement.
- Centralized `ErrorCodes` / `ErrorMessages` / `ValidationMessages` and a
  `GlobalExceptionMiddleware` so every failure path returns the same
  `{ success, code, message }` shape with no internal details leaked.
- 70 automated tests (50 unit + 20 integration, the latter running the real
  ASP.NET pipeline via `WebApplicationFactory` with the real external HTTP
  calls swapped for canned responses) covering pricing fallback/cache,
  quote math for every input combination, every insufficiency case, and the
  concurrency guarantees above.

**Frontend** — React / TypeScript / Vite / Tailwind, its own standalone
project (`AsasaGoldTrading.Frontend`, separate from the backend repo).

- Single-page journey: price + balances → trade form → locked quote review
  with a live countdown → confirm → receipt, exactly matching the brief's
  5-step diagram.
- `useTradeFlow` is the one hook owning that lifecycle (no Redux); the
  frontend sends only `{ tradeType, inputType, amount }` and never computes
  a price or amount itself.
- Semantic CSS-variable theme tokens (`--color-primary`, `--color-accent`,
  etc.) built from the brief's exact palette
  (`#0D4A46` / `#8CCB50` / `#F9FAFA` / `#1A1F1B`), with light/dark switching
  by re-declaring those variables under a `.dark` class — no component ever
  references a hardcoded hex value or a `dark:` variant.
- A collapsible Demo Controls panel, isolated in its own component/service
  so it can be deleted without touching any trading code, exposing every
  stress case from the brief as a button.
- Mobile-first: verified at 375px with no horizontal overflow, comfortable
  tap targets, and the demo-controls grid collapsing to one column.
- Basic accessibility done deliberately, not incidentally: labeled inputs,
  `role="alert"`/`aria-live` on errors and the countdown's expiry state,
  a visible focus ring, `aria-pressed` on the Buy/Sell and PKR/Gold
  toggles, and a native `<details>` disclosure for Demo Controls so it's
  keyboard-operable for free.

## Key decisions

- **Singleton in-memory repositories, not a database.** The brief requires
  this explicitly. It means state resets on every restart/redeploy — this
  is documented as intentional, not a bug, and is fine for a single-instance
  assessment demo; it would not survive a multi-instance production
  deployment, which the code makes no attempt to pretend otherwise.
- **A dedicated settlement lock, not one giant global lock.** Each
  repository has its own fine-grained lock for its own data; the
  `TradeSettlementLock` serializes only the confirm→debit→save sequence, so
  reads (balances, price) are never blocked by a settlement in progress.
- **Kept the "PakGold" label, swapped the real network call.** Rather than
  silently fail forever against a parked domain, `PakGoldProvider` now
  calls `api.gold-api.com` (live XAU/USD spot) and `open.er-api.com` (live
  USD→PKR rate) and combines them the same way the GoldPrice.org fallback
  already did. Verified live: Rs 39,539.98/gram, Buy/Sell formulas exact.
  This was a judgment call, discussed and confirmed mid-build rather than
  decided unilaterally — see the transcript for the reasoning.
- **Separated the frontend into its own repository.** The original scaffold
  had it colocated inside the backend repo, calling stale routes from an
  earlier draft. Given the two are independently deployable and the
  assessment explicitly allows "any stack," I judged a clean split more
  honest about the actual architecture than keeping them artificially
  bundled.
- **Deleted a superseded duplicate backend implementation** found alongside
  the current one (an earlier flat-structure draft, same problem solved
  twice) rather than leaving two competing versions of the same service in
  the repo.

## Known gaps

- **GoldPrice.org (the fallback) could not be verified live from the
  development sandbox** — its CDN blocks that environment's outbound IP at
  the edge (every path, not just the API), confirmed via response headers.
  The fallback logic itself is verified correct through mocked integration
  tests; whether the real call succeeds depends on the network it's
  actually deployed from, and is worth a quick manual check post-deploy.
- **Deployment was explicitly left out of this pass** at the user's
  request, to focus on backend/frontend correctness first. `GET /health`,
  production CORS/env configuration, and a Dockerfile are not yet in place.
- **No rate limiting or abuse protection** on the API — reasonable for a
  single-reviewer demo with no auth, not something a real deployment could
  skip.
- **The pricing defaults were initially wrong** (1.5%/1.5% margins and a
  1-minute cache instead of the brief's 10%/10% and 5 minutes) because an
  earlier pass used a companion prompts-document that specified the
  *formula shape* but not these exact numbers. Caught and fixed during a
  direct re-read of the actual assessment brief, with the one affected test
  updated to match.
