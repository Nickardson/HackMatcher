using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackMatcher.Solver
{
    /// <summary>
    /// Original solver by https://github.com/thquinn
    /// </summary>
    public class QuinnBoardSolver : IBoardSolver
    {
        public IEnumerable<Move> FindMoves(State state, out bool hasMatch)
        {
            hasMatch = false;
            Console.WriteLine("Searching for a move...");
            Queue<State> queue = new Queue<State>();
            Dictionary<State, Tuple<State, Move>> parents = new Dictionary<State, Tuple<State, Move>>();
            queue.Enqueue(state);
            double maxEval = Double.MinValue;
            State maxState = null;
            while (queue.Count > 0 && parents.Count < 25000)
            {
                State current = queue.Dequeue();
                Dictionary<Move, State> children = current.GetChildren();
                foreach (KeyValuePair<Move, State> child in children)
                {
                    if (parents.ContainsKey(child.Value))
                    {
                        continue;
                    }
                    parents.Add(child.Value, new Tuple<State, Move>(current, child.Key));
                    if (parents.Count % 25000 == 0)
                    {
                        Console.WriteLine("Searched " + parents.Count + " states.");
                    }
                    queue.Enqueue(child.Value);
                    // Check eval.
                    double eval = child.Value.Eval();
                    eval -= parents.Count / 10000000f;
                    if (eval > maxEval)
                    {
                        maxEval = eval;
                        maxState = child.Value;
                    }
                }
            }
            Console.WriteLine("Best eval: " + maxEval);
            List<Move> moves = new List<Move>();
            if (maxState == null)
            {
                moves.Add(new Move(Operation.GRAB_OR_DROP, 0));
                return moves;
            }
            while (parents.ContainsKey(maxState))
            {
                Tuple<State, Move> parent = parents[maxState];
                moves.Add(parent.Item2);
                maxState = parent.Item1;
            }
            moves.Reverse();
            if (maxState.hasMatch)
            {
                hasMatch = true;
            }
            return moves;
        }
    }
}
