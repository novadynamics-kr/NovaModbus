# Modbus RTU Conformance Matrix (#24)

Sources: MODBUS over Serial Line V1.02, Application Protocol V1.1b3.
Scope: AGENTS.md §4.2 (18 FCs + listed exceptions; ASCII, MEI 0x2B, custom FC excluded).

## Serial line

| Item | Spec | Status |
|------|------|--------|
| RTU ADU max 256B | Serial §2.5.1.1 | Enforced: framer discards oversize, `TryDecode` rejects, `MaxFrameSize` |
| PDU max 253B | ADU cap math | Enforced: FC17 write ≤121, file totals ≤252, `MaxPduSize` |
| t3.5 frame delimiter | Serial §2.5.1.1 | `RtuFramer` silence wait |
| t3.5 fixed 1.75ms above 19200 | Serial remark | `SilentInterval` |
| t1.5 inter-char discard | Serial §2.5.1.1 | `InterCharInterval` + framer gap logic (750µs fixed above 19200) |
| Master t3.5 quiet after no-response Tx | State diagram | Client enforces after broadcast sends |
| CRC-16/ARC, Lo first | Serial §2.5.1.2 | `Crc16`, covered by official vectors |
| Addresses 1-247 / 0 broadcast / 248-255 reserved | Serial §2.2 | Enforced on engine + transaction |
| CRC-error silence + bus counters | Serial §6.1 | `NoteBusCrcError`, no response |
| Char errors feed 0x0C | Serial §6.1 | `LineError` → `NoteCharError` (SerialPort wired; virtual injectable) |
| 11-bit chars, Even default, no-parity→2 stops, 8 data bits | Serial §2.5.1.2 | `SerialSettings.Validate` + UI options/defaults |

## Function codes

| FC | Client | Slave | Notes |
|----|--------|-------|-------|
| 01/02/03/04 | ✓ | ✓ | quantities 2000/125 enforced both sides |
| 05 | ✓ echo-checked | ✓ FF00/0000 enforced | |
| 06 | ✓ echo-checked | ✓ | |
| 07 | ✓ | ✓ single status byte | polls excluded from event count (R1) |
| 08 echo/counters | generic `DiagnosticsAsync` | ✓ 00/01/02/04/0A/0B/0C/0D/0E/0F/10/11/12/14 | 0x03 ASCII N/A; 0x13+ reserved → 01 |
| 0B/0C | ✓ | ✓ status always 0 (no program commands exist); msg count bus-level | polls excluded from event count |
| 0F (≤1968) | ✓ | ✓ | PDU math exact at cap |
| 10 (≤123) | ✓ | ✓ | |
| 11 | ✓ | ✓ identity + run flag + additional data | |
| 14/15 | ✓ | ✓ per-sub + total PDU checks; oversize answer → 0x04 | file 1..9999/rec 0..9999 via store miss → 0x02 |
| 16 | ✓ echo-checked | ✓ standard formula | |
| 17 (R≤125/W≤121) | ✓ | ✓ write-first order | W121 exact at ADU cap |
| 18 (≤31) | ✓ | ✓ | |
| Exceptions 01-08/0A/0B | parsed | generated 01/02/03/04/06 (+07/08/0A/0B parsed) | NAK(07) never emitted; counter reported |

## Diagnostics & events

- Listen-only: 0x04 enters (monitor only, counts nothing per-store); only 0x01 exits;
  0x01 in LO is silent; malformed 0x01 in LO stays silent + stays LO.
- Restart 0x01: clears LO, resets event counter, FF00 clears log / 0000 keeps; echo otherwise.
- Clear 0x0A: store + bus + NAK counters, diagnostic register; echo.
- Clear overrun 0x14: clears engine overrun; echo.
- Event count: successful completions only (normal + processed-silent incl. broadcast
  writes and resets excluded); never on exceptions/polls (FC07)/fetches (FC0B/0C).
- Event bytes: rx 0x80 (0xC0 broadcast) + tx 0x40 / exception-kind bits
  (01-03→0x41, 04→0x42, 05-06→0x44, 07→0x48); dedicated 0x20 (enter LO) / 0x00 (restart)
  replace the generic send byte; log newest-first, 64 cap.
- 0x0E includes processed broadcast writes; bus counters count every valid line frame.

## Broadcast (address 0)

- Slave executes write FCs 05/06/0F/10/15/16/17 on all stores, per-store
  counters/events, never responds (no exceptions/busy). Reads/FC08/unknown ignored.
- Client `BroadcastAsync` + 6 typed wrappers (0x17 via generic); rejects non-writes
  and oversize frames; t3.5 quiet enforced around sends.

## N/A by design

- ASCII mode, MEI 0x2B, custom function codes, gateway 0x0A/0x0B behavior.
- RS485 driver-enable timing (no OS API via `System.IO.Ports`).
- FC11/12 status bit (no long-running program commands; `SimulateBusy` excepts instead).
- Hardware timing proofs (covered by injectable virtual/fake transports + unit vectors).

## Resolved review points (from plan)

- R1 poll exclusion = FC07 (+FC0B/FC0C): Modicon "poll" terminology.
- R2 listen-only: line-level bus counts continue; per-store actions/counters stop.
- R3 broadcast writes increment per-store event count (successful completion).
- R4 restart resets the event counter only; other counters + diagnostic register untouched.
