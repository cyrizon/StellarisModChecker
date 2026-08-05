using System;
using System.Collections.Generic;
using StellarisModChecker.Models;

public interface IPlaysetRepository
{
    void LoadPlaysets();
    Dictionary<string, string>  GetPlaysets();
    List<string> GetPlaysetsID();
    List<Mod> GetModsForPlayset(string playsetId);
}