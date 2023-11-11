using System;
using PastelExtended;
using System.Collections.Generic;
using System.Linq;
using DbContextSync.Extensions;

namespace DbContextSync.Prompts
{
    public class SelectPrompt<TEnum> : ISelectPrompt<TEnum> where TEnum : struct, Enum
    {
        public SelectPrompt(string question): base(question, EnumExtensions.DisplayNameList<TEnum>()) { }

        public void SetHideOptions(HashSet<TEnum> options)
        {
            HideOptionIndexes = options.Cast<int>().ToHashSet();
        }
        public void AddHideOption(TEnum option)
        {
            HideOptionIndexes.Add((int)(object)option);
        }
    }

    public class SelectPrompt : ISelectPrompt<int>
    {
        public SelectPrompt(string question, List<string> options) : base(question, options) { }
        public SelectPrompt(string question, IEnumerable<string> options) : this(question, options.ToList()) { }
    }

	public class ISelectPrompt<T> : IPrompt<T>
	{
        public List<string> Options { get; set; }

        public HashSet<int> HideOptionIndexes { get; set; } = new();
        private List<string> _visibleOptions => Options.Where((o, i) => !HideOptionIndexes.Contains(i)).ToList();

        public Func<int, bool> Validate { get; set; } = _ => true;
        public Func<string, string> SelectedText { get; set; } = value => value;

        public ISelectPrompt(string question, List<string> options) : base(question)
        {
            Options = options;
        }

        public T Ask()
        {
            return Ask(_ => { });
        }

        public T Ask(Action<T> selectionChanged)
        {
            if (AutoSelectValue.HasValue && (int)(object)AutoSelectValue.Value < Options.Count && (int)(object)AutoSelectValue.Value >= 0)
            {
                int selectedIndex = (int)(object)AutoSelectValue.Value;
                if (!SilentOnAuto)
                {
                    if (SpaceBefore) { Console.WriteLine(); }
                    DrawSelectedOption(selectedIndex);
                }
                return (T)(object)selectedIndex;
            }
            else
            {
                int selectedIndex = DefaultValue.HasValue ? (int)(object)DefaultValue : 0;
                if (SpaceBefore) { Console.WriteLine(); }
                DrawOptions(selectedIndex);

                bool selected = false;
                while (!selected)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.UpArrow)
                    {
                        selectedIndex--;
                        if (selectedIndex < 0) { selectedIndex = _visibleOptions.Count - 1; }
                    }
                    if (key == ConsoleKey.DownArrow)
                    {
                        selectedIndex++;
                        if (selectedIndex > _visibleOptions.Count - 1) { selectedIndex = 0; }
                    }

                    if (key == ConsoleKey.UpArrow || key == ConsoleKey.DownArrow)
                    {
                        selectionChanged(MapExternalIndex(selectedIndex));
                        DrawOptions(selectedIndex);
                    }
                    if (key == ConsoleKey.Enter && Validate(selectedIndex))
                    {
                        selected = true;
                    }
                }

                DrawSelectedOption(selectedIndex);
                return MapExternalIndex(selectedIndex);
            }

        }


        private T MapExternalIndex(int selectedIndex)
        {
            int mappedIndex = selectedIndex;

            foreach (int i in HideOptionIndexes)
            {
                if (selectedIndex >= i)
                {
                    mappedIndex++;
                }
            }
            return (T)(object)mappedIndex;
        }


        private void DrawOptions(int selectedIndex)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine($"{"?".Fg(ConsoleColor.Green)} {Question}:");
            for (int i = 0; i < _visibleOptions.Count; i++)
            {
                Console.WriteLine(i == selectedIndex ? $"› {_visibleOptions[i]}".Fg(ConsoleColor.Green) : $"  {_visibleOptions[i]}");
            }
            Console.SetCursorPosition(Question.Length + 4, Console.CursorTop - _visibleOptions.Count - 1);
        }

        private void DrawSelectedOption(int selectedIndex)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine(new string(' ', Question.Length + 3));
            foreach (string option in _visibleOptions)
            {
                Console.WriteLine(new string(' ', option.Length + 2));
            }
            Console.SetCursorPosition(0, Console.CursorTop - _visibleOptions.Count - 1);
            Console.WriteLine($"{"✔".Fg(ConsoleColor.Green)} {Question}: {SelectedText(_visibleOptions[selectedIndex]).Fg(ConsoleColor.Cyan)}");
            Console.WriteLine();
        }
    }
}

