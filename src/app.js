/* robojam 2014-07-21 */

var RJ = RJ || {};
(function(){
    "use strict";

    var WebSocketServer = require("websocket").server;
    var http = require("http");
    var fs = require("fs");
    var SerialPort = require("serialport").SerialPort;
    var url = require("url");
    var path = require("path");

    var PORT = 8080;
    var SERIAL_PORT = "/dev/ttyACM0";
    var CLIENT_PAGE = "client.html";
    var IOS_CLIENT_PAGE = "ios_client.html";
    var JSMPG = "jsmpg.js";
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
        "-"
    ];

    var port;
    var plainHttpServer;
    var wsServer;
    var lastClientId = 0;
    var allClients = {};

    var log;
    var onMessage;
    var onConnection;
    var onSerialData;

    var ffmpeg = require("child_process").spawn("avconv", VIDEO_PARAMS);
    ffmpeg.stdout.on("data", function(data) {
        var clients;
        var client;
        var id;
        if(!wsServer) {
            return;
        }

        for(id in allClients) {
            client = allClients[id];
            if (client.connected) {
                client.sendBytes(data);
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
    plainHttpServer = http.createServer(function(req, res) {
        var pathname = url.parse(req.url, true).pathname;
        if(pathname.length > 0 && pathname[0] == "/") {
            pathname = pathname.substring(1, pathname.length);
        }

        if(pathname == "" || pathname == CLIENT_PAGE) {
            res.writeHead(200, {'Content-Type': 'text/html'});
            res.end(fs.readFileSync(CLIENT_PAGE));
        } else if(pathname == "" || pathname == IOS_CLIENT_PAGE) {
            res.writeHead(200, {'Content-Type': 'text/html'});
            res.end(fs.readFileSync(IOS_CLIENT_PAGE));
        } else if(pathname == JSMPG) {
            res.writeHead(200, {'Content-Type': 'text/javascript'});
            res.end(fs.readFileSync(JSMPG));
        } else {
            res.writeHead(404, {'Content-Type': 'text/html'});
            res.end("404 file not found.");
        }
    });

    plainHttpServer.listen(PORT, function() {
        log("Server is listening on port " + PORT);
    });

    wsServer = new WebSocketServer({httpServer: plainHttpServer});
    wsServer.on("request", function(request) {
        var connection, streamHeader;
        log("request " + request.origin);
        connection = request.accept(null, request.origin);

	connection.id = ++lastClientId;
	allClients[connection.id] = connection;

        streamHeader = new Buffer(8);
        streamHeader.write(STREAM_MAGIC_BYTES);
        streamHeader.writeUInt16BE(WIDTH, 4);
        streamHeader.writeUInt16BE(HEIGHT, 6);
        connection.sendBytes(streamHeader);

        log("Connection accepted. Peer " + connection.remoteAddress + " id:" + connection.id);

        connection.on("message", onMessage); 
        connection.on("close", function(reasonCode, description) {
            delete allClients[connection.id];
            log("Peer " + connection.remoteAddress + " disconnected." + " id:" + connection.id);
        });
    });

})();

