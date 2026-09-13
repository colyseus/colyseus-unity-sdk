//
// Generates ResendFixtures.cs — the patch that follows a client's full state.
// Core sends the full state on the JOIN_ROOM ack and includes the client in the
// next patch, so that patch re-sends every op the full state already holds.
// Those ops must decode as unchanged: no re-inserted array entries, no
// re-fired onAdd / listen. The reconnect resync of surviving primitives is
// covered too.
//
// Every scenario is replayed through the REAL JS Decoder + Callbacks, and its
// event log and final state are emitted as the expected values.
//
// Run (from the schema checkout, for its tsconfig):
//   cd ../schema && npx tsx --tsconfig tsconfig.test.json \
//     ../colyseus-unity-sdk/nuget/tests/Fixtures/generate-resend-fixtures.ts
//
import * as assert from "assert";
import * as fs from "fs";
import * as path from "path";
import { fileURLToPath } from "url";
import { schema, t, Encoder, Decoder } from "../../../../schema/src";
import { Callbacks } from "../../../../schema/src/decoder/strategy/Callbacks";

// Field order/types must match ResendTypes.cs.
const RUnit = schema({ name: t.string() }, "RUnit");
const RState = schema({
    label: t.string(),
    score: t.number(),
    ratio: t.float32(),
    order: t.array("string"),
    nums: t.array("number"),
    units: t.array(RUnit),
    names: t.map("string"),
}, "RState");

type Bytes = number[];
const copy = (u: Uint8Array): Bytes => Array.from(u);
const full = (e: Encoder<any>): Bytes => { const it = { offset: 0 }; return copy(e.encodeAll(it).subarray(0, it.offset)); };
const patch = (e: Encoder<any>): Bytes => { const it = { offset: 0 }; const b = copy(e.encode(it).subarray(0, it.offset)); e.discardChanges(); return b; };

const show = (s: any) =>
    `label=${s.label} score=${s.score} ratio=${s.ratio} order=[${[...s.order]}] nums=[${[...s.nums]}] ` +
    `units=[${[...s.units].map((u: any) => u.name)}] names=[${[...s.names].map(([k, v]: any) => `${k}:${v}`)}]`;

// The registrations the C# test makes (immediate = false on both sides).
function register(decoder: Decoder<any>, log: string[]) {
    const cb = Callbacks.get(decoder);
    cb.listen("label", (cur: any) => log.push(`label=${cur}`), false);
    cb.listen("score", (cur: any) => log.push(`score=${cur}`), false);
    cb.listen("ratio", (cur: any) => log.push(`ratio=${cur}`), false);
    cb.onAdd("order", (v: any, i: number) => log.push(`order+${i}=${v}`), false);
    cb.onRemove("order", (v: any, i: number) => log.push(`order-${i}=${v}`));
    cb.onAdd("nums", (v: any, i: number) => log.push(`nums+${i}=${v}`), false);
    cb.onRemove("nums", (v: any, i: number) => log.push(`nums-${i}=${v}`));
    cb.onAdd("units", (u: any, i: number) => log.push(`units+${i}=${u.name}`), false);
    cb.onRemove("units", (u: any, i: number) => log.push(`units-${i}=${u.name}`));
    cb.onAdd("names", (v: any, k: string) => log.push(`names+${k}=${v}`), false);
    cb.onRemove("names", (v: any, k: string) => log.push(`names-${k}=${v}`));
}

// ─── two clients ack JOIN_ROOM before the next patch tick ────────────────────
const joins = (() => {
    const server = new RState();
    const encoder = new Encoder(server);
    const join = (sid: string, n: number) => {
        server.label = `${sid} joined`;
        server.score = n;
        server.ratio = 1 / (2 * n);
        server.order.push(sid);
        server.nums.push(n);
        const u = new RUnit(); u.name = sid; server.units.push(u);
        server.names.set(sid, sid);
    };

    join("A", 1); const fullA = full(encoder);
    join("B", 2); const fullB = full(encoder);
    const tick1 = patch(encoder);          // re-sends both joins
    server.order.push("C");
    server.names.set("C", "C");
    server.label = "later";
    server.score = 3;
    const tick2 = patch(encoder);          // genuine changes still fire

    const replay = (first: Bytes) => {
        const client = new RState();
        const decoder = new Decoder(client);
        const log: string[] = [];
        register(decoder, log);
        for (const [name, frame] of [["full", first], ["tick1", tick1], ["tick2", tick2]] as const) {
            log.push(`--${name}`);
            decoder.decode(Uint8Array.from(frame));
        }
        assert.strictEqual(show(client), show(server), "the JS client matches the server");
        return { log, state: show(client) };
    };
    return { fullA, fullB, tick1, tick2, a: replay(fullA), b: replay(fullB) };
})();

// ─── reconnect: a resync snapshot over live state ────────────────────────────
const resync = (() => {
    const server = new RState();
    const encoder = new Encoder(server);
    server.label = "live"; server.score = 1; server.ratio = 0.5;
    server.order.push("x", "y"); server.nums.push(1, 2);
    const u = new RUnit(); u.name = "u"; server.units.push(u);
    server.names.set("k1", "a"); server.names.set("k2", "b");
    const joinFull = full(encoder); patch(encoder);

    // while the client is off the wire
    server.names.delete("k2"); server.score = 5; server.order.pop();
    patch(encoder);
    const resyncFull = full(encoder);

    const client = new RState();
    const decoder = new Decoder(client);
    decoder.decode(Uint8Array.from(joinFull));
    const log: string[] = [];
    register(decoder, log);
    decoder.decodeResync(Uint8Array.from(resyncFull));
    assert.strictEqual(show(client), show(server), "the JS client matches the server");
    return { joinFull, resyncFull, log, state: show(client) };
})();

// ─── emit ────────────────────────────────────────────────────────────────────
const arr = (b: Bytes) => `new byte[] { ${b.join(", ")} }`;
const str = (s: string) => JSON.stringify(s);
const lines = (l: string[]) => l.map((x) => `\t\t\t${str(x)},`).join("\n");
const out = `//
// GENERATED by generate-resend-fixtures.ts (next to this file) against the
// real @colyseus/schema encoder/decoder — do not edit by hand; re-run it.
//

namespace Colyseus.Tests.Fixtures
{
	internal static class ResendFixtures
	{
		/// <summary>Full state the first client (A) got on its ack.</summary>
		public static readonly byte[] FullA = ${arr(joins.fullA)};

		/// <summary>Full state the joiner (B) got on its ack, before the patch tick.</summary>
		public static readonly byte[] FullB = ${arr(joins.fullB)};

		/// <summary>The next patch: re-sends both joins.</summary>
		public static readonly byte[] Tick1 = ${arr(joins.tick1)};

		/// <summary>order + "C", names.C, label = "later", score = 3.</summary>
		public static readonly byte[] Tick2 = ${arr(joins.tick2)};

		/// <summary>The JS Callbacks log for client A over full / tick1 / tick2.</summary>
		public static readonly string[] LogA =
		{
${lines(joins.a.log)}
		};

		/// <summary>The JS Callbacks log for client B over full / tick1 / tick2.</summary>
		public static readonly string[] LogB =
		{
${lines(joins.b.log)}
		};

		public const string StateAfterTicks = ${str(joins.b.state)};

		/// <summary>Join snapshot, then (offline) names.k2 deleted, score = 5, order popped.</summary>
		public static readonly byte[] ResyncJoin = ${arr(resync.joinFull)};

		public static readonly byte[] ResyncSnapshot = ${arr(resync.resyncFull)};

		/// <summary>The JS Callbacks log for decodeResync over the join state.</summary>
		public static readonly string[] ResyncLog =
		{
${lines(resync.log)}
		};

		public const string ResyncState = ${str(resync.state)};
	}
}
`;

const here = path.dirname(fileURLToPath(import.meta.url));
fs.writeFileSync(path.join(here, "ResendFixtures.cs"), out);
console.log("wrote ResendFixtures.cs");
console.log("A:", joins.a.log.join(" | "));
console.log("B:", joins.b.log.join(" | "));
console.log("resync:", resync.log.join(" | "));
console.log("state:", joins.b.state);
