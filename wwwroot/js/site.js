const connection = new signalR.HubConnectionBuilder()
    .withUrl("/presenceHub")
    .build();

connection.start()
    .then(() => console.log("SignalR connected"))
    .catch(err => console.error("Error connecting to SignalR:", err));

connection.on("UserStatusChanged", (userId, isActivate) => {
    console.log(`User ${userId} is now ${isActivate ? 'online' : 'offline'}`);
});