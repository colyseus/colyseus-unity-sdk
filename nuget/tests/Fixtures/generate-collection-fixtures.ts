//
// Generates CollectionFixtures.cs — decode fixtures for default-initialized
// collections (codegen emits `= new MapSchema<T>()` / `= new ArraySchema<T>()`,
// matching the JS client where every collection field starts as an empty,
// not-yet-synced instance).
//
// Bytes come from the REAL @colyseus/schema encoder (with a StateView), and
// every scenario is replayed through the JS Decoder + Callbacks with the SAME
// callback registrations the C# tests make. The resulting event log is emitted
// as the expected sequence, so the C# port is held to the JS behavior rather
// than to a hand-written expectation.
//
// Run (from the schema checkout, for its tsconfig):
//   cd ../schema && npx tsx --tsconfig tsconfig.test.json \
//     ../colyseus-unity-sdk/nuget/tests/Fixtures/generate-collection-fixtures.ts
//
import * as assert from "assert";
import * as fs from "fs";
import * as path from "path";
import { fileURLToPath } from "url";
import { schema, t, Encoder, Decoder, StateView } from "../../../../schema/src";
import { Callbacks } from "../../../../schema/src/decoder/strategy/Callbacks";

// Field order/types must match CollectionDefaultsTypes.cs.
const DItem = schema({ v: t.uint8() }, "DItem");
const DHero = schema({
    x: t.number(),
    hp: t.uint8(),
    loot: t.array(DItem).view(1),   // never visible to a default-tag view
}, "DHero");
const DState = schema({
    heroes: t.map(DHero).view(),    // not sent until the view adds an entry
    tick: t.uint16(),
    open: t.map(DHero),
}, "DState");

// float32 fields/collections received into double-typed C# destinations.
const WState = schema({
    f: t.float32(),
    nums: t.array("float32"),
    tags: t.map("float32"),
}, "WState");

type Bytes = number[];
const copy = (u: Uint8Array): Bytes => Array.from(u);

function fullStateForView(encoder: Encoder<any>, view: StateView): Bytes {
    const buf = new Uint8Array(8192);
    const it = { offset: 0 };
    encoder.encodeAll(it, buf);
    return copy(encoder.encodeAllView(view, it.offset, it, buf));
}

function patchForView(encoder: Encoder<any>, view: StateView): Bytes {
    const it = { offset: 0 };
    encoder.encode(it);
    const bytes = copy(encoder.encodeView(view, it.offset, it));
    encoder.discardChanges();
    return bytes;
}

// ─── view-filtered map that starts unsent ────────────────────────────────────
const view = (() => {
    const server = new DState();
    const encoder = new Encoder(server);
    const mk = (hp: number) => { const h = new DHero(); h.x = hp * 1.5; h.hp = hp; h.loot.push(new DItem().assign({ v: hp })); return h; };

    const a = mk(10), b = mk(20), o1 = mk(30);
    server.heroes.set("a", a);
    server.open.set("o1", o1);
    server.tick = 1;
    encoder.discardChanges();

    const sv = new StateView();
    const full = fullStateForView(encoder, sv);
    const patches: Bytes[] = [];

    sv.add(a);                                         // P0: heroes arrives with "a"
    patches.push(patchForView(encoder, sv));
    server.heroes.set("b", b); sv.add(b);              // P1: "b"
    patches.push(patchForView(encoder, sv));
    sv.remove(a);                                      // P2: view drops "a" (its unsent loot must not GC the root)
    patches.push(patchForView(encoder, sv));
    server.tick = 2; server.open.get("o1")!.hp = 31;   // P3: root + another collection still live
    patches.push(patchForView(encoder, sv));
    sv.add(a);                                         // P4: "a" back
    patches.push(patchForView(encoder, sv));
    server.heroes.delete("b");                         // P5: server-side delete
    patches.push(patchForView(encoder, sv));

    // Replay through the JS decoder with the registrations the C# test makes.
    const client = new DState();
    const decoder = new Decoder(client);
    const cb = Callbacks.get(decoder);
    const log: string[] = [];
    const sz = (m: any) => (m ? String(m.size) : "null");
    const initial = client.heroes;
    assert.ok(initial, "the JS client starts with a default (unsynced) collection");

    cb.onAdd("heroes", (h: any, k: string) => log.push(`A.add:${k}:${h.hp}`));
    cb.onRemove("heroes", (h: any, k: string) => log.push(`A.remove:${k}:${h.hp}`));
    cb.listen("heroes", (cur: any, prev: any) => log.push(`L:${sz(cur)}:${sz(prev)}`));
    cb.listen("tick", (cur: number) => log.push(`tick:${cur}`));
    cb.onAdd("open", (h: any, k: string) => log.push(`open.add:${k}:${h.hp}`));

    log.push("--full");
    decoder.decode(Uint8Array.from(full));
    assert.strictEqual(client.heroes, initial, "heroes stays the default instance until the view sends it");
    assert.strictEqual(client.heroes.size, 0);

    // registered after the full state, while heroes is still the unsynced default
    cb.onAdd("heroes", (h: any, k: string) => log.push(`B.add:${k}:${h.hp}`));
    cb.onRemove("heroes", (h: any, k: string) => log.push(`B.remove:${k}:${h.hp}`));

    let replacedOnArrival = false;
    const keyOrder: string[] = [];
    patches.forEach((p, i) => {
        log.push(`--P${i}`);
        decoder.decode(Uint8Array.from(p));
        keyOrder.push(Array.from(client.heroes.keys()).join(","));
        if (i === 0) {
            replacedOnArrival = client.heroes !== initial;
            // registered once heroes is live: immediate replay of what's there
            cb.onAdd("heroes", (h: any, k: string) => log.push(`C.add:${k}:${h.hp}`));
            cb.onRemove("heroes", (h: any, k: string) => log.push(`C.remove:${k}:${h.hp}`));
        }
    });

    assert.deepStrictEqual(Array.from(client.heroes.keys()), ["a"]);
    assert.strictEqual(client.tick, 2);
    assert.strictEqual(client.open.get("o1")!.hp, 31);
    return { full, patches, log, replacedOnArrival, keyOrder };
})();

// ─── float32 into double destinations ────────────────────────────────────────
const wide = (() => {
    const server = new WState();
    const encoder = new Encoder(server);
    server.f = 0.1;
    server.nums.push(0.1, 1 / 3);
    server.tags.set("k", 0.7);
    const bytes = copy(encoder.encodeAll());

    const client = new WState();
    new Decoder(client).decode(Uint8Array.from(bytes));
    const values = [client.f, client.nums[0], client.nums[1], client.tags.get("k")!];
    assert.deepStrictEqual(values, [0.1, 0.1, 1 / 3, 0.7].map(Math.fround));
    return { bytes, values };
})();

// ─── emit ────────────────────────────────────────────────────────────────────
const arr = (b: Bytes) => `new byte[] { ${b.join(", ")} }`;
const str = (s: string) => JSON.stringify(s);
const out = `//
// GENERATED by generate-collection-fixtures.ts (next to this file) against the
// real @colyseus/schema encoder/decoder — do not edit by hand; re-run it.
//

namespace Colyseus.Tests.Fixtures
{
	internal static class CollectionFixtures
	{
		/// <summary>Full state for an EMPTY view: the view-filtered "heroes" map is absent.</summary>
		public static readonly byte[] ViewFull = ${arr(view.full)};

		/// <summary>P0 heroes arrives with "a" · P1 "b" · P2 view drops "a" · P3 tick=2, open.o1.hp=31 · P4 "a" back · P5 "b" deleted.</summary>
		public static readonly byte[][] ViewPatches =
		{
${view.patches.map((p) => `			${arr(p)},`).join("\n")}
		};

		/// <summary>The JS Callbacks event log over the same registrations.</summary>
		public static readonly string[] ViewLog =
		{
${view.log.map((l) => `			${str(l)},`).join("\n")}
		};

		/// <summary>heroes' key order after each patch — JS Map iteration order.</summary>
		public static readonly string[] ViewKeyOrder = { ${view.keyOrder.map(str).join(", ")} };

		/// <summary>JS swaps the default instance for a fresh decoded one when the collection first arrives.</summary>
		public const bool ViewReplacedOnArrival = ${view.replacedOnArrival};

		/// <summary>f=0.1, nums=[0.1, 1/3], tags={k:0.7} — all float32 on the wire.</summary>
		public static readonly byte[] Wide = ${arr(wide.bytes)};

		/// <summary>What the JS decoder yields (Math.fround of each input).</summary>
		public static readonly double[] WideValues = { ${wide.values.map((v) => String(v)).join(", ")} };
	}
}
`;

const here = path.dirname(fileURLToPath(import.meta.url));
fs.writeFileSync(path.join(here, "CollectionFixtures.cs"), out);
console.log("wrote CollectionFixtures.cs —", view.log.length, "log lines");
console.log(view.log.join("\n"));
