using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PalpitheionApi.Data;
using PalpitheionApi.Models;
using System.Threading.Tasks;

namespace PalpitheionApi.Services;

public class StatusPalpitesService(IHubContext<Hub> hubContext)
{
    private bool _votoBloqueado;
    private readonly object _lock = new();
    private readonly IHubContext<Hub> _hubContext = hubContext;

    public bool GetStatus()
    {
        lock (_lock)
        {
            return _votoBloqueado;
        }
    }

    public async Task<bool> ToggleStatus()
    {
        bool novoStatus;

        lock (_lock)
            novoStatus = !_votoBloqueado;

        await _hubContext.Clients.All.SendAsync("StatusPalpitesAlterado", new
        {
            bloquado = novoStatus
        });

        return novoStatus;
    }
}
