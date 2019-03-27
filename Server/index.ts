import * as http from "http";
import * as express from "express";

import { Server, serialize, FossilDeltaSerializer } from "colyseus";
import { DemoRoom } from "./DemoRoom";

const PORT = Number(process.env.PORT || 2567);

const app = express();
const gameServer = new Server({
  server: http.createServer(app)
});

// Register DemoRoom as "demo"
gameServer.register("demo", DemoRoom);

/**
 * FossilDelta demo (deprecated)
 */
@serialize(FossilDeltaSerializer)
class DemoRoomFossilDelta extends DemoRoom {}
gameServer.register("demo_fossil", DemoRoomFossilDelta);

app.get("/something", function (req, res) {
  console.log("something!", process.pid);
  res.send("Hey!");
});

// Listen on specified PORT number
gameServer.listen(PORT);

console.log("Running on ws://localhost:" + PORT);
