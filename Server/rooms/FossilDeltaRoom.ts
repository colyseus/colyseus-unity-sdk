import { Room, Client, generateId } from "colyseus";

export class FossilDeltaRoom extends Room {

  onCreate (options: any) {
    console.log("FossilDeltaRoom created.");
    this.setState({
      array: [],
      entities: {},
      int: 1,
      bool: true,
      string: "string",
    });

    this.onMessage("*", (client, type, message) => {
      this.state.entities[client.sessionId].x += 1;
      this.state.entities[client.sessionId].y -= 1;
    })
  }

  onJoin (client: Client, options: any) {
    this.state.entities[client.sessionId] = { x: 10, y: 10, name: "Jake Badlands" };
    this.state.array.push(1);
  }

  async onLeave (client: Client, consented: boolean) {
    delete this.state.entities[client.sessionId];
  }

  onDispose() {
    console.log("FossilDeltaRoom disposed.");
  }

}
