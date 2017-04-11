var Room = require('colyseus').Room;

class ChatRoom extends Room {

  constructor (options) {
    super(options);

    this.setPatchRate( 1000 / 20 );
    this.setSimulationInterval( this.update.bind(this) );

    this.setState({
      players: {}
    });

    console.log("ChatRoom created!", options);
  }

  requestJoin (options) {
    return true;
  }

  onJoin (client) {
    this.state.players[client.id] = { x: 0, y: 0 };
  }

  onLeave (client) {
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
