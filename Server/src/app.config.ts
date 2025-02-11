import config from "@colyseus/tools";

import { WebSocketTransport } from "@colyseus/ws-transport";
import { monitor } from "@colyseus/monitor";
import { playground } from "@colyseus/playground";

// import { RedisDriver } from "@colyseus/redis-driver";
// import { RedisPresence } from "@colyseus/redis-presence";

/**
 * Import your Room files
 */
import { MyRoom } from "./rooms/MyRoom";
import auth from "./config/auth";

export default config({
    options: {
        // devMode: true,
        // driver: new RedisDriver(),
        // presence: new RedisPresence(),
    },

    initializeTransport: (options) => new WebSocketTransport({
        pingInterval: 6000,
        pingMaxRetries: 4,
        maxPayload: 1024 * 1024 * 1, // 1MB Max Payload
      }),

    initializeGameServer: (gameServer) => {
        /**
         * Define your room handlers:
         */
        gameServer.define('my_room', MyRoom);

    },

    initializeExpress: (app) => {
        /**
         * Bind your custom express routes here:
         */
        app.get("/", (req, res) => {
            res.send(`Instance ID => ${process.env.NODE_APP_INSTANCE ?? "NONE"}`);
        });

        /**
         * Bind @colyseus/monitor
         * It is recommended to protect this route with a password.
         * Read more: https://docs.colyseus.io/tools/monitor/
         */
        app.use("/colyseus", monitor());

        // Bind "playground"
        app.use("/playground", playground);

        // Bind auth routes
        app.use(auth.prefix, auth.routes());
    },


    beforeListen: () => {
        /**
         * Before before gameServer.listen() is called.
         */
    }
});
