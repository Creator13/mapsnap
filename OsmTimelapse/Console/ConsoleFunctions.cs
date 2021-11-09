using System;

namespace ConsoleTools;

public class ConsoleFunctions
{
    public static bool ConfirmPrompt(string prompt, bool defaultValue)
    {
        var yesNo = $"[{(defaultValue ? "Y" : "y")}/{(!defaultValue ? "N" : "n")}]";

        ConsoleKey response;
        do
        {
            Console.Write($"{prompt} {yesNo} ");
            response = Console.ReadKey(false).Key;
            if (response != ConsoleKey.Enter)
            {
                Console.WriteLine();
            }
        } while (response != ConsoleKey.Y && response != ConsoleKey.N && response != ConsoleKey.Enter);

        bool confirmed;
        if (response == ConsoleKey.Enter)
        {
            confirmed = defaultValue;
            Console.WriteLine();
        }
        else
        {
            confirmed = response == ConsoleKey.Y;
        }

        return confirmed;
    }
}
