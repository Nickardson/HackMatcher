namespace HackMatcher
{
    public enum Operation
    {
        GRAB_OR_DROP,
        SWAP
    }

    public struct Move {
        public readonly Operation operation;
        public readonly int col;

        public Move(Operation operation, int col) {
            this.operation = operation;
            this.col = col;
        }

        public override string ToString() {
            return operation.ToString() + '@' + col;
        }
        public override int GetHashCode() {
            return (int)operation * 10 + col;
        }
    }
}