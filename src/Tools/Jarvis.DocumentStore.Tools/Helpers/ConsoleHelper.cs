using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.DocumentStore.Tools.Helpers
{
    static class ConsoleHelper
    {
        public static Char AskQuestionWithChar(Char[] allowedChars, String question, params object[] param)
        {
            Char answer = '\0';
            do
            {
                Console.Write(question, param);
                Console.Write("[" + String.Join("/", allowedChars) + "]:");
                answer = Console.ReadKey().KeyChar;
            } while (allowedChars.Contains(answer) == false);
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
