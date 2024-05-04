using System;

class Program
{
    static void Main(string[] args)
    {
        MotorController controller = new MotorController();
        controller.AddCommand(1, "Move to position 100");
        controller.AddCommand(2, "Move to position 200");

        Console.WriteLine("Press Enter to cancel all operations.");
        Console.ReadLine();

        controller.CancelOperations();
    }
}
