namespace HackMatcher
{
    public enum PieceColor
    {
        RED,
        PINK,
        YELLOW,
        TEAL,
        PURPLE,
        UNKNOWN
    }

    public class Piece {
        public PieceColor color;
        public bool bomb;

        public Piece(PieceColor color, bool bomb) {
            this.color = color;
            this.bomb = bomb;
        }
        public Piece(Piece other) {
            this.color = other.color;
            this.bomb = other.bomb;
        }
        
        public override string ToString() {
            return color.ToString() + (bomb ? "!" : "");
        }
        public string ToString(bool abbrev) {
            return abbrev ? (int)color + (bomb ? "!" : "") : ToString();
        }
        public override bool Equals(object obj) {
            if (obj.GetType() != typeof(Piece))
                return false;
            Piece other = (Piece)obj;
            return ToString() == other.ToString();
        }
        public override int GetHashCode() {
            return ToString().GetHashCode();
        }
    }
}