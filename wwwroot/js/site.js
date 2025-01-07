const connection = new signalR.HubConnectionBuilder()
    .withUrl("/presenceHub")
    .configureLogging(signalR.LogLevel.None)
    .build();

connection.start().catch(function (err) {
    // console.error(err.toString());
});

connection.on("UserStatusChanged", function (userId, isActive) {
    // console.log(`User ${userId} is now ${isActive ? "Online" : "Offline"}`);
});