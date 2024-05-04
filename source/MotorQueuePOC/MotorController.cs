using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class MotorController
{
    private Dictionary<int, Motor> motors = new Dictionary<int, Motor>();
    private Queue<string> commandQueue = new Queue<string>();
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private bool isCommandProcessing = false;

    public MotorController()
    {
        motors.Add(1, new Motor(1));  // Motor on Axis 1
        motors.Add(2, new Motor(2));  // Motor on Axis 2
    }

    public void AddCommand(string command)
    {
        lock (commandQueue)
        {
            commandQueue.Enqueue(command);
            if (!isCommandProcessing)
            {
                isCommandProcessing = true;
                Task.Run(() => ProcessCommands());
            }
        }
    }

    private async Task ProcessCommands()
    {
        while (commandQueue.Count > 0)
        {
            string command;
            lock (commandQueue)
            {
                command = commandQueue.Dequeue();
            }

            int axisId = int.Parse(command.Substring(0, 1));
            string motorCommand = command.Substring(1);

            if (motors.TryGetValue(axisId, out Motor motor))
            {
                await motor.ExecuteCommand(motorCommand, cancellationTokenSource.Token);
            }
        }
        isCommandProcessing = false;
    }

    public void CancelOperations()
    {
        cancellationTokenSource.Cancel();
        Console.WriteLine("Cancellation requested.");
    }
}
