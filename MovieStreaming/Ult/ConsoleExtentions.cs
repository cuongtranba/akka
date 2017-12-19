using System;

namespace MovieStreaming.Ult
{
    public static class ConsoleExtentions
    {
        public static void WriteLineWithColor(string content, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(content);
            Console.Clear();
        }
    }
}
