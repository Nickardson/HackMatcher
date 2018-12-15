using System.Collections.Generic;

namespace HackMatcher.Solver
{
    public interface IBoardSolver
    {
        IEnumerable<Move> FindMoves(State state, out bool hasMatch);
    }
}
