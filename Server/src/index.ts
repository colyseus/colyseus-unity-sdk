import http from "http";
import express from "express";
import cors from "cors";
import { Server } from "colyseus";
import { WebSocketTransport } from "@colyseus/ws-transport";
import { monitor } from "@colyseus/monitor";

import { MyRoom } from "./rooms/MyRoom";

let port = Number(process.env.PORT || 2567);
let app = express();

app.use(cors());
app.use(express.json());

let server = http.createServer(app);

let transport = new WebSocketTransport({
  server,
  pingInterval: 10000,
  pingMaxRetries: 10,
  maxPayload: 1024 * 1024 * 1 * 100, // Maximum message size (1MB == 1024 * 1024).
});

let gameServer = new Server({
  transport,
});

gameServer.define('MyRoom', MyRoom, {
  maxClients: 60,
  allowReconnectionTime: 120
});

app.use("/colyseus", monitor());

gameServer.listen(port);
console.log(`Listening on ws://localhost:${port}`);