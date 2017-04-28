using System;
using System.Linq;

namespace Jarvis.DocumentStore.Tools.Helpers
{
    internal static class ConsoleHelper
    {
        public static Char AskQuestionWithChar(Char[] allowedChars, String question, params object[] param)
        {
            char answer;
            do
            {
                Console.Write(question, param);
                Console.Write("[" + String.Join("/", allowedChars) + "]:");
                answer = Console.ReadKey().KeyChar;
            } while (!allowedChars.Contains(answer));
            return answer;
        }

        public static Boolean AskYesNoQuestion(String question, params object[] param)
        {
            Char answer;
            do
            {
                Console.Write(question, param);
                Console.Write("[Y/N]:");
                answer = Char.ToLower(Console.ReadKey().KeyChar);
                Console.WriteLine();
            } while (answer != 'y' && answer != 'n');
            return answer == 'y';
        }
    }
}
