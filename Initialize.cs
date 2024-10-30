using EternalFracture;
using static EternalFracture.InputHandler;
using static EternalFracture.ConsoleLogging;

namespace EternalFracture
{
    class Initialize
    {
        public static void Main(string[] args)
        {
            ConsoleLogging.SelectInputMethod();
            ConsoleLogging.DisplayHeader();
            ConsoleLogging.DisplayInstructions();

            while (true)
            {
                if (InputHandler.UseAsyncKeyState)
                {
                    InputHandler.CheckAsyncKeyState();
                }
                else
                {
                    InputHandler.CheckConsoleReadKey();
                }
                if (InputHandler.UseAsyncKeyState) Thread.Sleep(20);
            }
        }
    }
}