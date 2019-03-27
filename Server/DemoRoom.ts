import { Room, Client } from "colyseus";
import { Schema, type, MapSchema } from "@colyseus/schema";

class Player extends Schema {
  @type("number")
  x: number = 0;

  @type("number")
  y: number = 0;

  @type("boolean")
  connected: boolean = true;
}

class State extends Schema {
  @type({ map: Player })
  players = new MapSchema<Player>();
}

export class DemoRoom extends Room {

  constructor () {
    super();

    this.setState(new State());
  }

  onInit (options: any) {
    console.log("DemoRoom created!", options);

    this.setPatchRate(1000 / 20);
    this.setSimulationInterval((dt) => this.update(dt));
  }

  requestJoin (options: any) {
    console.log("request join!", options);
    return true;
  }

  onJoin (client: Client, options: any) {
    console.log("client joined!", client.sessionId);
    this.state.players[client.sessionId] = new Player();
  }

  async onLeave (client: Client, consented: boolean) {
    this.state.players[client.sessionId].connected = false;

    try {
      if (consented) {
        throw new Error("consented leave!");
      }

      console.log("let's wait for reconnection!")
      const newClient = await this.allowReconnection(client, 10);
      console.log("reconnected!", newClient.sessionId);

    } catch (e) {
      console.log("disconnected!", client.sessionId);
      delete this.state.players[client.sessionId];
    }
  }

  onMessage (client: Client, data: any) {
    console.log(data, "received from", client.sessionId);

    if (data === "move_right") {
      this.state.players[client.sessionId].x += 0.01;
    }
    console.log(this.state.players[client.sessionId].x);

    this.broadcast({ hello: "hello world" });
  }

  update (dt?: number) {
    // console.log("num clients:", Object.keys(this.clients).length);
  }

  onDispose () {
    console.log("disposing DemoRoom...");
  }

}