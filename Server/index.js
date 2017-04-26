"use strict";

var colyseus = require('colyseus')
  , http = require('http')

  , express = require('express')
  // , cors = require('cors')

  , port = process.env.PORT || 3553
  , app = express()
  , server = http.createServer(app)
  , gameServer = new colyseus.Server({ server: server })

  , ChatRoom = require('./chat_room');

gameServer.on('connect', function(client) {
  console.log(client.id, "connected");
})

gameServer.on('message', function(client, data) {
  console.log(client.id, "sent", data);
})

gameServer.on('disconnect', function(client) {
  console.log(client.id, "disconnected");
})

gameServer.register('chat', ChatRoom)

app.use(express.static( __dirname ))
server.listen(port, '127.0.0.1');

console.log(`Listening on http://localhost:${ port }`)
