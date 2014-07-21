/* robojam 2014-07-21 */

var RJ = RJ || {};
(function(){
        var WebSocketServer = require('websocket').server;
        var http = require('http');
        var fs = require('fs');
        var clientHtml = fs.readFileSync('client.html');
        var PORT = 8080;
        var plainHttpServer;
        var wsServer;

        plainHttpServer = http.createServer( function(req, res) {
                res.writeHead(200, { 'Content-Type': 'text/html'});
                res.end(clientHtml);
        });
        plainHttpServer.listen(8080, function() {
                console.log((new Date()) + ' Server is listening on port 8080');
        });

        wsServer = new WebSocketServer({httpServer: plainHttpServer});
        wsServer.on('request', function(request) {
                var connection;

                console.log((new Date()) + ' request ' + request.origin);
                connection = request.accept(null, request.origin);
                console.log((new Date()) + ' Connection accepted. Peer ' + connection.remoteAddress);

                connection.on('message', function(message) {
                        var data = JSON.parse(msg.utf8Data);
                        console.log(data);
                }); 

                connection.on('close', function(reasonCode, description) {
                        console.log((new Date()) + ' Peer ' + connection.remoteAddress + ' disconnected.');
                });
        });
})();

