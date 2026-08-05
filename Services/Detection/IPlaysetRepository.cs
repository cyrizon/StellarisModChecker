using System;
using System.Collections.Generic;

public interface IPlaysetRepository
{
    void LoadPlaysets();
    Dictionary<string, string>  GetPlaysets();
    List<string> GetPlaysetsID();
    void LoadPlaysetContents(string playsetId);
}