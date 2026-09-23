# Colyseus Editor Tools

Three windows under **Window > Colyseus**:

| Window | What it does |
|--------|--------------|
| **Game Server** | Starts and stops your Node.js server, and streams its output into Unity |
| **Schema Codegen** | Generates C# schema classes from your server's TypeScript definitions |
| **Room Inspector** | Inspects connected room state in real time during Play mode |

---

# Game Server

Runs your local Colyseus server without leaving the editor.

## Usage

1. Open **Window > Colyseus > Game Server**.
2. Press **Detect** to find your server project, or **Browse** to pick the folder holding its `package.json`. The command is filled in from that project's lockfile and `scripts` (`pnpm dev`, `npm run dev`, and so on).
3. Press **Start Server**. Output appears in the console below, timestamped and colored by severity.

The status line shows the process id, how long the server has been up, and the port.

## Fields

| Field | Description |
|-------|-------------|
| Working directory | The folder containing your server's `package.json`. Stored relative to the Unity project, so it resolves on a teammate's machine |
| Command | What to run. The **Scripts** button lists the scripts from `package.json` |
| Port | Passed to the server as the `PORT` environment variable |
| Auto-start on Play | Starts the server when you enter Play mode, if it isn't already running |

## Surviving recompiles

The server is **not** a child of the editor's script domain, so editing C# never restarts it. Output is redirected to `Library/Colyseus/server.log` and the window tails that file, which means a domain reload cannot wedge the server by leaving its output pipe unread. After a reload the panel re-attaches to the running process and backfills recent output; those backfilled lines carry no timestamp, because only the time they were read is known.

Re-attaching also works across editor restarts, so a server left running on purpose (or orphaned by a crash) is picked up by the next session rather than fighting you for the port.

**Stop Server** terminates the whole process tree — the shell, the package manager and the Node process it spawned — with `SIGTERM` first and `SIGKILL` for anything left (`taskkill /T /F` on Windows). Quitting Unity does the same, unless you turn off *Stop Server When Unity Quits* in the overflow menu.

## The overflow menu

The button at the right of the header (and the tab's own context menu) holds the rest: install dependencies, jump to the other two windows, generate schema files with the saved codegen settings, reveal the working directory, or open the raw log file.

## Shared (team) settings

The working directory, command, port and auto-start flag are written to
`ProjectSettings/Packages/io.colyseus.sdk/Server.json` and **committed to git**, so everyone on the project gets a working Start button. *Stop server when Unity quits* is per-machine.

## Troubleshooting

**Node not found** — see *Node not found* under Schema Codegen below; both windows share one setting.

**Port already in use** — the panel spots `EADDRINUSE` and says so. Another server is still holding the port: stop it, or change the port.

**Dependencies are not installed** — the panel offers an **Install** button that runs your package manager's install and streams it into the same console.

**Windows firewall prompt** — Windows asks to allow Node through the firewall the first time. Allowing it on private networks is enough for local development.

---

# Schema Codegen

Generates client-side C# schema classes from your server's TypeScript schema
definitions, by running the `schema-codegen` tool from the
[`@colyseus/schema`](https://www.npmjs.com/package/@colyseus/schema) npm package.

## Requirements

- [Node.js](https://nodejs.org/) installed. The tool runs `npx schema-codegen` under the hood.

## Usage

1. Open **Window > Colyseus > Schema Codegen**.
2. Confirm Node.js is detected (green dot). If not, see *Node not found* below.
3. Press **Detect** to search your server project for `.ts` files that import `@colyseus/schema`, or set **Schema source** yourself to a file, a folder, or a glob such as `src/rooms/schema/**/*.ts`.
4. Set **Output directory** to a folder under `Assets/` so Unity imports the result automatically.
5. (Optional) Set a **Namespace**, pin a **version**, or enable **Bundle into single file**.
6. Click **Generate**.

## Options

| Field | Maps to | Description |
|-------|---------|-------------|
| Schema source | `[path]` | `.ts` file, folder, or glob to read schema definitions from. `**` recurses |
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
it is stored per-machine in `EditorPrefs` and is *not* committed. Both this window and the Game
Server window share it.

## Troubleshooting

**Node not found** — Press **Configure…** next to the node version, then set the **Node bin directory**
(the folder containing `node`/`npx`) or press **Detect**. On macOS, a Unity Editor launched from
Finder/Dock does *not* inherit your shell's `PATH`, so an [nvm](https://github.com/nvm-sh/nvm)-installed
Node is invisible. Either point the field at your nvm `bin` directory, or symlink Node into a location
Unity can see:

```sh
ln -s -f "$(which node)" /usr/local/bin/node
ln -s -f "$(which npx)" /usr/local/bin/npx
```

**Output not appearing in the Project** — Make sure the output directory is under `Assets/`. Files
written outside `Assets/` are not imported by Unity.

**Schema mismatch / decode errors at runtime** — Pin the **@colyseus/schema version** to the same
version your server uses, so the generated classes match the wire format.

---

# Room Inspector

Inspects connected Colyseus room states in real time during Play mode.

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
