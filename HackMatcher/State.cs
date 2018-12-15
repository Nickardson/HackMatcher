using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HackMatcher
{
    public class State
    {
        public const int MaxRows = 9;
        public const int MaxCols = 7;

        static readonly int[][] NEIGHBORS = { new int[] { -1, 0 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { 0, 1 } };
        static StringBuilder sb = new StringBuilder();
        public Piece[,] board;
        public Piece held;
        public bool hasMatch;
        private int hashCode;

        /// <summary>
        /// Determine the number of blocks in the given column
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public int GetCountInColumn(int column)
        {
            for (int row = 0; row < MaxRows; row++)
            {
                if (board[column, row] == null)
                {
                    return row;
                }
            }

            return MaxRows;
        }

        /// <summary>
        /// Gets the number of blocks in the board, also counting the held item if any
        /// </summary>
        /// <returns></returns>
        public int GetItemCount()
        {
            int count = held != null ? 1 : 0;

            foreach (var col in board)
            {
                if (col != null)
                {
                    count++;
                }
            }

            return count;
        }

        public State(Piece[,] board, Piece held) {
            this.board = board;
            this.held = held;
            hasMatch = false;
        }
        public State(State other) {
            board = (Piece[,])other.board.Clone();
            if (other.held != null) {
                held = new Piece(other.held);
            } else {
                held = null;
            }
            hasMatch = true;
        }
        private void CalculateHashCode() {
            unchecked {
                hashCode = 17;
                foreach (Piece piece in board) {
                    hashCode *= 31;
                    if (piece == null) {
                        continue;
                    }
                    hashCode += (int)piece.color;
                    hashCode *= 31;
                    hashCode += piece.bomb ? 1 : 0;
                }
                if (held != null) {
                    hashCode *= 31;
                    hashCode += (int)held.color;
                    hashCode *= 31;
                    hashCode += held.bomb ? 1 : 0;
                }
            }
        }

        /// <summary>
        /// Determine a viability score for this state.
        /// </summary>
        /// <returns></returns>
        public double Eval() {
            hasMatch = false;
            double eval = 0;
            
            // make a checklist of all non-empty pieces
            HashSet<Tuple<int, int>> toCheck = new HashSet<Tuple<int, int>>();
            for (int x = 0; x < 7; x++) {
                for (int y = 0; y < board.GetLength(1); y++) {
                    if (board[x, y] == null) {
                        break;
                    }
                    toCheck.Add(new Tuple<int, int>(x, y));
                }
            }

            bool[,] hasAlreadyUsed = new bool[board.GetLength(0), board.GetLength(1)];

            // while there are at least 4 pieces, we could find a match somewhere.
            while (toCheck.Count >= 4) {
                // start looking at the next candidate.
                Tuple<int, int> start = toCheck.First();
                toCheck.Remove(start);
                hasAlreadyUsed[start.Item1, start.Item2] = true;

                // create a queue of items to check
                bool match = false;
                Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();
                queue.Enqueue(start);

                // determine how many are already connected
                int count = 1;
                while (queue.Any()) {
                    // look at the next piece in the queue
                    Tuple<int, int> current = queue.Dequeue();

                    // and check each neighbor
                    foreach (int[] coor in NEIGHBORS)
                    {
                        // determine where the neighbor is
                        var neighborX = current.Item1 + coor[0];
                        var neighborY = current.Item2 + coor[1];

                        // check if that neighbor is in bounds
                        if (neighborX < 0 || neighborX >= 7) {
                            continue;
                        }
                        if (neighborY < 0 || neighborY >= board.GetLength(1)) {
                            continue;
                        }
                        
                        if (board[neighborX, neighborY] == null)
                        {
                            continue;
                        }

                        // ensure we haven't already counted this neighbor as part of something else
                        if (hasAlreadyUsed[neighborX, neighborY])
                        {
                            continue;
                        }

                        // check that the neighbor is compatible with the initial piece
                        if (board[neighborX, neighborY].color != board[start.Item1, start.Item2].color) {
                            continue;
                        }
                        if (board[neighborX, neighborY].bomb != board[start.Item1, start.Item2].bomb) {
                            continue;
                        }

                        // we have a contiguous piece!
                        count++;
                        if (count >= (board[start.Item1, start.Item2].bomb ? 2 : 4)) {
                            match = true;
                            hasMatch = true;
                        }

                        Tuple<int, int> neighbor = new Tuple<int, int>(neighborX, neighborY);
                        hasAlreadyUsed[neighborX, neighborY] = true;
                        queue.Enqueue(neighbor);
                        toCheck.Remove(neighbor);
                    }
                }
                eval += count * count;
                if (match) {
                    eval += 1000;
                }
            }
            for (int x = 0; x < 7; x++) {
                int y = board.GetLength(1) - 1;
                while (y > 0 && board[x, y] == null) {
                    y--;
                }
                if (y > 3) {
                    eval -= 200 * Math.Pow(2, y - 3);
                }
            }
            return eval;
        }

        public Dictionary<Move, State> GetChildren() {
            Dictionary<Move, State> children = new Dictionary<Move, State>();
            // Grab/drop operations.
            for (int x = 0; x < 7; x++) {
                if (held == null) { // Grab.
                    if (board[x, 0] == null) {
                        continue;
                    }
                    int y = board.GetLength(1) - 1;
                    while (board[x, y] == null) {
                        y--;
                    }
                    State child = new State(this);
                    child.held = board[x, y];
                    child.board[x, y] = null;
                    children.Add(new Move(Operation.GRAB_OR_DROP, x), child);
                } else { // Drop.
                    if (board[x, board.GetLength(1) - 1] != null) {
                        continue;
                    }
                    int y = 0;
                    while (board[x, y] != null) {
                        y++;
                    }
                    State child = new State(this);
                    child.board[x, y] = held;
                    child.held = null;
                    children.Add(new Move(Operation.GRAB_OR_DROP, x), child);
                }
                // Swap.
                if (board[x, 1] == null) {
                    continue;
                }
                int swapY = board.GetLength(1) - 1;
                while (board[x, swapY] == null) {
                    swapY--;
                }
                State swapChild = new State(this);
                swapChild.board[x, swapY] = board[x, swapY - 1];
                swapChild.board[x, swapY - 1] = board[x, swapY];
                children.Add(new Move(Operation.SWAP, x), swapChild);
            }
            return children;
        }

        public override string ToString() {
            sb.Clear();
            for (int y = 0; y < board.GetLength(1); y++) {
                for (int x = 0; x < board.GetLength(0); x++) {
                    if (board[x,y] == null) {
                        sb.Append('_');
                        continue;
                    }
                    sb.Append(board[x, y].ToString(true));
                }
            }
            if (held != null) {
                sb.Append(held.ToString(true));
            }
            return sb.ToString();
        }

        public override int GetHashCode() {
            if (hashCode == 0) {
                CalculateHashCode();
            }
            return hashCode;
        }
    }
}