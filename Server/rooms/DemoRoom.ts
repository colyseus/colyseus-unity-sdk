import { Room, Client, generateId } from "colyseus";
import { Schema, MapSchema, ArraySchema, Context } from "@colyseus/schema";
import { verifyToken, User, IUser } from "@colyseus/social";

// Create a context for this room's state data.
const type = Context.create();

class Entity extends Schema {
  @type("number") x: number = 0;
  @type("number") y: number = 0;
}

class Player extends Entity {
  @type("boolean") connected: boolean = true;
}

class Enemy extends Entity {
  @type("number") power: number = Math.random() * 10;
}

class State extends Schema {
  @type({ map: Entity }) entities = new MapSchema<Entity>();
}

/**
 * Demonstrate sending schema data types as messages
 */
class Message extends Schema {
  @type("number") num;
  @type("string") str;
}

export class DemoRoom extends Room {

  onCreate (options: any) {
    console.log("DemoRoom created.", options);

    this.setState(new State());
    this.populateEnemies();

    this.setMetadata({
      str: "hello",
      number: 10
    });

    this.setPatchRate(1000 / 20);
    this.setSimulationInterval((dt) => this.update(dt));

    this.onMessage(0, (client, message) => {
      client.send(0, message);
    });

    this.onMessage("schema", (client) => {
      const message = new Message();
      message.num = Math.floor(Math.random() * 100);
      message.str = "sending to a single client";
      client.send(message);
    })

    this.onMessage("move_right", (client) => {
      this.state.entities[client.sessionId].x += 0.01;

      this.broadcast("hello", { hello: "hello world" });
    });

    this.onMessage("*", (client, type, message) => {
      console.log(`received message "${type}" from ${client.sessionId}:`, message);
    });
  }

  async onAuth (client, options) {
    console.log("onAuth(), options!", options);
    return await User.findById(verifyToken(options.token)._id);
  }

  populateEnemies () {
    for (let i=0; i<=3; i++) {
      const enemy = new Enemy();
      enemy.x = Math.random() * 2;
      enemy.y = Math.random() * 2;
      this.state.entities[generateId()] = enemy;
    }
  }

  onJoin (client: Client, options: any, user: IUser) {
    console.log("client joined!", client.sessionId);
    this.state.entities[client.sessionId] = new Player();

    client.send("type", { hello: true });
  }

  async onLeave (client: Client, consented: boolean) {
    this.state.entities[client.sessionId].connected = false;

    try {
      if (consented) {
        throw new Error("consented leave!");
      }

      console.log("let's wait for reconnection!")
      const newClient = await this.allowReconnection(client, 10);
      console.log("reconnected!", newClient.sessionId);

    } catch (e) {
      console.log("disconnected!", client.sessionId);
      delete this.state.entities[client.sessionId];
    }
  }


  update (dt?: number) {
    // console.log("num clients:", Object.keys(this.clients).length);
  }

  onDispose () {
    console.log("DemoRoom disposed.");
  }

}
