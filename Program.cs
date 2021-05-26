using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;

namespace NonogramInversor
{
    class Nonogram
    {
        private readonly int[][] columnRules;
        private readonly int[][] rowRules;
        private readonly uint[] board;

        private readonly IDictionary<int, uint[]> cache;

        public Nonogram(int[][] columns, int[][] rows)
        {
            this.columnRules = columns;
            this.rowRules = rows;
            this.board = new uint[rows.Length];
            this.cache = new Dictionary<int, uint[]>();
        }

        private uint[] GetPossibleRows(int rowIndex)
        {
            if (cache.TryGetValue(rowIndex, out var cachedResult))
                return cachedResult;

            var blocks = new Queue<(uint row, int consumed)>();
            blocks.Enqueue((0, 0));

            var length = columnRules.Length;

            var rules = rowRules[rowIndex];

            for (var ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
            {
                var rule = rules[ruleIndex];

                var remainingSum = 0;
                for (var sumIndex = ruleIndex + 1; sumIndex < rules.Length; sumIndex++)
                    remainingSum += rules[sumIndex] + 1;

                var end = length - (rule + remainingSum);

                var flag = 1;
                for (var oneIndex = 0; oneIndex < rule - 1; oneIndex++)
                    flag |= flag << 1;

                for (var blocksCount = blocks.Count; blocksCount > 0; blocksCount--)
                {
                    var (block, consumed) = blocks.Dequeue();

                    for (var start = consumed; start <= end; start++)
                    {
                        var positionedFlag = (uint)(flag << (length - rule - start));
                        var newBlock = block | positionedFlag;
                        var newConsumed = consumed + rule + start + 1;

                        blocks.Enqueue((newBlock, newConsumed));
                    }
                }
            }

            var result = blocks.Select(x => x.row).ToArray();
            cache[rowIndex] = result;

            return result;
        }

        private bool CheckBoard()
        {
#if DEBUG
            for (var row = 0; row < board.Length; row++)
            {
                var rules = rowRules[row];
                var totalCells = 0;
                for (var ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
                    totalCells += rules[ruleIndex];

                var actualCells = Popcnt.PopCount(board[row]);
                if (actualCells != totalCells)
                    return false;
            }
#endif

            for (var column = 0; column < columnRules.Length; column++)
            {
                var rules = columnRules[column];
                var totalCells = 0;
                for (var ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
                    totalCells += rules[ruleIndex];

                var actualCells = 0;
                var flag = 1 << (columnRules.Length - column - 1);

                for (var row = 0; row < board.Length; row++)
                    if ((board[row] & flag) == flag)
                        actualCells++;

                if (actualCells != totalCells)
                    return false;
            }

            return true;
        }

        private bool Solve(int rowIndex)
        {
            if (rowIndex >= board.Length)
                return CheckBoard();

            var possibleRows = GetPossibleRows(rowIndex);
            foreach (var row in possibleRows)
            {
                // Play
                board[rowIndex] = row;

                var result = Solve(rowIndex + 1);
                if (result)
                    return true;

                // Rollback
                board[rowIndex] = 0;
            }

            return false;
        }

        public void Solve()
        {
            var result = Solve(0);
            if (!result)
                throw new Exception();
        }

        public void Inverse()
        {
            for (var row = 0; row < board.Length; row++)
                board[row] = ~board[row];
        }

        public int[][] CalculateSolution()
        {
            var results = new int[columnRules.Length + rowRules.Length][];

            for (var column = 0; column < columnRules.Length; column++)
            {
                var rules = new List<int> { 0 };
                var index = 0;

                var flag = 1 << (columnRules.Length - column - 1);
                var prev = 0u;

                for (var row = 0; row < board.Length; row++)
                {
                    var cell = (uint)(board[row] & flag);
                    if (cell == flag)
                    {
                        if (rules.Count == index)
                            rules.Add(1);
                        else
                            rules[index]++;
                    }
                    else if (prev != 0)
                    {
                        index++;
                    }

                    prev = cell;
                }

                results[column] = rules.ToArray();
            }

            for (var row = 0; row < board.Length; row++)
            {
                var rules = new List<int> { 0 };
                var index = 0;

                var prev = 0u;

                for (var column = 0; column < columnRules.Length; column++)
                {
                    var flag = 1 << (columnRules.Length - column - 1);
                    var cell = (uint)(board[row] & flag);
                    if (cell == flag)
                    {
                        if (rules.Count == index)
                            rules.Add(1);
                        else
                            rules[index]++;
                    }
                    else if (prev != 0)
                    {
                        index++;
                    }

                    prev = cell;
                }

                results[columnRules.Length + row] = rules.ToArray();
            }

            return results;
        }

        public void Print()
        {
            for (var rowIndex = 0; rowIndex < board.Length; rowIndex++)
            {
                var row = board[rowIndex];

                for (var columnIndex = columnRules.Length - 1; columnIndex >= 0; columnIndex--)
                {
                    var flag = 1 << columnIndex;
                    if ((row & flag) == flag)
                        Console.Write(" \x25A0 |");
                    else
                        Console.Write("   |");
                }

                Console.WriteLine();
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // var inputs = Console.ReadLine().Split(' ');
            // var width = int.Parse(inputs[0]);
            // var height = int.Parse(inputs[1]);

            // var columns = new int[width][];
            // for (int i = 0; i < width; i++)
            //     columns[i] = Console.ReadLine().Split(' ').Select(int.Parse).ToArray();

            // var rows = new int[height][];
            // for (int i = 0; i < height; i++)
            //     rows[i] = Console.ReadLine().Split(' ').Select(int.Parse).ToArray();

            // var game = new Nonogram(columns, rows);
            // game.Solve();
            // game.Inverse();
            // var solution = game.CalculateSolution();

            // foreach (var row in solution)
            //     Console.WriteLine(string.Join(" ", row));

            Test0();
            Test1();
            // Test2();
        }

        static void Test(int[][] columns, int[][] rows)
        {
            var game = new Nonogram(columns, rows);
            Console.WriteLine("Original:");
            game.Solve();
            game.Print();
            Console.WriteLine("Inverted:");
            game.Inverse();
            game.Print();
            var solution = game.CalculateSolution();
            foreach (var row in solution)
                Console.WriteLine(string.Join(" ", row));
        }

        static void Test0()
        {
            var columns = new int[][]
            {
                new [] { 1, 1 },
                new [] { 2 },
                new [] { 3 },
                new [] { 4 },
            };

            var rows = new int[][]
            {
                new [] { 4 },
                new [] { 3 },
                new [] { 2 },
                new [] { 1, 1 },
            };

            Test(columns, rows);
        }

        static void Test1()
        {
            var columns = new int[][]
            {
                new [] { 1 },
                new [] { 3 },
                new [] { 2 },
                new [] { 5 },
                new [] { 1 },
            };

            var rows = new int[][]
            {
                new [] { 1 },
                new [] { 1, 3 },
                new [] { 3 },
                new [] { 1, 1 },
                new [] { 1, 1 },
            };

            Test(columns, rows);
        }

        static void Test2()
        {
            var columns = new int[][]
            {
                new [] { 2 },
                new [] { 4 },
                new [] { 4 },
                new [] { 8 },
                new [] { 1, 1 },
                new [] { 1, 1 },
                new [] { 1, 1, 2 },
                new [] { 1, 1, 4 },
                new [] { 1, 1, 4 },
                new [] { 8 },
            };

            var rows = new int[][]
            {
                new [] { 4 },
                new [] { 3, 1 },
                new [] { 1, 3 },
                new [] { 4, 1 },
                new [] { 1, 1 },
                new [] { 1, 3 },
                new [] { 3, 4 },
                new [] { 4, 4 },
                new [] { 4, 2 },
                new [] { 2 },
            };

            Test(columns, rows);
        }
    }
}
