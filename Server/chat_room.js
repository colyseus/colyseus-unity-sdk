var Room = require('colyseus').Room;

class ChatRoom extends Room {

  constructor () {
    super();

    this.setState({
      players: {},
      messages: []
    });
  }

  onInit (options) {
    this.setPatchRate( 1000 / 20 );
    this.setSimulationInterval( this.update.bind(this) );

    console.log("ChatRoom created!", options);
  }

  requestJoin (options) {
    console.log("request join!", options);
    return true;
  }

  onJoin (client) {
    console.log("client joined!", client.id);
    this.state.players[client.id] = { x: 0, y: 0 };
  }

  onLeave (client) {
    console.log("client left!", client.id);
    delete this.state.players[client.id];
  }

  onMessage (client, data) {
    console.log(data, "received from", client.id);
    this.state.messages.push(client.id + " sent " + data);
  }

  update () {
    console.log("num clients:", Object.keys(this.clients).length);
    for (var id in this.state.players) {
      this.state.players[id].x++;
    }
  }

  onDispose () {
    console.log("Dispose ChatRoom");
  }

}

module.exports = ChatRoom;
