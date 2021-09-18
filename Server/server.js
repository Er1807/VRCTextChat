const WebSocket = require('ws');

const wss = new WebSocket.Server({ port: 8080 });

class Connection {
    constructor(ws, userID) {
        this.ws = ws;
        this.userID = userID;
    }
    send(message) {
        console.log(`>> ${message}`);
        this.ws.send(message);
    }
}

var connectedClients = [];

wss.on('connection', function connection(ws) {

    ws.on('message', function incoming(message) {
        message = message.toString('utf8').trim();
        console.log(message);
        try {
            var args = message.split(" ");
            switch (args[0]) {
                case "startConnection":
                    var con = new Connection(ws, args[1]);
                    connectedClients.push(new Connection(ws, args[1]));
                    con.send(`connected`);
                    break;

                case "sendMessageTo":
                    var currentClient = connectedClients.find(o => o.ws === ws);
                    var remoteClient = connectedClients.find(o => o.userID === args[1]);
                    if (currentClient !=undefined && remoteClient != undefined) {
                        remoteClient.send(`message ${currentClient.userID} ${args[2]}`);
                    } else {
                        currentClient.send('User not online');
                    }
                    break;

                default:
                    console.log(`Missing command`);
                    break;
            }
        
        } catch (error) {
            console.log(error)
            
            var currentClient = connectedClients.find(o => o.wa === ws);
            const index = connectedClients.indexOf(currentClient);
            if (index > -1) {
                connectedClients.splice(index, 1);
            }
            ws.close();   
        }}
    );

    ws.on('close', function close() {
        var currentClient = connectedClients.find(o => o.wa === ws);
        const index = connectedClients.indexOf(currentClient);
        if (index > -1) {
            connectedClients.splice(index, 1);
        }
    });
});