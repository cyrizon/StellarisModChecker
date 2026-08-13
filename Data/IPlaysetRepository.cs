using System.Collections.Generic;
using StellarisModChecker.Models;

namespace StellarisModChecker.Data;

public interface IPlaysetRepository
{
    void LoadPlaysets();
    Dictionary<string, string>  GetPlaysets();
    List<string> GetPlaysetsID();
    List<Mod> GetModsForPlayset(string playsetId);
}