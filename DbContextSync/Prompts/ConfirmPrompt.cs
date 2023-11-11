using System;
using PastelExtended;
using System.Diagnostics;

namespace DbContextSync.Prompts
{
	public class ConfirmPrompt: IPrompt<bool?>
	{
        public ConfirmPrompt(string question) : base(question)
        {
            DefaultValue = false;
        }
        
        public bool Ask()
        {
            if (AutoSelectValue.HasValue)
            {
                bool confirm = (bool)AutoSelectValue.Value;
                if (!SilentOnAuto)
                {
                    if (SpaceBefore) { Console.WriteLine(); }
                    DrawSelectedValue(confirm);
                }
                return confirm;
            }
            else
            {
                bool? confirm = DefaultValue.HasValue ? DefaultValue.Value : null;
                bool entered = false;

                if (SpaceBefore) { Console.WriteLine(); }
                Console.Write($"{"?".Fg(ConsoleColor.Green)} {Question}:{Options()}");

                while (!entered)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.N)
                    {
                        confirm = false;
                    }
                    if (key == ConsoleKey.Y)
                    {
                        confirm = true;
                    }
                    if (key == ConsoleKey.DownArrow || key == ConsoleKey.UpArrow)
                    {
                        if (confirm == null) { confirm = true; }
                        confirm = !confirm;
                    }
                    if (key == ConsoleKey.Enter && confirm != null)
                    {
                        entered = true;
                    }

                    DrawValue(confirm);
                }

                if (confirm != null)
                {
                    DrawSelectedValue((bool)confirm);
                    return (bool)confirm;
                }
                throw new UnreachableException();
            }

        }

        private string Options(bool? confirmedValue = null)
        {
            string y = confirmedValue == null && DefaultValue.HasValue && DefaultValue.Value == true ? "Y" : "y";
            string n = confirmedValue == null && DefaultValue.HasValue && DefaultValue.Value == false ? "N" : "n";
            return $" {$"({y}/{n})".Fg(ConsoleColor.DarkGray)} ";
        }


        private void DrawValue(bool? confirm)
        {
            Console.SetCursorPosition(Question.Length + 3, Console.CursorTop);
            Console.Write(Options(confirm));
            if (confirm == true)
            {
                Console.Write("Yes ");
            }
            if (confirm == false)
            {
                Console.Write("No ");
            }
        }


        private void DrawSelectedValue(bool confirm)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Question.Length + 13));

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine($"{"✔".Fg(ConsoleColor.Green)} {Question}: {(confirm ? "Yes" : "No").Fg(ConsoleColor.Cyan)}");
            Console.WriteLine();
        }
    }
}

