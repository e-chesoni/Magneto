using System.Threading.Tasks;
using SAMLIGHT_CLIENT_CTRL_EXLib;

namespace LaserComsPOC;

internal class Program
{
    static ScSamlightClientCtrlEx ctrlNew = new ScSamlightClientCtrlEx();

    /// <summary>
    /// Check communication to WaveRuner
    /// </summary>
    static void cci_hello_world()
    {
        try
        {
            // show hello world message box in samlight
            ctrlNew.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandTest);
        }
        catch (Exception exception)
        {
            Console.WriteLine("CCI Error! \n" + Convert.ToString(exception));
        }
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Hello, SamLight Console App!");
        cci_hello_world();

    }
}
