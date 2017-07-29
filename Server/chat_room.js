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
    this.state.messages.push("client " + client.id + " joined");
  }

  onLeave (client) {
    this.state.messages.push("client " + client.id + " left");
    delete this.state.players[client.id];
  }

  onMessage (client, data) {
    console.log(data, "received from", client.id);
  }

  update () {
    for (var id in this.state.players) {
      this.state.players[id].x++;
    }
  }

  dispose () {
    console.log("Dispose ChatRoom");
  }

}

module.exports = ChatRoom;
