var Room = require('colyseus').Room

class ChatRoom extends Room {

  constructor (options) {
    super(options)

    this.setPatchRate( 1000 / 20 );

    this.setState({ messages: [ "Welcome!" ] })

    console.log("ChatRoom created!", options)
  }

  requestJoin (options) {
    return true;
  }

  onJoin (client) {
    this.state.messages.push(`${ client.id } joined.`)
  }

  onLeave (client) {
    this.state.messages.push(`${ client.id } left.`)
  }

  onMessage (client, data) {
    console.log("onMessage", data);

    this.state.messages.push(data)

    console.log(this.state);
  }

  dispose () {
    console.log("Dispose ChatRoom")
  }

}

module.exports = ChatRoom
