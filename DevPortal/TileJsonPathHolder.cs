using System.Threading;

namespace DevPortal;

public class TileJsonPathHolder
{
    public string Path
    {
        get => Volatile.Read(ref field);
        set => Volatile.Write(ref field, value);
    } = "";
}