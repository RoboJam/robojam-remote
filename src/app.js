/* robojam 2014-07-21 */

var RJ = RJ || {};
(function(){
        var WebSocketServer = require('websocket').server;
        var http = require('http');
        var fs = require('fs');
	var SerialPort = require("serialport").SerialPort;
        var clientHtml = fs.readFileSync('client.html');
        var PORT = 8080;
	var SERIAL_PORT = "/dev/ttyACM0";
	var port = new SerialPort(SERIAL_PORT, {
		baudRate: 115200,
		dataBits: 8,
		parity: "none",
		stopBits: 1,
		flowControl: false
	});
        var plainHttpServer;
        var wsServer;

        plainHttpServer = http.createServer( function(req, res) {
                res.writeHead(200, { 'Content-Type': 'text/html'});
                res.end(clientHtml);
        });

        wsServer = new WebSocketServer({httpServer: plainHttpServer});

	port.on("open", function() {
		console.log("open serialport!");
        	plainHttpServer.listen(8080, function() {
                	console.log((new Date()) + ' Server is listening on port 8080');
        	});
	});

        wsServer.on('request', function(request) {
                var connection;

                console.log((new Date()) + ' request ' + request.origin);
                connection = request.accept(null, request.origin);
                console.log((new Date()) + ' Connection accepted. Peer ' + connection.remoteAddress);

                connection.on('message', function(message) {
                        var data = JSON.parse(message.utf8Data);
                        console.log(data);

			port.write(data.direction, function(error, results) {
				console.log("serial error:" + error);
				console.log("serial result:" + results);
			});
                }); 

                connection.on('close', function(reasonCode, description) {
                        console.log((new Date()) + ' Peer ' + connection.remoteAddress + ' disconnected.');
                });
        });

	port.on("data", function(input) {
		if(new String(input).trim().length > 0) {
			console.log("receive from serial:" + input);
		}
	});
})();

