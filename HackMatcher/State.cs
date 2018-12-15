using System;
using System.Collections.Generic;
using System.Text;

namespace HackMatcher
{
    public class State {
        static readonly int[][] NEIGHBORS = { new int[] { -1, 0 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { 0, 1 } };
        static StringBuilder sb = new StringBuilder();
        Piece[,] board;
        public Piece held;
        public bool hasMatch;
        private int hashCode;

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
            HashSet<Tuple<int, int>> toCheck = new HashSet<Tuple<int, int>>();
            for (int x = 0; x < 7; x++) {
                for (int y = 0; y < board.GetLength(1); y++) {
                    if (board[x, y] == null) {
                        break;
                    }
                    toCheck.Add(new Tuple<int, int>(x, y));
                }
            }
            while (toCheck.Count > 3) {
                var enumerator = toCheck.GetEnumerator();
                enumerator.MoveNext();
                Tuple<int, int> start = enumerator.Current;
                bool match = false;
                Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();
                queue.Enqueue(start);
                toCheck.Remove(start);

                // determine how many are already connected
                int count = 1;
                while (queue.Count > 0) {
                    Tuple<int, int> current = queue.Dequeue();
                    foreach (int[] coor in NEIGHBORS) {
                        Tuple<int, int> neighbor = new Tuple<int, int>(current.Item1 + coor[0], current.Item2 + coor[1]);
                        if (neighbor.Item1 < 0 || neighbor.Item1 >= 7) {
                            continue;
                        }
                        if (neighbor.Item2 < 0 || neighbor.Item2 >= board.GetLength(1)) {
                            continue;
                        }
                        if (!toCheck.Contains(neighbor)) {
                            continue;
                        }
                        if (board[neighbor.Item1, neighbor.Item2].color != board[start.Item1, start.Item2].color) {
                            continue;
                        }
                        if (board[neighbor.Item1, neighbor.Item2].bomb != board[start.Item1, start.Item2].bomb) {
                            continue;
                        }
                        count++;
                        if (count >= (board[start.Item1, start.Item2].bomb ? 2 : 4)) {
                            match = true;
                            hasMatch = true;
                        }
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