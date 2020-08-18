import http from "http";
import express from "express";
import cors from "cors";

import { Server, LobbyRoom } from "colyseus";
import socialRoutes from "@colyseus/social/express";

import { DemoRoom } from "./rooms/DemoRoom";
import { FossilDeltaRoom } from "./rooms/FossilDeltaRoom";
import { RoomWithoutState } from "./rooms/RoomWithoutState";

const PORT = Number(process.env.PORT || 2567);

const app = express();

/**
 * CORS should be used during development only.
 * Please remove CORS on production, unless you're hosting the server and client on different domains.
 */
app.use(cors());

const gameServer = new Server({
  server: http.createServer(app),
  pingInterval: 0,
});

// Register DemoRoom as "demo"
gameServer.define("demo", DemoRoom);
gameServer.define("lobby", LobbyRoom);
gameServer.define("fossildelta", FossilDeltaRoom);
gameServer.define("no_state", RoomWithoutState);

app.use("/", socialRoutes);

app.get("/something", function (req, res) {
  console.log("something!", process.pid);
  res.send("Hey!");
});

// Listen on specified PORT number
gameServer.listen(PORT);

console.log("Running on ws://localhost:" + PORT);
