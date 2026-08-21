namespace CalcEngine.Gui;

static class Program
{
    /// <summary>
    /// Starts the demonstration client and shows the spreadsheet window.
    /// Returns once the user closes it.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }    
}