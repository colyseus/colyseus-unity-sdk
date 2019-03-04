const http = require("http");
const express = require("express");

const colyseus = require("colyseus");
const DemoRoom = require('./demo_room');

const PORT = process.env.PORT || 2567;

const app = new express();
const gameServer = new colyseus.Server({
  server: http.createServer(app)
});

// Register DemoRoom as "chat"
gameServer.register("demo", DemoRoom);

app.get("/something", function (req, res) {
  console.log("something!", process.pid);
  res.send("Hey!");
});

// Listen on specified PORT number
gameServer.listen(PORT);

console.log("Running on ws://localhost:" + PORT);
