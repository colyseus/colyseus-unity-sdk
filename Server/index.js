const cluster = require("cluster");
const express = require("express");

const ClusterServer = require("colyseus").ClusterServer;
const ChatRoom = require('./chat_room');

const PORT = process.env.PORT || 3553;
const gameServer = new ClusterServer();

// Register ChatRoom as "chat"
gameServer.register("chat", ChatRoom);

if (cluster.isMaster) {
    gameServer.listen(PORT);
    gameServer.fork();

} else {
    let app = new express();

    app.get("/something", function (req, res) {
        console.log("something!", process.pid);
        res.send("Hey!");
    });

    // Create HTTP Server
    gameServer.attach({ server: app });
}

console.log("Listening on " + PORT);
