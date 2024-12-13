using App.Models;
using Microsoft.AspNetCore.SignalR;

public class PresenceHub : Hub
{
    private readonly AppDbContext _dbContext;

    public PresenceHub(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name;
        if (userId != null)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userId);
            if (user != null)
            {
                user.isActivate = true;
                await _dbContext.SaveChangesAsync();

                await Clients.Others.SendAsync("UserStatusChanged", userId, true);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.Identity?.Name;
        if (userId != null)
        {
            var user = _dbContext.Users.FirstOrDefault(u => u.UserName == userId);
            if (user != null)
            {
                user.isActivate = false;
                await _dbContext.SaveChangesAsync();

                await Clients.Others.SendAsync("UserStatusChanged", userId, false);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
