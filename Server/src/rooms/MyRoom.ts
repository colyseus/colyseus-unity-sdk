import { Room, Client } from "colyseus";
import { MyRoomState, Player } from "./schema/MyRoomState";

export type PositionPack = {
  x: number;
  y: number;
}

export class MyRoom extends Room<MyRoomState> {

  onCreate (options: any) {
    // Configuring room
    this.setPatchRate(1000 / 20);
    this.setState(new MyRoomState());

    // Listening to client's messages

    // Listen to transform changes from the client.
    this.onMessage("position", (client, position) => this.onPlayerMove(client, position));
  }

  onJoin (client: Client, options: any) {
    console.log(client.sessionId, "joined!");

    this.state.players.set(client.sessionId, new Player());

    // Send welcome message to the client.
    client.send("welcomeMessage", "Welcome to Colyseus!");
  }

  onPlayerMove(client: Client, position: PositionPack) {
    const player = this.state.players.get(client.sessionId);
    player.x = position.x;
    player.y = position.y;

    console.log({ clientId: client.id, position });
  }

  onLeave (client: Client, consented: boolean) {
    this.state.players.delete(client.sessionId);
    console.log(client.sessionId, "left!");
  }

  onDispose() {
    console.log("room", this.roomId, "disposing...");
  }

}
