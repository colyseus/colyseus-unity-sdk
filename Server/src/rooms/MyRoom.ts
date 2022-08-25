import { Room, Client } from "colyseus";
import { MyRoomState, Player } from "./schema/MyRoomState";

export type Position = {
  x: number,
  y: number
}

export class MyRoom extends Room<MyRoomState> {

  onCreate (options: any) {
    this.setState(new MyRoomState());
  }

  onJoin (client: Client, options: any) {
    console.log(client.sessionId, "joined!");
    
    this.state.players.set(client.sessionId, new Player());

    // Send welcome message to the client.
    client.send("welcomeMessage", "Welcome to Colyseus!");

    // Listen to position changes from the client.
    this.onMessage("position", (client, position: Position) => {
      const player = this.state.players.get(client.sessionId);
      player.x = position.x;
      player.y = position.y;
    });
  }

  onLeave (client: Client, consented: boolean) {
    console.log(client.sessionId, "left!");
  }

  onDispose() {
    console.log("room", this.roomId, "disposing...");
  }

}
