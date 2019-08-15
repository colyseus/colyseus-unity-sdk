import http from "http";
import express from "express";
import cors from "cors";

import { Server, serialize, FossilDeltaSerializer } from "colyseus";
import { DemoRoom } from "./DemoRoom";

import socialRoutes from "@colyseus/social/express";

const PORT = Number(process.env.PORT || 2567);

const app = express();

/**
 * CORS should be used during development only.
 * Please remove CORS on production, unless you're hosting the server and client on different domains.
 */
app.use(cors());
app.use(express.json());

const gameServer = new Server({
  server: http.createServer(app),
  express: app,
  pingTimeout: 0
});

// Register DemoRoom as "demo"
gameServer.define("demo", DemoRoom);

app.use("/", socialRoutes);

app.get("/something", function (req, res) {
  console.log("something!", process.pid);
  res.send("Hey!");
});

// Listen on specified PORT number
gameServer.listen(PORT);

console.log("Running on ws://localhost:" + PORT);
