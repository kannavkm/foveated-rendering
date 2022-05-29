using System.Threading;
using NetMQ;

// class if we wanted to do something on another thread i.e get data of gaze in our case
public abstract class RunAbleThread
{
    private readonly Thread _runnerThread;

    protected RunAbleThread()
    {
        _runnerThread = new Thread(Run);
    }

    protected bool Running { get; private set; }

    protected abstract void Run();

    public void Start()
    {
        Running = true;
        _runnerThread.Start();
    }

    public void Stop()
    {
        Running = false;
        // block main thread, wait for _runnerThread to finish its job first, so we can be sure that 
        // _runnerThread will end before main thread end
        // _runnerThread.Join();
        _runnerThread.Abort();
        NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
    }
    
}