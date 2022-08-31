import { MapSchema, Schema, Context, type } from "@colyseus/schema";

export class Player extends Schema {

  @type("number") x: number = 0;
  @type("number") y: number = 0;

}

export class MyRoomState extends Schema {
	@type({map: Player})
	players: MapSchema<Player> = new MapSchema<Player>();
}
