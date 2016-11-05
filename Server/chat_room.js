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
    this.state.messages.push(`${ client.id } leaved.`)
  }

  onMessage (client, data) {
    if (data.message == "kick") {
      this.clients.filter(c => c.id !== client.id).forEach(other => other.close())

    } else {
      this.state.messages.push(data)
    }

    console.log("ChatRoom:", client.id, data)
  }

  dispose () {
    console.log("Dispose ChatRoom")
  }

}

module.exports = ChatRoom
