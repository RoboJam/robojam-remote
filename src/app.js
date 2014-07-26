/* robojam 2014-07-21 */

var RJ = RJ || {};
(function(){
    var PORT = 8080;
    var SERIAL_PORT = "/dev/ttyACM0";
    var CONTEXT_ROOT = "./";
    var STREAM_MAGIC_BYTES = "jsmp"; 
    var WIDTH = 320;
    var HEIGHT = 240;

    var SERIAL_PARAMS = {
        baudRate: 115200,
    	dataBits: 8,
    	parity: "none",
    	stopBits: 1,
    	flowControl: false
    };

    var VIDEO_PARAMS = [
        "-f", "video4linux2",
        "-s", "" + WIDTH + "x" + HEIGHT,
        "-b", "800k",
        "-r", "30",
        "-i", "/dev/video0",
        "-f", "mpeg1video",
        "pipe:1"
    ];

    var DATA_OPTS = {binary:true};

    var WebSocketServer = require("websocket").server;
    var http = require("http");
    var fs = require("fs");
    var SerialPort = require("serialport").SerialPort;
    var port;
    var plainHttpServer;
    var wsServer;

    var log;
    var onMessage;
    var onSerialData;

    var ffmpeg = require("child_process").spawn("avconv", VIDEO_PARAMS);
    ffmpeg.stdout.on("data", function(data) {
        var clients;
        var client;
        var i;
        if(wsServer) {
            return;
        }

        clients = wsServer.clients;
        for(i in clients ) {
            client = clients[i];
            if (client.readyState == 1) {
                client.send(data, DATA_OPTS);
                log("send data length:" + data.length);
            }
        }
    });
    ffmpeg.stderr.setEncoding("utf8");
    ffmpeg.stderr.on("data", function(data) {
        if(/^execvp\(\)/.test(data)) {
            log("failed to start ffmpeg");
            process.exit(1);
        }
    });

    /* open serial port */
    port = new SerialPort(SERIAL_PORT, SERIAL_PARAMS, function(error) {
        log("serial port open error:" + error);
    });

    /* logging function */
    log = function(msg) {
        console.log(new Date() + " " + msg);
    };

    /* WebSocket handlers */
    onConnection = function(socket) {
        var streamHeader = new Buffer(8);
        streamHeader.write(STREAM_MAGIC_BYTES);
        streamHeader.writeUInt16BE(width, 4);
        streamHeader.writeUInt16BE(height, 6);
        socket.send(streamHeader, DATA_OPTS);
    };
    onMessage = function(message) {
        var data = JSON.parse(message.utf8Data);
        log(data.direction);
        port.write(data.direction, function(error, results) {
            if(error) {
                log("write error:" + error);
            }
        });
        port.drain(function(){});
    };

    /* Serial port handler */
    onSerialData = function(input) {
        if(new String(input).trim().length > 0) {
	    log("receive from serial:" + input);
        }
    };

    /* set up serial port */
    port.on("open", function() {
        /* startup server after opened serial port */
        log("open serialport!");
    });
    port.on("data", onSerialData);

    /* HTTP server for static files. */
    plainHttpServer = http.createServer( function(req, res) {
        res.writeHead(200, {'Content-Type': 'text/html'});
        res.end(fs.readFileSync(CLIENT_PAGE));
    });

    plainHttpServer.listen(PORT, function() {
        log("Server is listening on port " + PORT);
    });

    wsServer = new WebSocketServer({httpServer: plainHttpServer});
    wsServer.on("request", function(request) {
        var connection;
        log("request " + request.origin);
        connection = request.accept(null, request.origin);
        log("Connection accepted. Peer " + connection.remoteAddress);

        connection.on("message", onMessage); 
        connection.on("close", function(reasonCode, description) {
            log("Peer " + connection.remoteAddress + " disconnected.");
        });
    });

})();

