# Colyseus Room Inspector

Unity Editor tool for inspecting connected Colyseus room states in real-time during Play mode.

## Usage

1. Open **Window > Colyseus > Room Inspector**
2. Enter Play mode and connect to a Colyseus server
3. Active rooms are automatically discovered and displayed

### Toolbar

| Button | Description |
|--------|-------------|
| **Auto Refresh** | Toggle automatic updates (every 0.5s) |
| **Refresh Now** | Manually refresh the display |
| **Copy State JSON** | Copy current state to clipboard |

### Example Output

Given a room with `MapSchema<Player> players` and `float gameTime`:

```
Room: my_room (abc123)
  +-- Connection Info
  |   +-- Room ID: abc123
  |   +-- Session ID: xyz789
  |   +-- Connection: Connected
  |   +-- Source Object: NetworkManager
  +-- Room State
      +-- State Type: MyRoomState
      +-- players (MapSchema) [2 items]
      |   +-- [player1] (Player)
      |   |   +-- x: 10.5
      |   |   +-- y: 20.3
      |   |   +-- name: "Alice"
      |   +-- [player2] (Player)
      |       +-- x: 15.2
      |       +-- y: 18.7
      |       +-- name: "Bob"
      +-- gameTime: 45.2
```

## Supported Types

- Primitives (int, float, string, bool)
- Nested Schema objects
- `MapSchema<T>` and `ArraySchema<T>` collections

## Limitations

- **Play mode only** -- not available in Edit mode
- **Read-only** -- cannot edit state values
- Collections limited to **100 items** displayed
- Nesting limited to **10 levels** deep

## Troubleshooting

**"No active Colyseus rooms found"** -- Ensure you are in Play mode and have connected to a room. The inspector discovers rooms by scanning MonoBehaviour fields via reflection.

**State shows as "null"** -- The room is connected but hasn't received the initial state yet. Wait a moment or check your server-side room.

**Values not updating** -- Check that Auto Refresh is enabled in the toolbar and that the room is still connected.

---

# Schema Codegen

Unity Editor tool for generating client-side C# schema classes from your server's
TypeScript schema definitions, by running the `schema-codegen` tool from the
[`@colyseus/schema`](https://www.npmjs.com/package/@colyseus/schema) npm package.

## Requirements

- [Node.js](https://nodejs.org/) installed. The tool runs `npx schema-codegen` under the hood.

## Usage

1. Open **Window > Colyseus > Schema Codegen**
2. Confirm Node.js is detected (green check). If not, see *Node not found* below.
3. Set **Schema source** to a `.ts` file, a folder of `*.ts` files, or a glob (e.g. `src/rooms/schema/*.ts`) -- usually inside your server project.
4. Set **Output directory** to a folder under `Assets/` so Unity imports the result automatically.
5. (Optional) Set a **Namespace**, pin a **version**, or enable **Bundle into single file**.
6. Click **Generate**. On success the generated files are written and the AssetDatabase refreshes.

## Options

| Field | Maps to | Description |
|-------|---------|-------------|
| Schema source | `[path]` | `.ts` file, folder, or glob to read schema definitions from |
| Output directory | `--output` | Destination for generated `.cs` files |
| Namespace | `--namespace` | Wraps generated classes in a namespace |
| @colyseus/schema version | `-p @colyseus/schema@<v>` | Pin the codegen version to match your server. Leave blank to use a locally installed version (when a `package.json` is found) or the latest published version |
| Bundle into single file | `--bundle` | Writes all classes to one `.cs` file |

## Shared (team) settings

The schema source, output directory, namespace, version and bundle options are saved to
`ProjectSettings/Packages/io.colyseus.sdk/SchemaCodegen.json` and **committed to git**, so everyone
on the project generates with the same configuration. Paths are stored relative to the project root
(e.g. `../server/src/schema`) so they resolve on each teammate's machine.

The **Node bin directory** is the one exception: it is specific to each developer's Node install, so
it is stored per-machine in `EditorPrefs` and is *not* committed.

## Troubleshooting

**Node not found** -- Set the **Node bin directory** manually (the folder containing `node`/`npx`),
or click **Detect**. On macOS, a Unity Editor launched from Finder/Dock does *not* inherit your
shell's `PATH`, so an [nvm](https://github.com/nvm-sh/nvm)-installed Node is invisible. Either point
the field at your nvm `bin` directory, or symlink Node into a location Unity can see:

```sh
ln -s -f "$(which node)" /usr/local/bin/node
ln -s -f "$(which npx)" /usr/local/bin/npx
```

**Output not appearing in the Project** -- Make sure the output directory is under `Assets/`. Files
written outside `Assets/` are not imported by Unity.

**Schema mismatch / decode errors at runtime** -- Pin the **@colyseus/schema version** to the same
version your server uses, so the generated classes match the wire format.
