using NiceCleanLib.Models;

namespace NiceCleanLib.Services.Interfaces;

public interface IPinRepository
{
    Pin Add(Pin pin);
    Pin? Delete(int id);
    List<Pin> GetAll();
    Pin? GetById(int id);
    Pin? Update(int id, Pin pin);
    bool IsLocationOccupied(double latitude, double longitude);
}