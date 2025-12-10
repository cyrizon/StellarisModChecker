using System;
using System.Collections.Generic;

public abstract class IPlaysetRepository
{
    public IPlaysetRepository(string playsetPath)
    {
    }

    public abstract void LoadPlaysets();
    public abstract List<string> GetPlaysetsID();
}