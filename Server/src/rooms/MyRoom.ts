import { Room, Client, generateId } from "colyseus";
import { ColyseusRoomState, ColyseusNetworkedEntity, ColyseusNetworkedUser } from "./schema/ColyseusRoomState";
const logger = require("../helpers/logger");

  export class MyRoom extends Room<ColyseusRoomState> {
    
    clientEntities = new Map<string, string[]>();
    serverTime: number = 0;
    customMethodController: any = null;
    customLogic: any;
    roomOptions: any;

    /**
     * Callback for the "customMethod" message from the client to run a custom function within the custom logic.
     * Function name is sent from a client.
     * @param {*} client 
     * @param {*} request 
     */
     onCustomMethod(client: Client, request: any) {
        try {
            if (this.customMethodController != null) {
                this.customMethodController.ProcessMethod(this, client, request);

            } else {
                logger.debug("NO Custom Method Logic Set");
            }

        } catch (error) {
            logger.error("Error with custom Method logic: " + error);
        }
    }
    
    /**
     * Callback for the "entityUpdate" message from the client to update an entity
     * @param {*} clientID 
     * @param {*} data 
     */
     onEntityUpdate(clientID: string, data: any) {
 
        if(this.state.networkedEntities.has(`${data[0]}`) === false) return;

        let stateToUpdate = this.state.networkedEntities.get(data[0]);
        
        let startIndex = 1;
        if(data[1] === "attributes") startIndex = 2;
        
        for (let i = startIndex; i < data.length; i+=2) {
            const property = data[i];
            let updateValue = data[i+1];
            if(updateValue === "inc") {
                updateValue = data[i+2];
                updateValue = parseFloat(stateToUpdate.attributes.get(property)) +  parseFloat(updateValue);
                i++; // inc i once more since we had a inc;
            }

            if(startIndex == 2) {
                stateToUpdate.attributes.set(property, updateValue.toString());
            } else {
                (stateToUpdate as any)[property] = updateValue;
            }
        }

        stateToUpdate.timestamp = parseFloat(this.serverTime.toString());
    }
           
    /**
     * Callback for when the room is created
     * @param {*} options The room options sent from the client when creating a room
     */
    async onCreate(options: any) {
        console.log("\n*********************** EXAMPLE ROOM Created ***********************");
        console.log(options);
        console.log("***********************\n");
        
        this.maxClients = 25;
        this.roomOptions = options;
        
        if(options["roomId"] != null) {
            this.roomId = options["roomId"];           
        }

        // Set the room state
        this.setState(new ColyseusRoomState());

        // Set the callback for the "ping" message for tracking server-client latency
        this.onMessage("ping", (client: Client) => {
            client.send(0, { serverTime: this.serverTime });
        });

        // Set the callback for the "customMethod" message
        this.onMessage("customMethod", (client: Client, request: any) => {
           this.onCustomMethod(client, request);
        });

        // Set the callback for the "entityUpdate" message
        this.onMessage("entityUpdate", (client: Client, entityUpdateArray: any) => {
            if(this.state.networkedEntities.has(`${entityUpdateArray[0]}`) === false) return;

            this.onEntityUpdate(client.id, entityUpdateArray);
        });

        // Set the callback for the "removeFunctionCall" message
        this.onMessage("remoteFunctionCall", (client: Client, RFCMessage: any) => {
            //Confirm Sending Client is Owner 
            if(this.state.networkedEntities.has(`${RFCMessage.entityId}`) === false) return;

            RFCMessage.clientId = client.id;

            // Broadcast the "remoteFunctionCall" to all clients except the one the message originated from
            this.broadcast("onRFC", RFCMessage, RFCMessage.target == 0 ? {} : {except : client});
        });

        // Set the callback for the "setAttribute" message to set an entity or user attribute
        this.onMessage("setAttribute", (client: Client, attributeUpdateMessage: any) => {
            if(attributeUpdateMessage == null 
                || (attributeUpdateMessage.entityId == null && attributeUpdateMessage.userId == null)
                || attributeUpdateMessage.attributesToSet == null) {
                return; // Invalid Attribute Update Message
            }
    
            // Set entity attribute
            if(attributeUpdateMessage.entityId){
                //Check if this client owns the object
                if(this.state.networkedEntities.has(`${attributeUpdateMessage.entityId}`) === false) return;
                
                this.state.networkedEntities.get(`${attributeUpdateMessage.entityId}`).timestamp = parseFloat(this.serverTime.toString());
                let entityAttributes = this.state.networkedEntities.get(`${attributeUpdateMessage.entityId}`).attributes;
                for (let index = 0; index < Object.keys(attributeUpdateMessage.attributesToSet).length; index++) {
                    let key = Object.keys(attributeUpdateMessage.attributesToSet)[index];
                    let value = attributeUpdateMessage.attributesToSet[key];
                    entityAttributes.set(key, value);
                }
            }
            // Set user attribute
            else if(attributeUpdateMessage.userId) {
                
                //Check is this client ownes the object
                if(this.state.networkedUsers.has(`${attributeUpdateMessage.userId}`) === false) {
                    logger.error(`Set Attribute - User Attribute - Room does not have networked user with Id - \"${attributeUpdateMessage.userId}\"`);
                    return;
                }
    
                this.state.networkedUsers.get(`${attributeUpdateMessage.userId}`).timestamp = parseFloat(this.serverTime.toString());
    
                let userAttributes = this.state.networkedUsers.get(`${attributeUpdateMessage.userId}`).attributes;
    
                for (let index = 0; index < Object.keys(attributeUpdateMessage.attributesToSet).length; index++) {
                    let key = Object.keys(attributeUpdateMessage.attributesToSet)[index];
                    let value = attributeUpdateMessage.attributesToSet[key];
                    userAttributes.set(key, value);
                }
            }
    

        });

        // Set the callback for the "removeEntity" message
        this.onMessage("removeEntity", (client: Client, removeId: string) => {
            if(this.state.networkedEntities.has(removeId)) {
                this.state.networkedEntities.delete(removeId);
            }
        });

        // Set the callback for the "createEntity" message
        this.onMessage("createEntity", (client: Client, creationMessage: any) => {
            // Generate new UID for the entity
            let entityViewID = generateId();
            let newEntity = new ColyseusNetworkedEntity().assign({
                id: entityViewID,
                ownerId: client.id,
                timestamp: this.serverTime
            });

            if(creationMessage.creationId != null) newEntity.creationId = creationMessage.creationId;

            newEntity.timestamp = parseFloat(this.serverTime.toString());

            for (let key in creationMessage.attributes) {
                if(key === "creationPos")
                {
                    newEntity.xPos = parseFloat(creationMessage.attributes[key][0]);
                    newEntity.yPos = parseFloat(creationMessage.attributes[key][1]);
                    newEntity.zPos = parseFloat(creationMessage.attributes[key][2]);
                }
                else if(key === "creationRot")
                {
                    newEntity.xRot = parseFloat(creationMessage.attributes[key][0]);
                    newEntity.yRot = parseFloat(creationMessage.attributes[key][1]);
                    newEntity.zRot = parseFloat(creationMessage.attributes[key][2]);
                    newEntity.wRot = parseFloat(creationMessage.attributes[key][3]);
                }
                else {
                    newEntity.attributes.set(key, creationMessage.attributes[key].toString());
                }
            }

            // Add the entity to the room state's networkedEntities map 
            this.state.networkedEntities.set(entityViewID, newEntity);

            // Add the entity to the client entities collection
            if(this.clientEntities.has(client.id)) {
                this.clientEntities.get(client.id).push(entityViewID);
            } else {
                this.clientEntities.set(client.id, [entityViewID]);
            }
            
        });

        this.setPatchRate(1000 / 20);
    
        const customLogic = (options["logic"] != null) ? require('./customLogic/'+options["logic"]+'.js') : null;

        if(customLogic == null)  logger.debug("NO Custom Logic Set");

        this.setSimulationInterval(dt => {
            this.serverTime += dt;
            //Run Custom Logic for room if loaded
            try {
                if(customLogic != null) 
                    customLogic.ProcessLogic(this, dt);

            } catch (error) {
                logger.error("Error with custom room logic: " + error);
            }
            
        } );

    }

    // Callback when a client has joined the room
    onJoin(client: Client, options: any) {
        logger.info(`Client joined!- ${client.sessionId} ***`);
       
        let newNetworkedUser = new ColyseusNetworkedUser().assign({
            id: client.id,
            sessionId: client.sessionId,
        });
        
        this.state.networkedUsers.set(client.sessionId, newNetworkedUser);

        client.send("onJoin", newNetworkedUser);
    }

    // Callback when a client has left the room
    async onLeave(client: Client, consented: boolean) {
        let networkedUser = this.state.networkedUsers.get(client.sessionId);
        
        if(networkedUser){
            networkedUser.connected = false;
        }

        logger.silly(`*** User Leave - ${client.sessionId} ***`);
        // this.clientEntities is keyed by client.id
        // this.state.networkedUsers is keyed by client.sessionid

        try {
            if (consented) {
                throw new Error("consented leave!");
            }

            logger.info("let's wait for reconnection for client: " + client.sessionId);
            const newClient = await this.allowReconnection(client, 10);
            logger.info("reconnected! client: " + newClient.id);

        } catch (e) {
            logger.info("disconnected! client: " + client.id);
            logger.silly(`*** Removing Networked User and Entity ${client.id} ***`);
            
            //remove user
            this.state.networkedUsers.delete(client.sessionId);

            //remove entities
            if(this.clientEntities.has(client.id)) {
                let allClientEntities = this.clientEntities.get(client.id);
                allClientEntities.forEach(element => {

                    this.state.networkedEntities.delete(element);
                });

                // remove the client from clientEntities
                this.clientEntities.delete(client.id);

                if(this.customMethodController != null)
                {
                    this.customMethodController.ProcessUserLeft(this);
                }
            } 
            else{
                logger.error(`Can't remove entities for ${client.id} - No entry in Client Entities!`);
            }
        }
    }

    onDispose() {
    }

}
