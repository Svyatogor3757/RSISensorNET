namespace KUKA.RSI.Sensors {
    public class RSITag : IEquatable<RSITag?> {
        public string Key;
        public string Value;
        public bool FeedBacked;

        public RSITag(string key, string value, bool feedBacked) {
            Key = key;
            Value = value;
            FeedBacked = feedBacked;
        }

        public RSITag() {
            Key = Value = string.Empty;
        }

        public override bool Equals(object? obj) {
            return Equals(obj as RSITag);
        }

        public bool Equals(RSITag? other) {
            return other is not null &&
                   Key == other.Key &&
                   Value == other.Value &&
                   FeedBacked == other.FeedBacked;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Key, Value, FeedBacked);
        }

        public static bool operator ==(RSITag? left, RSITag? right) {
            return EqualityComparer<RSITag>.Default.Equals(left, right);
        }

        public static bool operator !=(RSITag? left, RSITag? right) {
            return !(left == right);
        }
    }
}
