var colyseus = require('colyseus')
  , ChatRoom = require('./chat_room')
  , http = require('http')
  , express = require('express')
  , port = process.env.PORT || 2657
  , app = express();

var server = http.createServer(app)
  , gameServer = new colyseus.Server({server: server})

gameServer.on('connection', function(client) {
  console.log(client.id, "connected");
})
gameServer.on('disconnect', function(client) {
  console.log(client.id, "disconnected");
})

gameServer.register(ChatRoom)

app.use(express.static(__dirname));
server.listen(port);

console.log(`Listening on http://localhost:${ port }`)
