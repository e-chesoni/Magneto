using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MotorQueuePOC;

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
    private Queue<MotorCommand> controllerQueue = new Queue<MotorCommand>();
    private Dictionary<int, Queue<string>> motorQueues = new Dictionary<int, Queue<string>>();
    private Dictionary<int, int> motorPositions = new Dictionary<int, int>();
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private object lockObject = new object();
    private bool isCommandProcessing = false;

    public MotorController()
    {
        // Initialize queues and positions for motor 1 and motor 2
        motorQueues.Add(1, new Queue<string>());
        motorQueues.Add(2, new Queue<string>());
        motorPositions.Add(1, 0); // Assuming starting at position 0
        motorPositions.Add(2, 0); // Assuming starting at position 0
    }

    public void AddCommand(int motorId, string command)
    {
        lock (controllerQueue)
        {
            controllerQueue.Enqueue(new MotorCommand { MotorId = motorId, Command = command });
            if (!isCommandProcessing)
            {
                isCommandProcessing = true;
                Task.Run(() => ProcessControllerQueue(cancellationTokenSource.Token));
            }
        }
    }

    private async Task ProcessControllerQueue(CancellationToken cancellationToken)
    {
        while (controllerQueue.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            MotorCommand command;
            lock (controllerQueue)
            {
                command = controllerQueue.Dequeue();
            }

            Queue<string> motorQueue;
            if (motorQueues.TryGetValue(command.MotorId, out motorQueue))
            {
                lock (motorQueue)
                {
                    motorQueue.Enqueue(command.Command);
                }
                await ProcessMotorQueue(command.MotorId, motorQueue, cancellationToken);
            }
        }
        isCommandProcessing = false;
    }

    private async Task ProcessMotorQueue(int motorId, Queue<string> motorQueue, CancellationToken cancellationToken)
    {
        while (motorQueue.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            string command;
            lock (motorQueue)
            {
                command = motorQueue.Dequeue();
            }
            await SendCommandToMotor(motorId, command, cancellationToken);
        }
    }

    private async Task SendCommandToMotor(int motorId, string command, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        Console.WriteLine($"Motor {motorId}: Executing command {command}");
        await Task.Delay(1000, cancellationToken); // Simulate motor execution time

        // Simulated position checking logic
        int desiredPosition = int.Parse(command.Replace("Move to position ", ""));
        // Simulate moving and checking position
        while (motorPositions[motorId] != desiredPosition && !cancellationToken.IsCancellationRequested)
        {
            motorPositions[motorId] += (motorPositions[motorId] < desiredPosition) ? 10 : -10;
            Console.WriteLine($"Motor {motorId}: Moving to {motorPositions[motorId]}");
            await Task.Delay(100, cancellationToken); // Simulate incremental movement time
        }

        if (!cancellationToken.IsCancellationRequested)
            Console.WriteLine($"Motor {motorId}: Command {command} completed at position {motorPositions[motorId]}");
    }

    public void Run()
    {
        Console.WriteLine("Press Enter to cancel all operations.");
        var task = Task.Run(() =>
        {
            Console.ReadLine();
            cancellationTokenSource.Cancel();
            Console.WriteLine("Cancellation requested.");
        });

        // Wait until all commands are processed or cancelled
        while (isCommandProcessing && !cancellationTokenSource.IsCancellationRequested)
        {
            Thread.Sleep(100); // Poll every 100 ms
        }

        if (!cancellationTokenSource.IsCancellationRequested)
        {
            // Verify final positions (this could be replaced with actual position checking if necessary)
            foreach (var position in motorPositions)
            {
                Console.WriteLine($"Motor {position.Key} is at final position {position.Value}");
            }

            Console.WriteLine("All motors have reached their final positions.");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var controller = new MotorController();
        controller.AddCommand(1, "Move to position 100");
        controller.AddCommand(2, "Move to position 200");
        controller.AddCommand(1, "Move to position 150");

        controller.Run(); // Run and wait for all commands to complete or to be cancelled
    }
}
