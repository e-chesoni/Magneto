using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class MotorCommand
{
    public int MotorId
    {
        get; set;
    }
    public string Command
    {
        get; set;
    }
}

public class MotorController
{
    private Dictionary<int, Motor> motors = new Dictionary<int, Motor>();
    private Queue<MotorCommand> commandQueue = new Queue<MotorCommand>();
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private bool isCommandProcessing = false;

    public MotorController()
    {
        motors.Add(1, new Motor(1));
        motors.Add(2, new Motor(2));
    }

    public void AddCommand(int motorId, string command)
    {
        lock (commandQueue)
        {
            commandQueue.Enqueue(new MotorCommand { MotorId = motorId, Command = command });
            if (!isCommandProcessing)
            {
                isCommandProcessing = true;
                Task.Run(() => ProcessCommands());
            }
        }
    }

    private async Task ProcessCommands()
    {
        while (commandQueue.Count > 0 && !cancellationTokenSource.IsCancellationRequested)
        {
            MotorCommand command;
            lock (commandQueue)
            {
                command = commandQueue.Dequeue();
            }
            if (motors.TryGetValue(command.MotorId, out Motor motor))
            {
                await motor.ExecuteCommand(command.Command, cancellationTokenSource.Token);
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
