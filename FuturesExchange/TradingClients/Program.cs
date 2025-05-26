using System;
using System.Threading;

public class MarketMaker
{
    private Thread thread;

    public MarketMaker()
    {
        thread = new Thread(Run);
    }

    public void Start()
    {
        thread.Start();
    }

    private void Run()
    {

    }

    public void Join()
    {
        thread.Join();
    }
}
