var Room = require('colyseus').Room;

module.exports = class DemoRoom extends Room {

  constructor () {
    super();

    this.setState({
      players: {},
    });
  }

  onInit (options) {
    console.log("DemoRoom created!", options);

    this.setPatchRate( 1000 / 20 );
    this.setSimulationInterval( this.update.bind(this) );
  }

  requestJoin (options) {
    console.log("request join!", options);
    return true;
  }

  onJoin (client) {
    console.log("client joined!", client.sessionId);
    this.state.players[client.sessionId] = { x: 0, y: 0, connected: true };
  }

  async onLeave (client, consented) {
    this.state.players[client.sessionId].connected = false;

    try {
      if (consented) {
        throw new Error("consented leave!");
      }

      console.log("let's wait for reconnection!")
      const newClient = await this.allowReconnection(client, 20);
      console.log("reconnected!", newClient.sessionId);

    } catch (e) {
      delete this.state.players[client.sessionId];
      console.log(e);
    }
  }

  onMessage (client, data) {
    console.log(data, "received from", client.sessionId);

    if (data === "move_right") {
      this.state.players[client.sessionId].x += 0.01;
    }

    this.broadcast({ hello: "hello world" });
  }

  update () {
    // console.log("num clients:", Object.keys(this.clients).length);
  }

  onDispose () {
    console.log("disposing DemoRoom...");
  }

}