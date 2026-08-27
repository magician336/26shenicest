using System.Text;

namespace DoNotForgetMe.Network
{
    /// <summary>
    /// 房间码生成器。
    /// 房间码即 Fusion 会话名（SessionName），双端用同一串字符即可配对。
    /// 使用无歧义字符集（排除 0/O/1/I/L），口头转述不易出错——
    /// 本作的沟通媒介是现实语音（ADR 0002），房间码常被念出来。
    /// </summary>
    public static class RoomCodeGenerator
    {
        /// <summary>无歧义大写字母数字（34 个字符）。</summary>
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        /// <summary>生成一个 5 位随机房间码（约定区间 4~6 位取中值）。</summary>
        public static string Generate(int length = 5)
        {
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(Alphabet[UnityEngine.Random.Range(0, Alphabet.Length)]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 校验玩家输入的房间码：4~6 位，仅限无歧义字符集内字符。
        /// 返回 true 表示合法。
        /// </summary>
        public static bool IsValid(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            if (code.Length < 4 || code.Length > 6) return false;

            foreach (var c in code)
            {
                if (Alphabet.IndexOf(char.ToUpperInvariant(c)) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>规范化输入（去空白、转大写）。</summary>
        public static string Normalize(string code)
        {
            if (code == null) return string.Empty;

            var sb = new StringBuilder(code.Length);
            foreach (var c in code)
            {
                if (!char.IsWhiteSpace(c))
                {
                    sb.Append(char.ToUpperInvariant(c));
                }
            }
            return sb.ToString();
        }
    }
}
