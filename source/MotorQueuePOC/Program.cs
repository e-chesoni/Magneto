using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        MotorController controller = new MotorController();
        controller.AddCommand("1MVA10");  // Move motor on Axis 1 to 10 mm
        controller.AddCommand("2MVR5");   // Move motor on Axis 2 by 5 mm
        controller.AddCommand("1POS?");   // Query position of motor on Axis 1
        controller.AddCommand("2MVA15");  // Move motor on Axis 2 to 15 mm
        controller.AddCommand("1MVR-5");  // Move motor on Axis 1 by -5 mm

        Console.WriteLine("Processing commands. Press Enter to cancel all operations.");
        var cancelTask = Task.Run(() => {
            Console.ReadLine();
            controller.CancelOperations();
        });

        // Await the task that listens for cancellation to ensure that the program stays open.
        await cancelTask;

        Console.WriteLine("Finished processing commands or cancelled. Press any key to exit.");
        Console.ReadKey();
    }
}
