/* robojam 2014-07-21 */

var RJ = RJ || {};
(function() {
    RJ.WIDTH = 320;
    RJ.HEIGHT = 240;

    var VIDEO_PARAMS = [
        "-f", "video4linux2",
        "-s", "" + RJ.WIDTH + "x" + RJ.HEIGHT,
        "-vsync", "vfr",
        "-b", "800k",
        "-i", "/dev/video0",
        "-f", "mpeg1video",
        "-"
    ];
    RJ.startVideo = function(onData, onError) {
        var ffmpeg;
        ffmpeg = require("child_process").spawn("avconv", VIDEO_PARAMS);
        ffmpeg.stdout.on("data", function(data) {onData(data);});
        ffmpeg.stderr.setEncoding("utf8");
        ffmpeg.stderr.on("data", function(data) {onError(data);});
    };
})();

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

    var SERIAL_PARAMS = {
        baudRate: 115200,
    	dataBits: 8,
    	parity: "none",
    	stopBits: 1,
    	flowControl: false
    };

    var port;
    var plainHttpServer;
    var wsServer;
    var lastClientId = 0;
    var allClients = {};

    var log;
    var onMessage;
    var onConnection;
    var onSerialData;

    RJ.startVideo(function(data) {
        var clients;
        var client;
        var id;
        if(!wsServer) {
            return;
        }

        for(id in allClients) {
            client = allClients[id];
            if (client.connected) {
                try {
                client.sendBytes(data);
                } catch(e) {
                    log("send error:" + e);
                }
            }
        }
    }, function(data) {
        log("video error:" + data);
        if(/^execvp\(\)/.test(data)) {
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
        log("receive command:" + data.direction);
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
    /* port.on("data", onSerialData); */

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

    var initializeCameraClient = function(request) {
        var streamHeader, connection;

        connection = request.accept("camera", request.origin);
        log("Command socket accepted. Peer " + connection.remoteAddress);
	connection.id = ++lastClientId;
	allClients[connection.id] = connection;

        streamHeader = new Buffer(8);
        streamHeader.write(STREAM_MAGIC_BYTES);
        streamHeader.writeUInt16BE(RJ.WIDTH, 4);
        streamHeader.writeUInt16BE(RJ.HEIGHT, 6);
        connection.sendBytes(streamHeader);

        connection.on("close", function(reasonCode, description) {
            delete allClients[connection.id];
            log("Peer " + connection.remoteAddress + " disconnected." + " id:" + connection.id);
        });
    };

    var initializeCommandClient = function(request) {
        var connection;
        connection = request.accept("command", request.origin);
        connection.on("message", onMessage);
    };

    wsServer = new WebSocketServer({httpServer: plainHttpServer});
    wsServer.on("request", function(request) {
        var connection;
        log("request " + request.origin + " protocols " + request.requestedProtocols);

        if(request.requestedProtocols == "command") {
            initializeCommandClient(request);
        }
        if(request.requestedProtocols == "camera") {
            initializeCameraClient(request);
        }
    });

})();

