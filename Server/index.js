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

gameServer.register('chat', ChatRoom)

app.use(express.static( __dirname ))
gameServer.listen(port, '127.0.0.1');

console.log(`Listening on http://localhost:${ port }`)
