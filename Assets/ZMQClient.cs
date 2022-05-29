using System;
using System.Globalization;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public class ZMQClient : RunAbleThread
{

    public volatile float _x = 0.5f, _y = 0.5f;
    protected override void Run()
    {
        ForceDotNet.Force(); // this line is needed to prevent unity freeze after one use, not sure why yet
        using (SubscriberSocket client = new SubscriberSocket())
        {
            client.Connect("tcp://localhost:5555");
            client.Subscribe("message");
            Debug.Log("Connecting to the python program");

            while (Running)
            {
                string message = client.ReceiveFrameString(); // this returns true if it's successful
                string[] words = message.Split(default(string[]), 3, StringSplitOptions.RemoveEmptyEntries);
                _x = float.Parse(words[1], CultureInfo.InvariantCulture.NumberFormat);
                _y = float.Parse(words[2], CultureInfo.InvariantCulture.NumberFormat);
                // Debug.Log(_x);
                // Debug.Log(_y);
            }
        }

        NetMQConfig.Cleanup(); // this line is needed to prevent unity freeze after one use, not sure why yet
    }
}