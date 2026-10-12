# Plan: Modbus RTU Spec Compliance (#24)

Sources: MODBUS over Serial Line V1.02 + Application Protocol V1.1b3.
Method: code inventory (Rtu/Client/Slave) cross-checked against spec text.
In-scope FCs per AGENTS.md unchanged (2B/MEI + custom FC stay excluded).

## Conformance matrix (gap only; compliant items omitted)

| # | Spec | Item | Current | Step |
|---|------|------|---------|------|
| G1 | Serial §2.5.1.1 | ADU max 256B enforced | framer accumulates unbounded | S1 |
| G2 | Serial §2.5.1.1 | baud >19200: fixed t3.5=1.75ms | formula + 2ms floor | S1 |
| G3 | Serial §2.5.1.1 | t1.5 inter-char gap → discard frame | missing | S1 |
| G4 | Serial state diagram | master t3.5 quiet before Tx after broadcast (no-response path) | sends immediately | S1 |
| G5 | App §6 / Serial §2.2 | broadcast(0) writes executed, no response | dropped without execution | S2 |
| G6 | App §6.8 (PI_MBUS: FC08 broadcast N/A) | non-write FCs on broadcast ignored | covered by S2 design | S2 |
| G7 | App §6.8.1 | FC08 sub 0x10 NAK count | IllegalFunction | S3 |
| G8 | App §6.8.1 | FC08 sub 0x14 clear overrun + flag | IllegalFunction | S3 |
| G9 | App §6.8 | listen-only state: 0x04 enters (monitor only), only 0x01 exits, 0x01 silent while LO | stateless (0x04 just null) | S3 |
| G10 | App §6.9/6.10 | FC11/12 status 0xFFFF when busy | always 0x0000 | S4 |
| G11 | App §6.9 | event count: successful completion only; excl. exceptions/FC07/FC0B/FC0C | counted on every request | S4 |
| G12 | App §6.10 | FC12 message count = bus-level (== FC08 0x0B) | slave-level count | S4 |
| G13 | App §6.10 | event bytes: rx/tx bit layouts, listen-only=0x20, restart=0x00, newest-first | 0x00/0xFF oldest-first | S4 |
| G14 | App §6.9 | FC08 0x01 also resets event counter | log-only handling | S4 |
| G15 | App §6.17 + ADU cap | FC17 write quantity max 121 (13+2N≤256) | 123 accepted (259B frames) | S5 |
| G16 | Serial §2.5.1.2 | serial setup: Even/1 default, 8 data bits only, no-parity→2 stops | None/One default; 5/6/7 bits + 1.5 stops offered | S5 |
| G17 | App §6.14/6.15 | file sub-request total length ≤ PDU cap | per-sub checks only (verify) | S5 |
| G18 | Serial §6.1 | serial error events (parity/frame/overrun) → overrun counter | ErrorReceived not wired | S3 |

Compliant (no action): 18 FC dispatch both sides, CRC-16/ARC LE, exception codes
01-08/0A/0B, FC05 value rule, PDU length checks per codec, 1-247/broadcast/reserved
addressing, CRC-error silence + bus counters, 0x0A echo + full reset, restart 0000/FF00
validation + log rule, mask-write formula, 0x17 write-first order, FIFO 31 cap,
event-log 64 cap, response slaveId/function/echo validation in transaction loop.

## Steps (1 step = 1 commit, code + docs + tests)

### S1 — Framing & timing hardening (`RtuFramer`, transaction)
- 256B cap: discard oversize frames in `ReadFrameAsync` (no unbounded growth).
- `SilentInterval`: baud >19200 → fixed 1.75ms (spec remark); add `InterCharInterval`
  (formula 1.5char, >19200 → 750µs).
- t1.5 guard: per-byte timing in framer; gap > t1.5 mid-frame → discard, restart.
  VirtualTransport delay injection for tests.
- Master Tx quiet: t3.5 wait after no-response sends (broadcast) before next Tx.
  Client/slave-owned timestamp (no `ISerialTransport` change); override hook for tests.
- Tests: oversize discard, fixed-timer vectors, t1.5 discard/accept, broadcast-quiet timing.

### S2 — Broadcast support (slave + client)
- Slave `HandleRequest` addr 0: execute write FCs 05/06/0F/10/15/16/17 on ALL stores,
  per-store NoteSlaveMessage + rx(broadcast bit)/tx-no-response events, no response,
  no exception responses. Reads/FC08/unknown on broadcast: ignored (bus count only).
- Client: `BroadcastAsync` fire-and-forget write API (typed wrappers for the 7 write FCs);
  `ExchangeAsync` keeps rejecting 0. Enforce post-broadcast t3.5 (S1 hook).
- Tests: loopback apply-to-all-stores, silence (no bytes back), FC08-broadcast ignored.

### S3 — Diagnostics completion (store + slave + transport)
- `ModbusDataStore`: add `NakCount` (+NoteNak), `OverrunCount` (+NoteOverrun, included
  in `ResetCounters`), `ListenOnly` flag.
- FC08: sub 0x10 → NAK counter; sub 0x14 → clear overrun (+ echo); 0x0A also clears NAK/overrun.
- Listen-only: 0x04 sets flag (response null as today); while set, only 0x01 processed
  (exits flag, no response per spec); all else null without action/counters.
- `SystemPortsSerialTransport`: wire `SerialPort.ErrorReceived` → surfaced counter
  callback (parity/frame/overrun → overrun; virtual/fake transports: manual note API).
- Tests: subfunction vectors, LO enter/exit/silence, restart-in-LO silence, 0x0A clears all.

### S4 — Counter & event-log semantics (store + slave)
- FC11/FC12 status: 0xFFFF when busy (`SimulateBusy`), else 0x0000.
- FC12 message count: `engine.BusMessageCount`.
- Event counting rework in `HandleRequest`: count + rx/tx event bytes only on
  successful completion; exclude exceptions and FC07/FC0B/FC0C polls
  (FC07 exclusion = review point).
- Event bytes per §6.10 bit layouts (rx bit7=1 incl. broadcast bit6; tx bit7=0/bit6=1
  incl. exception-kind bits; listen-only 0x20; restart 0x00); log order newest-first.
- FC08 0x01 also resets the event counter (keeps other counters; FF00 log rule unchanged).
- Tests: FC11/12 vectors (status busy/ready, bus-level msg count, newest-first order,
  exclusion rules), spec FC12 example shape.

### S5 — PDU limits + serial setup (codecs + settings + UI)
- `ReadWriteMultipleCodec.ValidateWriteCount`: max 123 → 121 (client+slave share codec).
- File record: verify + enforce total request/response length vs PDU cap (S…); fix if needed.
- `SerialSettings` default Even/One; `BuildSettings` enforces 11-bit rule
  (8 data bits only; None→Two stops); UI: DataBits fixed 8, stop options One/Two,
  parity default Even (Client + Slave settings VMs).
- Tests: 0x17 quantity 122/123 rejection, oversize file-request rejection,
  settings-validation unit tests.

### S6 — Docs & gates
- `docs/tech/modbus-rtu-conformance.md`: full matrix (gap + compliant), spec refs.
- Gate: build 0 warnings → test all pass → spec-vector re-check → no regressions.
- Merge → push `nova main` → close #24 (separate approval).

## Open review points
- R1: "poll commands" exclusion set (FC07 included by Modicon terminology — confirm).
- R2: listen-only + bus-message counting (count line detections, skip per-store actions).
- R3: broadcast + event-count increment (propose yes: successful completion; confirm).
- R4: restart 0x01 scope (event counter reset yes; other counters + diag reg untouched — confirm).
