using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CacheType = System.Collections.Generic.Dictionary<int, HackMatcher.State>;

namespace HackMatcher.Solver
{
    /// <summary>
    /// Solver by https://github.com/fidel-solver/exapunks-hack-match/blob/master/solver.cpp
    /// Translated to C#
    /// </summary>
    public class FidelBoardSolver : IBoardSolver
    {
        public IEnumerable<Move> FindMoves(State board, out bool hasMatch)
        {
            hasMatch = false;

            var moves = new List<Move>();
            moves.Clear();
            var maxMaxMoves = board.GetItemCount() < 12 ? 7 : 10;
            for (int maxMoves = 1; maxMoves < maxMaxMoves; ++maxMoves)
            {
                CacheType cache = new CacheType(100000);
                Debug.Assert(!moves.Any());
                if (solveImpl(board, moves, maxMoves, cache))
                {
                    hasMatch = true;
                    return moves;
                }
            }
            if (moves.Any())
            {
                balanceBoard(board, moves);
                if (moves.Any())
                {
                    return moves;
                }
            }

            return new List<Move>();
        }

        private bool hasMatchImpl(State board, int i, int j, Piece item, bool[,] visited, ref int matchesRemaining)
        {
            if (i >= State.MaxCols) return false;
            if (j >= board.GetCountInColumn(i)) return false;
            if (visited[i, j]) return false;
            if (!Equals(board.board[i, j], item)) return false;
            visited[i, j] = true;
            --matchesRemaining;
            if (matchesRemaining == 0) return true;
            if (hasMatchImpl(board, i + 1, j, item, visited, ref matchesRemaining)) return true;
            if (hasMatchImpl(board, i - 1, j, item, visited, ref matchesRemaining)) return true;
            if (hasMatchImpl(board, i, j + 1, item, visited, ref matchesRemaining)) return true;
            if (hasMatchImpl(board, i, j - 1, item, visited, ref matchesRemaining)) return true;
            return false;
        }

        public bool hasMatch(State board, int i, int j)
        {
            bool[,] visited = new bool[State.MaxCols, State.MaxRows];
            int requiredMatchesRemaining = board.board[i, j].bomb ? 2 : 4;
            return hasMatchImpl(board, i, j, board.board[i, j], visited, ref requiredMatchesRemaining);
        }

        bool solveImpl(State board, List<Move> moves, int maxMoves, CacheType cache)
        {
            if (moves.Count() == maxMoves) return false;

            var boardHash = board.GetHashCode();
            var alreadyHasBoard = cache.ContainsKey(boardHash);
            if (alreadyHasBoard) return false;
            cache[boardHash] = board;

            IEnumerable<int> cols = new[] { 0, 1, 2, 3, 4, 5, 6 };
            if (board.held != null)
            {
                // put column indexes in ascending order of height
                var colsList = cols.OrderBy(board.GetCountInColumn).ToList();

                for (int colIndex = 0; colIndex < State.MaxCols; ++colIndex)
                {
                    int i = colsList[colIndex];
                    if (board.GetCountInColumn(i) < State.MaxRows)
                    {
                        State curBoard = new State(board);
                        moves.Add(new Move(Operation.PUT, i));
                        makeMove(curBoard, moves.Last());
                        if (hasMatch(curBoard, i, curBoard.GetCountInColumn(i) - 1))
                        {
                            return true;
                        }
                        if (solveImpl(curBoard, moves, maxMoves, cache)) return true;
                        moves.RemoveAt(moves.Count - 1);
                    }
                }
            }
            else
            {
                // put column indexes in ascending order of height
                var colsList = cols.OrderByDescending(board.GetCountInColumn).ToList();
                for (int colIndex = 0; colIndex < State.MaxCols; ++colIndex)
                {
                    int i = colsList[colIndex];
                    if (board.GetCountInColumn(i) > 0)
                    {
                        State curBoard = new State(board);
                        moves.Add(new Move(Operation.TAKE, i));
                        makeMove(curBoard, moves.Last());
                        if (solveImpl(curBoard, moves, maxMoves, cache)) return true;
                        moves.RemoveAt(moves.Count - 1);
                    }
                }
            }

            for (int i = 0; i < State.MaxCols; ++i)
            {
                if (board.GetCountInColumn(i) > 1)
                {
                    State curBoard = new State(board);
                    moves.Add(new Move(Operation.SWAP, i));
                    makeMove(curBoard, moves.Last());
                    if (hasMatch(curBoard, i, curBoard.GetCountInColumn(i) - 1))
                    {
                        return true;
                    }
                    if (hasMatch(curBoard, i, curBoard.GetCountInColumn(i) - 2))
                    {
                        return true;
                    }
                    if (solveImpl(curBoard, moves, maxMoves, cache)) return true;
                    moves.RemoveAt(moves.Count - 1);
                }
            }
            return false;
        }

        void balanceBoard(State board, List<Move> moves)
        {
            State curBoard = new State(board);
            for (int moveCount = 0; moveCount < 4; ++moveCount)
            {
                IEnumerable<int> cols = new[] { 0, 1, 2, 3, 4, 5, 6 };
                var colsSorted = cols.OrderBy(board.GetCountInColumn).ToList();

                if (curBoard.GetCountInColumn(colsSorted[0]) + 1 >= curBoard.GetCountInColumn(colsSorted[State.MaxCols - 1])) return;
                if (curBoard.held != null)
                {
                    moves.Add(new Move(Operation.PUT, colsSorted[0]));
                }
                else
                {
                    moves.Add(new Move(Operation.TAKE, colsSorted[State.MaxCols - 1]));
                }
                makeMove(curBoard, moves.Last());
            }
        }

        void printMoves(List<Move> moves)
        {
            foreach (var move in moves)
            {
                switch (move.operation)
                {
                    case Operation.TAKE:
                        Console.Write('t');
                        break;
                    case Operation.PUT:
                        Console.Write('p');
                        break;
                    case Operation.SWAP:
                        Console.Write('s');
                        break;
                    default:
                        throw new ArgumentException($"Unsupported move type {move.operation}");
                }
                Console.Write(move.col);
                Console.Write(" ");
            }
            Console.Write("\n");
        }

        void makeMove(State board, Move move)
        {
            int col = move.col;
            switch (move.operation)
            {
                case Operation.TAKE:
                    Debug.Assert(board.GetCountInColumn(col) > 0);
                    Debug.Assert(board.held == null);
                    //--board.counts[col];
                    board.held = board.board[col, board.GetCountInColumn(col)];
                    return;
                case Operation.PUT:
                    Debug.Assert(board.held != null);
                    Debug.Assert(board.GetCountInColumn(col) < State.MaxRows);
                    board.board[col, board.GetCountInColumn(col)] = board.held;
                    //++board.counts[col];
                    board.held = null;
                    return;
                case Operation.SWAP:
                    Debug.Assert(board.GetCountInColumn(col) > 1);
                    var baseDepth = board.GetCountInColumn(col);

                    // swap the two blocks
                    var temp = board.board[col, baseDepth - 1];
                    board.board[col, baseDepth - 1] = board.board[col, baseDepth - 2];
                    board.board[col, baseDepth - 2] = temp;

                    return;
                default:
                    throw new ArgumentException($"Unsupported move type {move.operation}");
            }
        }
    }
}
