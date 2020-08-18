import { Room, Client } from "colyseus";

export class RoomWithoutState extends Room {

  onCreate (options: any) {
    console.log("RoomWithoutState created.");
  }

  onJoin (client: Client, options: any) {
    console.log(client.sessionId, "joined!");
    client.send("hello", "sending some data");
  }

  onLeave (client: Client, consented: boolean) {
    console.log(client.sessionId, "left!", { consented });
  }

  onDispose () {
    console.log("RoomWithoutState disposed.");
  }

}
