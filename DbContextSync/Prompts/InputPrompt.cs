using System;
using PastelExtended;
using System.Diagnostics;

namespace DbContextSync.Prompts
{
	public class InputPrompt: IPrompt<string?>
	{
        public InputPrompt(string question) : base(question) { }
        
        public string? Ask()
        {
            if (AutoSelectValue.HasValue)
            {
                string answer = AutoSelectValue.Value;
                if (!SilentOnAuto)
                {
                    if (SpaceBefore) { Console.WriteLine(); }
                    DrawSelectedValue(answer);
                }
                return answer;
            }
            else
            {
                if (SpaceBefore) { Console.WriteLine(); }
                Console.Write($"{"?".Fg(ConsoleColor.Green)} {Question}: ");
                
                string? answer = DefaultValue.HasValue ? DefaultValue.Value : "";
                int cursorPos = answer.Length;
                bool entered = false;
                while (!entered)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                    if (keyInfo.KeyChar >= 32 && keyInfo.Key != ConsoleKey.Backspace && (keyInfo.Modifiers == 0 || keyInfo.Modifiers == ConsoleModifiers.Shift))
                    {
                        answer = answer.Insert(cursorPos, keyInfo.KeyChar.ToString());
                        cursorPos = RedrawAnswer(answer, cursorPos, 1);
                    }

                    if (keyInfo.Key == ConsoleKey.Backspace && answer.Length > 0)
                    {
                        answer = answer.Remove(cursorPos - 1, 1);
                        cursorPos = RedrawAnswer(answer, cursorPos, -1);
                    }
                    if (keyInfo.Key == ConsoleKey.Delete && cursorPos < answer.Length)
                    {
                        answer = answer.Remove(cursorPos, 1);
                        cursorPos = RedrawAnswer(answer, cursorPos);
                    }

                    if (keyInfo.Key == ConsoleKey.LeftArrow && cursorPos > 0)
                    {
                        cursorPos = RedrawAnswer(answer, cursorPos, -1);
                    }
                    if (keyInfo.Key == ConsoleKey.RightArrow && cursorPos < answer.Length)
                    {
                        cursorPos = RedrawAnswer(answer, cursorPos, 1);
                    }
                    if (keyInfo.Key == ConsoleKey.Home || keyInfo.Key == ConsoleKey.UpArrow)
                    {
                        cursorPos = RedrawAnswer(answer, 0);
                    }
                    if (keyInfo.Key == ConsoleKey.End || keyInfo.Key == ConsoleKey.DownArrow)
                    {
                        cursorPos = RedrawAnswer(answer, answer.Length);
                    }

                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        entered = true;
                    }
                }

                if (answer == "") { answer = null; }

                DrawSelectedValue(answer);
                return answer;
            }

        }


        private int RedrawAnswer(string answer, int cursorPos, int? offsetCursor = null)
        {
            if (offsetCursor != null)
            {
                cursorPos += (int)offsetCursor;
            }

            Console.SetCursorPosition(Question.Length + 4, Console.CursorTop);
            Console.Write($"{answer} ");
            Console.SetCursorPosition(Question.Length + 4 + cursorPos, Console.CursorTop);
            return cursorPos;
        }

        private void DrawSelectedValue(string? answer)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Question.Length + 4));

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine($"{"✔".Fg(ConsoleColor.Green)} {Question}: {(answer == null ? "null".Fg(ConsoleColor.DarkGray) : answer.Fg(ConsoleColor.Cyan))}");
            Console.WriteLine();
        }
    }
}

