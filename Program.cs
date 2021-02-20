using System;
using System.Collections.Generic;
using System.Linq;

namespace Calc
{
    static class Extension
    {
        public static List<T> Slice<T>(this List<T> list, int start = 0, int end = -1)
        {
            if (end == -1) end = list.Count;
            var result = new List<T>();
            for (var i = start; i < end; i++)
                result.Add(list[i]);
            return result;
        }
    }

    class Calc
    {
        private readonly List<string> lexems;
        private string expr;
        private string defSymbols = "+-*/,^";
        private string defNumbers = "1234567890";

        private bool IsSign(string s) => defSymbols.Contains(s);
        private bool IsNumber(string s) => defNumbers.Contains(s);
        private bool IsSign(char s) => defSymbols.Contains(s);
        private bool IsNumber(char s) => defNumbers.Contains(s);

        private string Validate()
        {
            // Есть ли неопределенные символы
            var undefChars = expr.Where(i => !(IsNumber(i) | IsSign(i) | "()".Contains(i)));
            if (undefChars.Any())
            {
                return undefChars.First() == '.'
                    ? $"Unexpected character [ {undefChars.First()} ], maybe you meant [ , ]?"
                    : $"Unexpected character [ {undefChars.First()} ]";
            }

            // Повторяются ли знаки друг за другом
            for (var i = 0; i != expr.Length - 1; i++)
                if (IsSign(expr[i]) && IsSign(expr[i + 1]))
                    return $"Repeating signs [ {expr[i]} ], [ {expr[i + 1]} ]";

            // Есть ли знаки слева и справа выражения
            if (IsSign(expr[0]))
                return $"Expression cannot start with operator [ {expr[0]} ]";
            if (IsSign(expr[^1]))
                return $"Expression cannot end with operator [ {expr[^1]} ]";

            // Совпадают ли скобки

            var parentlessCounter = 0;
            foreach (var i in expr)
            {
                if (i == '(') parentlessCounter++;
                else if (i == ')') parentlessCounter--;
                if (parentlessCounter < 0) return "Error: Parentheses does not match";
            }

            if (parentlessCounter != 0) return "Error: Parentheses does not match";

            // Если всё хорошо
            return null;
        }

        private void Parse()
        {
            expr += '\0';
            for (var i = 0; i < expr.Length - 1; i++)
            {
                if (expr[i] == '(') lexems.Add(expr[i].ToString());
                if (expr[i] == ')') lexems.Add(expr[i].ToString());
                else if (IsSign(expr[i]))
                    lexems.Add(expr[i].ToString());
                else if (IsNumber(expr[i]))
                {
                    lexems.Add("");
                    for (var j = i; j < expr.Length; j++)
                    {
                        if (IsNumber(expr[j]) | expr[j] == ',')
                        {
                            lexems[^1] += expr[j];
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    i -= 1;
                }
            }

            for (var i = 1; i < lexems.Count; i++)
                if ((IsNumber(lexems[i - 1]) | lexems[i - 1] == ")") & lexems[i] == "(")
                    lexems.Insert(i, "*");
        }

        private int FindCloseParenthesis(List<string> list, int openParenthesis)
        {
            var counter = 1;
            for (var i = openParenthesis + 1; i < list.Count; i++)
            {
                if (list[i] == "(") counter++;
                else if (list[i] == ")") counter--;
                if (counter == 0) return i;
            }

            return 0;
        }

        private decimal Pow(decimal a, decimal b)
        {
            if (b == 0) return 1;
            if (a == 0) return 0;
            decimal res = a;
            for (var i = 1; i < Math.Abs(b); i++)
                res *= a;
            return b > 0 ? res : 1 / res;
        }

        private List<string> Abstract(List<string> a)
        {
            Console.WriteLine($"() : {string.Join(" ", a)}");
            var index = 0;
            if (a.Contains("("))
            {
                var buf = a.IndexOf("(");
                var _a = a;
                a = _a.Slice(0, buf);
                a.AddRange(Abstract(_a.Slice(buf + 1, FindCloseParenthesis(_a, buf))));
                a.AddRange(_a.Slice(FindCloseParenthesis(_a, buf) + 1, _a.Count));
            }

            else if (a.Contains("*") | a.Contains("/") | a.Contains("+") | a.Contains("-") | a.Contains("^"))
            {
                if (a.Any(i => i == "^"))
                {
                    index = a.IndexOf(a.First(i => i == "^"));
                    a[index - 1] = Convert.ToString(Pow(decimal.Parse(a[index - 1]), decimal.Parse(a[index + 1])));
                }
                else if (a.Any(i => "*/".Contains(i)))
                {
                    index = a.IndexOf(a.First(i => "*/".Contains(i)));
                    if (a[index] == "*")
                        a[index - 1] = Convert.ToString(decimal.Parse(a[index - 1]) * decimal.Parse(a[index + 1]));
                    else if (a[index] == "/")
                        a[index - 1] = Convert.ToString(decimal.Parse(a[index - 1]) / decimal.Parse(a[index + 1]));
                }

                else if (a.Any(i => "+-".Contains(i)))
                {
                    index = a.IndexOf(a.First(i => "+-".Contains(i)));
                    if (a[index] == "+")
                        a[index - 1] = Convert.ToString(decimal.Parse(a[index - 1]) + decimal.Parse(a[index + 1]));
                    else if (a[index] == "-")
                        a[index - 1] = Convert.ToString(decimal.Parse(a[index - 1]) - decimal.Parse(a[index + 1]));
                }

                a.RemoveAt(index);
                a.RemoveAt(index);
            }
            return a.Count == 1 ? a : Abstract(a);
        }

        public string Solve()
        {
            // Подготовим токены
            var validate = Validate();
            if (validate != null) return $"Error: {validate}";

            Parse();

            // Упростим выражения насколько возможно
            Console.WriteLine("---------------");
            try
            {
                return Abstract(lexems)[0];
            }

            catch (OverflowException)
            {
                return "Error: Value too large or too small";
            }
        }


        public Calc(string expr)
        {
            lexems = new List<string>();
            this.expr = expr.Replace(" ", "");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            start:
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(".------------------------------------------------");
            Console.WriteLine("|   .--------------------.");
            Console.WriteLine("|   | Calculator by Noki |");
            Console.WriteLine("|   '--------------------'");
            Console.WriteLine("|");
            Console.WriteLine("|    Write expression, 'clear' or 'exit'");
            Console.WriteLine("|");
            Console.ForegroundColor = ConsoleColor.Green;
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("| -> ");
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) continue;
                if (input == "clear") goto start;
                if (input == "exit")
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("|");
                    Console.WriteLine("|    Exiting...");
                    Console.WriteLine("'- ");
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"| <- {new Calc(input).Solve()}\n|");
            }
        }
    }
}